using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Represents enemy data stored in the spatial grid.
    /// Contains the entity reference, its world position and its grid cell for efficient spatial queries.
    /// </summary>
    public struct EnemyGridData
    {
        public Entity mEntity;
        public float3 mPosition;

        /// <summary>
        /// Grid cell of the enemy. Several cells can share a bucket, so lookups compare it to the requested cell.
        /// </summary>
        public int2 mCell;
    }

    /// <summary>
    /// Read-only view of the sorted spatial grid.
    /// Enemies are stored contiguously, sorted by bucket: all the enemies of a bucket are in the range
    /// [mBucketStart[bucket], mBucketStart[bucket + 1]) of mEntries. A cell is mapped to a bucket with a hash,
    /// so a bucket may also contain enemies from other cells, filtered out with EnemyGridData.mCell.
    /// </summary>
    public struct EnemySpatialGrid
    {
        [ReadOnly] public NativeArray<int> mBucketStart;
        [ReadOnly] public NativeArray<EnemyGridData> mEntries;
        public int mBucketMask;

        /// <summary>
        /// Number of enemies stored in the grid. Zero when the grid is empty.
        /// </summary>
        public int mCount;

        /// <summary>
        /// Converts a world position into grid cell coordinates.
        /// </summary>
        /// <param name="pPosition">World position.</param>
        /// <param name="pCellSize">Size of a grid cell in world units.</param>
        /// <returns>The grid cell containing the position.</returns>
        public static int2 GetCell(float3 pPosition, float pCellSize)
        {
            return new int2((int)math.floor(pPosition.x / pCellSize), (int)math.floor(pPosition.y / pCellSize));
        }

        /// <summary>
        /// Maps a grid cell to its bucket index.
        /// </summary>
        /// <param name="pCell">Grid cell coordinates.</param>
        /// <param name="pBucketMask">Bucket count minus one (the bucket count is a power of two).</param>
        /// <returns>The bucket index of the cell.</returns>
        public static int GetBucket(int2 pCell, int pBucketMask)
        {
            return (int)(math.hash(pCell) & (uint)pBucketMask);
        }

        /// <summary>
        /// Returns the range of mEntries holding the bucket of a cell.
        /// Entries of other cells sharing the same bucket must be skipped by comparing EnemyGridData.mCell.
        /// </summary>
        /// <param name="pCell">Grid cell coordinates.</param>
        /// <param name="pStart">First index of the bucket in mEntries.</param>
        /// <param name="pEnd">Index after the last entry of the bucket.</param>
        public void GetCellRange(int2 pCell, out int pStart, out int pEnd)
        {
            if (mCount == 0)
            {
                pStart = 0;
                pEnd = 0;
                return;
            }

            int lBucket = GetBucket(pCell, mBucketMask);
            pStart = mBucketStart[lBucket];
            pEnd = mBucketStart[lBucket + 1];
        }
    }

    /// <summary>
    /// System that rebuilds the spatial grid used for enemy neighbor queries every frame.
    /// The grid is built with a parallel counting sort instead of concurrent hash map insertions:
    /// 1. ClearBucketCountsJob: resets the per-bucket counters.
    /// 2. ComputeGridKeysJob (parallel): each enemy computes its bucket, writes it at its own index and
    ///    atomically increments its bucket counter.
    /// 3. ComputeBucketOffsetsJob (single job): prefix sum over the buckets, giving the first index of each bucket.
    /// 4. ScatterGridEntriesJob (parallel): each enemy atomically reserves a slot in its bucket and copies itself there.
    /// The atomic operations are spread over thousands of bucket counters, so threads rarely contend, unlike the
    /// hash map whose insertions all go through shared counters.
    /// Neighbor queries then read contiguous memory instead of following a linked list through the hash map.
    /// Executes during the simulation phase, before enemy movement calculations.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemyMovementSystem))]
    public partial struct EnemyHashSystem : ISystem
    {
        /// <summary>
        /// Global cell size in world units for the spatial hash grid.
        /// Internal to the Roguelike.DOTS namespace to avoid leaking spatial grid implementation details to gameplay code.
        /// </summary>
        internal const float CellSize = 2f;

        private const int MinCapacity = 1024;

        /// <summary>
        /// Read-only view of the grid built this frame, used by the movement job and the EnemyBridge.
        /// </summary>
        public EnemySpatialGrid mGrid;

        /// <summary>
        /// Handle of the last job using the spatial grid (the grid jobs, then the movement job that reads it).
        /// Since nothing waits for these jobs during their frame, any main-thread access to the grid
        /// must complete this handle first.
        /// </summary>
        public JobHandle mGridJobHandle;

        private NativeArray<int> mKeys;
        private NativeArray<EnemyGridData> mUnsortedEntries;
        private NativeArray<EnemyGridData> mSortedEntries;
        private NativeArray<int> mBucketStart;

        /// <summary>
        /// Number of enemies per bucket, then reused as the write cursor of each bucket during the scatter.
        /// </summary>
        private NativeArray<int> mBucketCounts;
        private int mCapacity;
        private EntityQuery mEnemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState pState)
        {
            // Same components as ComputeGridKeysJob, so the count matches the indices written by the job
            mEnemyQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, EnemySpeedComponent>()
                .Build();

            Allocate(MinCapacity);
        }

        /// <summary>
        /// Cleans up the spatial grid resources when the system is destroyed.
        /// Ensures proper memory deallocation to prevent memory leaks.
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState pState)
        {
            mGridJobHandle.Complete();
            Dispose();
        }

        /// <summary>
        /// Rebuilds the spatial grid with the current enemy positions.
        /// Resizes the grid arrays when the horde grows, or shrinks a lot, then schedules the grid jobs.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState pState)
        {
            // The grid is still read by the previous frame's movement job, which nothing waits for during its frame.
            // Complete it before resizing the grid arrays on the main thread.
            mGridJobHandle.Complete();

            int lEnemyCount = mEnemyQuery.CalculateEntityCount();
            if (lEnemyCount == 0)
            {
                mGrid.mCount = 0;
                return;
            }

            // Grow with 50% headroom, shrink only when the arrays are 4x too large (hysteresis)
            int lTargetCapacity = math.max(MinCapacity, (int)(lEnemyCount * 1.5f));
            if (mCapacity < lEnemyCount || mCapacity > lTargetCapacity * 4)
            {
                Dispose();
                Allocate(lTargetCapacity);
            }

            int lBucketMask = mBucketCounts.Length - 1;

            var lClearHandle = new ClearBucketCountsJob
            {
                mBucketCounts = mBucketCounts,
            }.Schedule();

            var lKeysHandle = new ComputeGridKeysJob
            {
                mCellSize = CellSize,
                mBucketMask = lBucketMask,
                mKeys = mKeys,
                mEntries = mUnsortedEntries,
                mBucketCounts = mBucketCounts,
            }.ScheduleParallel(JobHandle.CombineDependencies(pState.Dependency, lClearHandle));

            var lOffsetsHandle = new ComputeBucketOffsetsJob
            {
                mBucketCounts = mBucketCounts,
                mBucketStart = mBucketStart,
            }.Schedule(lKeysHandle);

            pState.Dependency = new ScatterGridEntriesJob
            {
                mKeys = mKeys,
                mUnsortedEntries = mUnsortedEntries,
                mBucketCursor = mBucketCounts,
                mSortedEntries = mSortedEntries,
            }.Schedule(lEnemyCount, 1024, lOffsetsHandle);

            mGridJobHandle = pState.Dependency;

            mGrid = new EnemySpatialGrid
            {
                mBucketStart = mBucketStart,
                mEntries = mSortedEntries,
                mBucketMask = lBucketMask,
                mCount = lEnemyCount,
            };
        }

        /// <summary>
        /// Allocates the grid arrays for a given enemy capacity.
        /// The bucket count is a power of two close to the capacity, so a cell maps to a bucket with a bit mask.
        /// </summary>
        /// <param name="pCapacity">Maximum number of enemies the grid can hold.</param>
        private void Allocate(int pCapacity)
        {
            mCapacity = pCapacity;
            int lBucketCount = math.ceilpow2(math.max(MinCapacity, pCapacity / 2));

            mKeys = new NativeArray<int>(pCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            mUnsortedEntries = new NativeArray<EnemyGridData>(pCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            mSortedEntries = new NativeArray<EnemyGridData>(pCapacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            mBucketStart = new NativeArray<int>(lBucketCount + 1, Allocator.Persistent);
            mBucketCounts = new NativeArray<int>(lBucketCount, Allocator.Persistent);
            mGrid = default;
        }

        private void Dispose()
        {
            if (mKeys.IsCreated) mKeys.Dispose();
            if (mUnsortedEntries.IsCreated) mUnsortedEntries.Dispose();
            if (mSortedEntries.IsCreated) mSortedEntries.Dispose();
            if (mBucketStart.IsCreated) mBucketStart.Dispose();
            if (mBucketCounts.IsCreated) mBucketCounts.Dispose();
        }
    }

    /// <summary>
    /// Burst-compiled job resetting the per-bucket counters before they are filled by ComputeGridKeysJob.
    /// </summary>
    [BurstCompile]
    public struct ClearBucketCountsJob : IJob
    {
        public NativeArray<int> mBucketCounts;

        public unsafe void Execute()
        {
            UnsafeUtility.MemClear(mBucketCounts.GetUnsafePtr(), (long)mBucketCounts.Length * sizeof(int));
        }
    }

    /// <summary>
    /// Burst-compiled parallel job computing the grid cell and bucket of every enemy.
    /// Each enemy writes at its own index in the query, and atomically increments the counter of its bucket.
    /// </summary>
    [BurstCompile]
    [WithAll(typeof(EnemySpeedComponent))]
    public partial struct ComputeGridKeysJob : IJobEntity
    {
        public float mCellSize;
        public int mBucketMask;

        [NativeDisableParallelForRestriction] public NativeArray<int> mKeys;
        [NativeDisableParallelForRestriction] public NativeArray<EnemyGridData> mEntries;
        [NativeDisableParallelForRestriction] public NativeArray<int> mBucketCounts;

        /// <summary>
        /// Stores the bucket and grid data of a single enemy at its index in the query, and counts it in its bucket.
        /// </summary>
        /// <param name="pIndex">Index of the enemy in the query, unique per enemy.</param>
        /// <param name="pEntity">The enemy entity.</param>
        /// <param name="pTransform">The enemy's transform containing its world position.</param>
        private unsafe void Execute([EntityIndexInQuery] int pIndex, Entity pEntity, in LocalTransform pTransform)
        {
            int2 lCell = EnemySpatialGrid.GetCell(pTransform.Position, mCellSize);
            int lBucket = EnemySpatialGrid.GetBucket(lCell, mBucketMask);

            mKeys[pIndex] = lBucket;
            mEntries[pIndex] = new EnemyGridData
            {
                mEntity = pEntity,
                mPosition = pTransform.Position,
                mCell = lCell,
            };

            // Atomic increment: other threads may count enemies of the same bucket at the same time
            int* lCounts = (int*)mBucketCounts.GetUnsafePtr();
            Interlocked.Increment(ref lCounts[lBucket]);
        }
    }

    /// <summary>
    /// Burst-compiled prefix sum over the buckets: mBucketStart[b] becomes the index of the first enemy of bucket b.
    /// The counters are turned into write cursors for ScatterGridEntriesJob.
    /// This is the only sequential step, and it is linear in the number of buckets.
    /// </summary>
    [BurstCompile]
    public struct ComputeBucketOffsetsJob : IJob
    {
        public NativeArray<int> mBucketCounts;
        public NativeArray<int> mBucketStart;

        public void Execute()
        {
            int lOffset = 0;
            mBucketStart[0] = 0;

            for (int b = 0; b < mBucketCounts.Length; b++)
            {
                int lCount = mBucketCounts[b];
                mBucketCounts[b] = lOffset;
                lOffset += lCount;
                mBucketStart[b + 1] = lOffset;
            }
        }
    }

    /// <summary>
    /// Burst-compiled parallel job copying every enemy into the next free slot of its bucket.
    /// The slot is reserved with an atomic increment of the bucket cursor, so the order of the enemies
    /// inside a bucket is not deterministic, which does not matter for neighbor queries.
    /// </summary>
    [BurstCompile]
    public struct ScatterGridEntriesJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> mKeys;
        [ReadOnly] public NativeArray<EnemyGridData> mUnsortedEntries;

        [NativeDisableParallelForRestriction] public NativeArray<int> mBucketCursor;
        [NativeDisableParallelForRestriction] public NativeArray<EnemyGridData> mSortedEntries;

        public unsafe void Execute(int pIndex)
        {
            int* lCursors = (int*)mBucketCursor.GetUnsafePtr();
            int lSlot = Interlocked.Increment(ref lCursors[mKeys[pIndex]]) - 1;
            mSortedEntries[lSlot] = mUnsortedEntries[pIndex];
        }
    }
}
