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
    /// The movement job never records into an entity command buffer: nothing waits for it during the frame,
    /// so it keeps running on the worker threads while the main thread prepares the rendering.
    /// It is completed at the start of the next simulation by the EnemyDeathSystem.
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
        /// Accesses the spatial grid from EnemyHashSystem.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time in seconds.</param>
        /// <param name="pPlayerPosition">Current player world position.</param>
        [BurstCompile]
        private void ScheduleMovementJob(ref SystemState pState, float pDeltaTime, float3 pPlayerPosition)
        {
            // Retrieve the spatial hash system handle and reference
            var lHashSystemHandle = pState.WorldUnmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            ref var lHashSystem = ref pState.WorldUnmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashSystemHandle);

            // Schedule the movement job with all necessary data
            pState.Dependency = new EnemyMovementJob
            {
                mDeltaTime = pDeltaTime,
                mPlayerPosition = pPlayerPosition,
                mGrid = lHashSystem.mGrid,                   // Spatial grid for neighbor detection
                mCellSize = EnemyHashSystem.CellSize,        // Grid cell size in world units
                mElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                mAnimationTiltAngle = math.radians(5f),
                mAnimationTiltSpeed = 5f,
                mJobFrameCount = mSystemFrameCount
            }.ScheduleParallel(pState.Dependency);

            // The movement job reads the grid and depends on the hashing job: completing it releases the grid
            lHashSystem.mGridJobHandle = pState.Dependency;
        }

        /// <summary>
        /// Burst-compiled job that processes movement for each enemy entity.
        /// Handles player collision detection, separation forces, and animation.
        /// </summary>
        [BurstCompile]
        [WithPresent(typeof(EnemyDeadTag), typeof(EnemyContactTag))]
        public partial struct EnemyMovementJob : IJobEntity
        {
            public float mDeltaTime;
            public float3 mPlayerPosition;
            public int mJobFrameCount;
            public float mElapsedTime;
            public float mAnimationTiltAngle;
            public float mAnimationTiltSpeed;
            /// <summary>
            /// Sorted spatial grid built this frame by the EnemyHashSystem (read-only).
            /// </summary>
            public EnemySpatialGrid mGrid;
            
            /// <summary>
            /// Size of each grid cell in world units.
            /// </summary>
            public float mCellSize;


            /// <summary>
            /// Knockback speed above which an enemy completely stops chasing the player.
            /// </summary>
            private const float StaggerKnockbackSpeed = 1.5f;
            private const float HitFlashFadeSpeed = 8f;

            /// <summary>
            /// Executes movement logic for a single enemy entity.
            /// Processes collision with player, separation from nearby enemies, and directional movement.
            /// </summary>
            /// <param name="pEntity">The enemy entity being updated.</param>
            /// <param name="pTransform">Enemy transform component (modified).</param>
            /// <param name="pSpeed">Enemy speed component with smoothed separation (modified).</param>
            /// <param name="pDead">Death marker, enabled when the enemy dies.</param>
            /// <param name="pContact">Contact marker, enabled when the enemy dies by touching the player.</param>
            /// <param name="pHitFlash">Red flash intensity, faded out over time.</param>
            /// <param name="pKnockback">Knockback velocity, added to the movement and damped over time.</param>
            private void Execute(Entity pEntity, ref LocalTransform pTransform, ref EnemySpeedComponent pSpeed,
                EnabledRefRW<EnemyDeadTag> pDead, EnabledRefRW<EnemyContactTag> pContact,
                ref EnemyHitFlash pHitFlash, ref EnemyKnockback pKnockback)
            {
                // Already dead (e.g. hit by a weapon this frame): nothing left to simulate
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
                // Only flags the enemy: the EnemyDeathSystem destroys it, damages the player and drops the
                // experience orb at the start of the next simulation, outside of any job
                // Bigger enemies (elites) touch the player from further away
                float lContactRadius = 0.5f + 0.5f * pTransform.Scale;
                if (lSqrDistanceToPlayer < lContactRadius * lContactRadius)
                {
                    pDead.ValueRW = true;
                    pContact.ValueRW = true;
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

                            // The enemies of the cell are stored contiguously in its bucket
                            mGrid.GetCellRange(lCell, out int lStart, out int lEnd);

                            for (int j = lStart; j < lEnd && lNeighborCount < MAX_NEIGHBORS; j++)
                            {
                                EnemyGridData lOther = mGrid.mEntries[j];

                                // Skip enemies of other cells sharing the bucket, and self-comparison
                                if (math.all(lOther.mCell == lCell) && lOther.mEntity != pEntity)
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

                // --- Hit Feedback ---
                // A knocked back enemy is staggered: it stops chasing the player while the push is strong, then
                // gradually resumes. Without this, its own movement cancels most of the push, especially for runners
                float lStagger = math.saturate(math.length(pKnockback.mVelocity) / StaggerKnockbackSpeed);

                // Calculate new position with frame-rate independent movement
                float3 newPosition = lCurrentPosition + (pSpeed.mValue * (1f - lStagger) * mDeltaTime * finalDirection);

                // The knockback pushes the enemy back, then fades out quickly (exponential damping)
                newPosition += pKnockback.mVelocity * mDeltaTime;
                pKnockback.mVelocity *= math.exp(-EnemyKnockback.DampingRate * mDeltaTime);

                // The red flash fades out in about 0.12 seconds
                pHitFlash.mValue = math.max(0f, pHitFlash.mValue - HitFlashFadeSpeed * mDeltaTime);

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