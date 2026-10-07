using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Represents enemy data stored in the spatial grid.
    /// Contains the entity reference and its world position for efficient spatial queries.
    /// </summary>
    public struct EnemyGridData
    {
        public Entity mEntity;
        public float3 mPosition;
    }

    /// <summary>
    /// System that maintains a spatial hash grid for efficient enemy spatial queries.
    /// Uses a 2D grid-based approach to partition enemies by their position for faster collision detection.
    /// Executes during the simulation phase, before enemy movement calculations.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemyMovementSystem))]
    public partial struct EnemyHashSystem : ISystem
    {
        /// <summary>
        /// Global cell size in world units for the spatial hash grid.
        /// Used across all systems and proxy queries for consistent spatial indexing.
        /// </summary>
        public const float CellSize = 2f;

        public NativeParallelMultiHashMap<int2, EnemyGridData> mSpatialGrid;
        private EntityQuery mEnemyQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState pState)
        {
            // Require that at least one EnemySpeedComponent exists before running
            pState.RequireForUpdate<EnemySpeedComponent>();
            
            // Initialize the spatial grid with initial capacity for 1000 enemies
            mSpatialGrid = new NativeParallelMultiHashMap<int2, EnemyGridData>(1000, Allocator.Persistent);
            
            // Build and cache a query for fast enemy counting
            mEnemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemySpeedComponent>()
                .Build();
        }

        /// <summary>
        /// Cleans up the spatial grid resources when the system is destroyed.
        /// Ensures proper memory deallocation to prevent memory leaks.
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState pState)
        {
            // Safely dispose of the grid if it was successfully allocated
            if (mSpatialGrid.IsCreated)
            {
                mSpatialGrid.Dispose();
            }
        }

        /// <summary>
        /// Updates the spatial grid with current enemy positions each frame.
        /// Dynamically adjusts grid capacity based on the current enemy count.
        /// Schedules the HashEnemiesJob for burst-compiled parallel hashing.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState pState)
        {
            // Count the current number of enemies in the world
            int lEnemyCount = mEnemyQuery.CalculateEntityCount();

            // Calculate target capacity with 50% overhead for dynamic growth
            int lTargetCapacity = (int)(lEnemyCount * 1.5f);

            // Expand grid capacity if current capacity is insufficient
            if (mSpatialGrid.Capacity < lTargetCapacity)
            {
                mSpatialGrid.Capacity = lTargetCapacity;
            }   
            // Shrink grid capacity if it's using excessive memory (4x target overhead)
            else if (mSpatialGrid.Capacity > lTargetCapacity * 4)
            {
                // Dispose old grid and allocate new one with optimized capacity
                mSpatialGrid.Dispose();
                mSpatialGrid = new NativeParallelMultiHashMap<int2, EnemyGridData>(lTargetCapacity, Allocator.Persistent);
            }

            // Clear all grid entries from the previous frame
            mSpatialGrid.Clear();
            // Obtain a parallel writer to allow concurrent grid updates from job
            var lGridWriter = mSpatialGrid.AsParallelWriter();
            
            // Schedule the hashing job to process all enemies in parallel
            pState.Dependency = new HashEnemiesJob
            {
                mGridWriter = lGridWriter,
                mCellSize = CellSize,  // Each grid cell represents a 2x2 unit area
            }.ScheduleParallel(pState.Dependency);
        }
    }

    /// <summary>
    /// Burst-compiled job that assigns each enemy to the appropriate grid cell based on position.
    /// Executes in parallel across all enemy entities for performance.
    /// </summary>
    [BurstCompile]
    public partial struct HashEnemiesJob : IJobEntity
    {
        /// <summary>
        /// Parallel writer for adding enemies to grid cells.
        /// Allows multiple job instances to write concurrently without conflicts.
        /// </summary>
        public NativeParallelMultiHashMap<int2, EnemyGridData>.ParallelWriter mGridWriter;
        public float mCellSize;

        /// <summary>
        /// Processes a single enemy entity and adds it to the appropriate grid cell.
        /// Calculates grid position by dividing world position by cell size.
        /// </summary>
        /// <param name="pEntity">The enemy entity being hashed.</param>
        /// <param name="pTransform">The enemy's transform containing world position.</param>
        private void Execute(Entity pEntity, in LocalTransform pTransform, in EnemySpeedComponent _)
        {
            // Convert world position to grid cell coordinates using floor division
            int2 lGridPosition = new int2(
                (int)math.floor(pTransform.Position.x / mCellSize),
                (int)math.floor(pTransform.Position.y / mCellSize)
            );

            // Add this enemy to its corresponding grid cell
            mGridWriter.Add(lGridPosition, new EnemyGridData
            {
                mEntity = pEntity,
                mPosition = pTransform.Position
            });
        }
    }
}