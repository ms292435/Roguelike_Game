using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that manages enemy movement toward the player with separation and collision avoidance.
    /// Uses spatial hashing for efficient neighbor detection and applies smooth separation forces.
    /// Executes during the simulation phase each frame.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct EnemyMovementSystem : ISystem
    {
        private int mSystemFrameCount;

        [BurstCompile]
        public void OnCreate(ref SystemState pState)
        {
            // Require at least one enemy with a speed component before running
            pState.RequireForUpdate<EnemySpeedComponent>();
        }

        /// <summary>
        /// Main update loop that retrieves player position and schedules the movement job.
        /// Cannot use [BurstCompile] due to managed Player.Instance access.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Exit early if player hasn't been instantiated
            if (Player.Instance == null) return;
            
            // Increment frame counter for staggered separation calculations
            mSystemFrameCount++;

            // Retrieve frame delta time for frame-rate independent movement
            float lDeltaTime = SystemAPI.Time.DeltaTime;
            
            // Get player's current world position
            float3 lPlayerPosition = Player.Instance.transform.position;

            // Schedule the movement job for all enemies
            ScheduleMovementJob(ref pState, lDeltaTime, lPlayerPosition);
        }

        /// <summary>
        /// Schedules the parallel movement job for all enemies.
        /// Accesses the spatial grid from EnemyHashSystem and creates command buffer.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time in seconds.</param>
        /// <param name="pPlayerPosition">Current player world position.</param>
        [BurstCompile]
        private void ScheduleMovementJob(ref SystemState pState, float pDeltaTime, float3 pPlayerPosition)
        {
            // Retrieve the spatial hash system handle and reference
            var lHashSystemHandle = pState.WorldUnmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            ref var lHashSystem = ref pState.WorldUnmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashSystemHandle);

            // Create an entity command buffer for deferred entity creation and destruction
            var lEcbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var lEcb = lEcbSingleton.CreateCommandBuffer(pState.WorldUnmanaged).AsParallelWriter();

            // Schedule the movement job with all necessary data
            pState.Dependency = new EnemyMovementJob
            {
                mDeltaTime = pDeltaTime,
                mPlayerPosition = pPlayerPosition,
                mGrid = lHashSystem.mSpatialGrid,              // Spatial grid for neighbor detection
                mCellSize = EnemyHashSystem.CellSize,        // Grid cell size in world units
                mECB = lEcb,                                 // Command buffer for entity operations
                mElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                mAnimationTiltAngle = math.radians(5f),
                mAnimationTiltSpeed = 5f,
                mJobFrameCount = mSystemFrameCount
            }.ScheduleParallel(pState.Dependency);
        }

        /// <summary>
        /// Burst-compiled job that processes movement for each enemy entity.
        /// Handles player collision detection, separation forces, and animation.
        /// </summary>
        [BurstCompile]
        [WithPresent(typeof(EnemyDeadTag))]
        public partial struct EnemyMovementJob : IJobEntity
        {
            public float mDeltaTime;
            public float3 mPlayerPosition;
            public int mJobFrameCount;
            public float mElapsedTime;
            public float mAnimationTiltAngle;
            public float mAnimationTiltSpeed;
            [ReadOnly] public NativeParallelMultiHashMap<int2, EnemyGridData> mGrid;
            
            /// <summary>
            /// Size of each grid cell in world units.
            /// </summary>
            public float mCellSize;
            public EntityCommandBuffer.ParallelWriter mECB;

            /// <summary>
            /// Executes movement logic for a single enemy entity.
            /// Processes collision with player, separation from nearby enemies, and directional movement.
            /// </summary>
            /// <param name="pSortKey">Entity index for command buffer ordering.</param>
            /// <param name="pEntity">The enemy entity being updated.</param>
            /// <param name="pTransform">Enemy transform component (modified).</param>
            /// <param name="pSpeed">Enemy speed component with smoothed separation (modified).</param>
            /// <param name="pDamage">Enemy damage component (read-only).</param>
            /// <param name="pDead">Death marker, enabled once the enemy is queued for destruction.</param>
            private void Execute([EntityIndexInQuery] int pSortKey, Entity pEntity,
                ref LocalTransform pTransform, ref EnemySpeedComponent pSpeed, in EnemyDamageComponent pDamage,
                EnabledRefRW<EnemyDeadTag> pDead)
            {
                // Already queued for destruction (e.g. hit by a weapon this frame): nothing left to simulate
                if (pDead.ValueRO) return;

                // Get current enemy position and normalize to 2D space (z = 0)
                float3 lCurrentPosition = pTransform.Position;
                lCurrentPosition.z = 0f;
                
                // Get player position and normalize to 2D space
                float3 lPlayerPosition = mPlayerPosition;
                lPlayerPosition.z = 0f;

                // Calculate squared distance to reduce expensive square root operations
                float lSqrDistanceToPlayer = math.lengthsq(lPlayerPosition - lCurrentPosition);

                // --- Death Detection: Only when enemy touches player ---
                if (lSqrDistanceToPlayer < 1f)
                {
                    // Create phantom entity to deliver damage to player
                    var lDmgEntity = mECB.CreateEntity(pSortKey);
                    mECB.AddComponent(pSortKey, lDmgEntity, new DamagePlayerTag { mValue = pDamage.mValue });

                    // Create phantom entity to spawn experience orb at death position
                    var lXpEntity = mECB.CreateEntity(pSortKey);
                    mECB.AddComponent(pSortKey, lXpEntity, new SpawnExperienceTag { mPosition = lCurrentPosition });

                    // Destroy the enemy entity and mark it so no other code path destroys it again
                    pDead.ValueRW = true;
                    mECB.DestroyEntity(pSortKey, pEntity);
                    return;
                }

                // --- Movement Calculation ---
                // Calculate normalized direction toward player
                float3 lTargetDirection = math.normalizesafe(lPlayerPosition - lCurrentPosition);

                // --- Separation: Staggered over frames to reduce grid lookup cost ---
                // Only process separation every 4 frames, distributed by entity index
                if ((pEntity.Index % 4) == (mJobFrameCount % 4))
                {
                    // Calculate which grid cell the enemy occupies
                    int2 lGridPosition = new int2(
                        (int)math.floor(lCurrentPosition.x / mCellSize),
                        (int)math.floor(lCurrentPosition.y / mCellSize)
                    );

                    // Accumulator for separation forces from nearby enemies
                    float3 lSeparationForce = float3.zero;
                    
                    // Squared radius for separation detection (1.2^2 = 1.44)
                    float lSqrSepRadius = 1.44f;
                    
                    // Count of neighbors found (limited to prevent expensive computations)
                    int lNeighborCount = 0;
                    const int MAX_NEIGHBORS = 4;

                    // Spiral pattern for neighbor traversal: avoids bias toward bottom-left cells
                    // This prevents correlated separation forces that would cause drift
                    unsafe
                    {
                        // Precomputed spiral offsets from center cell outward
                        int2* lSpiral = stackalloc int2[9]
                        {
                            new int2( 0,  0), new int2( 1,  0), new int2( 0,  1),
                            new int2(-1,  0), new int2( 0, -1), new int2( 1,  1),
                            new int2(-1,  1), new int2( 1, -1), new int2(-1, -1)
                        };

                        // Iterate through nearby grid cells in spiral order
                        for (int i = 0; i < 9 && lNeighborCount < MAX_NEIGHBORS; i++)
                        {
                            // Calculate target cell coordinates
                            int2 lCell = lGridPosition + lSpiral[i];

                            // Attempt to retrieve first enemy in this cell
                            if (mGrid.TryGetFirstValue(lCell, out EnemyGridData lOther, out var it))
                            {
                                // Iterate through all enemies in this cell
                                do
                                {
                                    // Skip self-comparison
                                    if (lOther.mEntity != pEntity)
                                    {
                                        // Get other enemy position and normalize to 2D
                                        float3 lOtherPos = lOther.mPosition;
                                        lOtherPos.z = 0f;
                                        
                                        // Calculate displacement vector from other to current
                                        float3 lDiff = lCurrentPosition - lOtherPos;
                                        float lSqrDist = math.lengthsq(lDiff);

                                        // Check if within separation radius
                                        if (lSqrDist < lSqrSepRadius)
                                        {
                                            // Handle zero-distance edge case with hash-based fallback
                                            // Ensures deterministic and well-distributed separation directions
                                            if (lSqrDist < 1e-6f)
                                            {
                                                // Generate pseudo-random direction using entity index hash
                                                uint h = (uint)pEntity.Index * 2654435761u;
                                                lDiff = new float3(
                                                    ((h & 0xFF) / 127.5f) - 1f,
                                                    (((h >> 8) & 0xFF) / 127.5f) - 1f,
                                                    0f
                                                ) * 0.01f;
                                            }

                                            // Calculate separation force scaled by proximity
                                            float force = 1f - (lSqrDist / lSqrSepRadius);
                                            lSeparationForce += math.normalizesafe(lDiff) * force;
                                            lNeighborCount++;
                                        }
                                    }
                                } while (mGrid.TryGetNextValue(out lOther, ref it) && lNeighborCount < MAX_NEIGHBORS);
                            }
                        }
                    }

                    // Normalize separation force if its magnitude exceeds 1
                    if (math.lengthsq(lSeparationForce) > 1f)
                        lSeparationForce = math.normalizesafe(lSeparationForce);

                    // Smoothly interpolate toward new separation force (15 units/sec response time)
                    pSpeed.mSmoothedSeparation = math.lerp(pSpeed.mSmoothedSeparation, lSeparationForce, mDeltaTime * 15f);
                }
                else
                {
                    // On off-frames, decay separation force toward zero
                    pSpeed.mSmoothedSeparation = math.lerp(pSpeed.mSmoothedSeparation, float3.zero, mDeltaTime * 2f);
                }

                // --- Final Direction Calculation ---
                // Combine chase direction with separation force (separation weighted at 8x)
                float3 finalDirection = math.normalizesafe(lTargetDirection + (pSpeed.mSmoothedSeparation * 8.0f));
                finalDirection.z = 0f;

                // Calculate new position with frame-rate independent movement
                float3 newPosition = lCurrentPosition + (pSpeed.mValue * mDeltaTime * finalDirection);
                newPosition.z = 0f;

                // --- Animation ---
                // Calculate per-entity animation offset to prevent synchronized tilt across all enemies
                float lAnimOffset = (pEntity.Index % 1000) * 0.01f;
                
                // Calculate sinusoidal tilt based on elapsed time
                float lTilt = math.sin((mElapsedTime + lAnimOffset) * mAnimationTiltSpeed) * mAnimationTiltAngle;
                
                // Apply rotation and update position
                pTransform.Rotation = quaternion.RotateZ(lTilt);
                pTransform.Position = newPosition;
            }
        }
    }
}