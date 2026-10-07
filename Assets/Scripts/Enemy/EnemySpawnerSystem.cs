using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that manages enemy spawning at screen edges based on difficulty progression.
    /// Runs on the main thread to access Unity's Camera and rendering systems.
    /// Instantiates enemy waves with random spawn positions around viewport boundaries.
    /// </summary>
    public partial struct EnemySpawnerSystem : ISystem
    {
        /// <summary>
        /// Cached batch mesh ID for rendering spawned enemies.
        /// Registered once during first update and reused for all enemies.
        /// </summary>
        private BatchMeshID mMeshID;
        
        /// <summary>
        /// Cached batch material ID for rendering spawned enemies.
        /// Registered once during first update and reused for all enemies.
        /// </summary>
        private BatchMaterialID mMaterialID;
        private bool mInitialized;

        /// <summary>
        /// Initializes the system and sets up requirements.
        /// </summary>
        public void OnCreate(ref SystemState pState)
        {
            // Require spawner data and managed components to be present
            pState.RequireForUpdate<EnemySpawnerData>();
            pState.RequireForUpdate<EnemySpawnerManaged>();
        }

        /// <summary>
        /// Main update loop that handles spawning logic each frame.
        /// Checks spawn timer, calculates screen edge positions, and instantiates enemy waves.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Maximum enemy cap to prevent performance degradation
            int lMaxEnemyCount = 100000;

            // Exit early if maximum enemy count is reached
            if (EnemyCounterSystem.TotalEnemies >= lMaxEnemyCount) return;

            // --- Camera Initialization Check ---
            // Ensure main camera exists before proceeding with spawn logic
            if (Camera.main == null)
            {
                Debug.LogWarning("Spawner: No camera tagged 'MainCamera' found in the scene!");
                return;
            }

            // --- One-Time Mesh and Material Registration ---
            // Extract and cache mesh/material references from prefab's RenderMeshArray
            if (!mInitialized)
            {
                // Query spawner data to access the enemy prefab
                foreach (var (lSpawnerData, _) in SystemAPI.Query<RefRO<EnemySpawnerData>, EnemySpawnerManaged>())
                {
                    // Skip if prefab is invalid
                    if (lSpawnerData.ValueRO.mPrefab == Entity.Null) continue;

                    // Check if prefab has rendering components
                    if (pState.EntityManager.HasComponent<RenderMeshArray>(lSpawnerData.ValueRO.mPrefab))
                    {
                        // Retrieve the RenderMeshArray shared component from prefab
                        var lRenderMeshArray = pState.EntityManager.GetSharedComponentManaged<RenderMeshArray>(lSpawnerData.ValueRO.mPrefab);
                        
                        // Get the Entities Graphics system for batch rendering
                        var lEntitiesGraphicsSystem = pState.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

                        // Register first mesh in array with batch renderer
                        mMeshID = lEntitiesGraphicsSystem.RegisterMesh(lRenderMeshArray.MeshReferences[0]);
                        
                        // Register first material in array with batch renderer
                        mMaterialID = lEntitiesGraphicsSystem.RegisterMaterial(lRenderMeshArray.MaterialReferences[0]);
                        
                        // Mark initialization complete
                        mInitialized = true;
                    }
                }

                // Exit if initialization was incomplete
                if (!mInitialized) return;
            }

            // --- Spawn Logic ---
            // Get current enemy count for capacity checks
            int lCurrentEnemyCount = EnemyCounterSystem.TotalEnemies;
            
            // Clamp delta time to prevent large jumps (max 0.1s per frame)
            float lDeltaTime = math.min(SystemAPI.Time.DeltaTime, 0.1f);
            
            // Get main camera and viewport information
            Camera lCam = Camera.main;
            float3 lCamPos = lCam.transform.position;
            float lHeight = lCam.orthographicSize;
            float lWidth = lHeight * lCam.aspect;
            float lMargin = 2f;  // Buffer distance outside viewport for spawn edge
            
            // Iterate through all spawner entities
            foreach (var (lSpawnerData, _) in SystemAPI.Query<RefRW<EnemySpawnerData>, EnemySpawnerManaged>())
            {
                // --- Prefab Validation Check ---
                // Ensure enemy prefab is valid before attempting instantiation
                if (lSpawnerData.ValueRO.mPrefab == Unity.Entities.Entity.Null)
                {
                    Debug.LogError("Spawner: Enemy Prefab is missing or corrupted!");
                    continue;
                }

                // Update game timer and spawn timer
                lSpawnerData.ValueRW.mGameTimer += lDeltaTime;
                lSpawnerData.ValueRW.mTimer += lDeltaTime;

                // Get current spawn interval (can be modified by difficulty curve)
                float lCurrentInterval = lSpawnerData.ValueRO.mBaseSpawnInterval;

                // --- Spawn Trigger Check ---
                // Trigger spawn wave when timer exceeds interval
                if (lSpawnerData.ValueRO.mTimer >= lCurrentInterval)
                {
                    // Reset spawn timer for next wave
                    lSpawnerData.ValueRW.mTimer = 0f;

                    // Calculate how many enemies can be spawned (respects max cap)
                    int lRemaining = lMaxEnemyCount - lCurrentEnemyCount;
                    int lCount = math.min(lMaxEnemyCount, lRemaining);

                    // Randomly select which screen edge to spawn from
                    int lSide = lSpawnerData.ValueRW.mRandom.NextInt(0, 4);
                    float lSpawnX = 0, lSpawnY = 0;
                    
                    // Calculate spawn position based on selected screen edge
                    switch (lSide)
                    {
                        case 0: // UP - Spawn along top edge
                            lSpawnX = lSpawnerData.ValueRW.mRandom.NextFloat(-lWidth, lWidth);
                            lSpawnY = lHeight + lMargin;
                            break;
                        case 1: // DOWN - Spawn along bottom edge
                            lSpawnX = lSpawnerData.ValueRW.mRandom.NextFloat(-lWidth, lWidth);
                            lSpawnY = -lHeight - lMargin;
                            break;
                        case 2: // LEFT - Spawn along left edge
                            lSpawnX = -lWidth - lMargin;
                            lSpawnY = lSpawnerData.ValueRW.mRandom.NextFloat(-lHeight, lHeight);
                            break;
                        case 3: // RIGHT - Spawn along right edge
                            lSpawnX = lWidth + lMargin;
                            lSpawnY = lSpawnerData.ValueRW.mRandom.NextFloat(-lHeight, lHeight);
                            break;
                    }

                    // Calculate cluster center position (offset from camera, Z at 0)
                    float3 lClusterCenter = new float3(lCamPos.x + lSpawnX, lCamPos.y + lSpawnY, 0f);

                    // --- Enemy Instantiation ---
                    // Allocate temporary array for spawned enemy references
                    var lSpawnedEnemies = new NativeArray<Entity>(lCount, Allocator.Temp);
                    
                    // Instantiate all enemies in the wave at once
                    pState.EntityManager.Instantiate(lSpawnerData.ValueRO.mPrefab, lSpawnedEnemies);
                    
                    // --- Position and Rendering Setup ---
                    // Apply random offsets and rendering configuration to each enemy
                    for (int i = 0; i < lSpawnedEnemies.Length; i++)
                    {
                        var lEnemyEntity = lSpawnedEnemies[i];
                        
                        // Generate random offset to prevent perfectly overlapping spawn positions
                        float3 lOffset = new float3(
                            lSpawnerData.ValueRW.mRandom.NextFloat(-1f, 1f),
                            lSpawnerData.ValueRW.mRandom.NextFloat(-1f, 1f),
                            0f
                        );

                        // Set entity position at cluster center plus random offset
                        SystemAPI.SetComponent(lEnemyEntity, LocalTransform.FromPosition(lClusterCenter + lOffset));

                        // Assign batch rendering information for visual rendering
                        pState.EntityManager.SetComponentData(lEnemyEntity, new MaterialMeshInfo
                        {
                            MeshID = mMeshID,           // Use cached mesh ID
                            MaterialID = mMaterialID    // Use cached material ID
                        });
                    }

                    // Dispose temporary array to prevent memory leaks
                    lSpawnedEnemies.Dispose();
                }
            }
        }
    }
}