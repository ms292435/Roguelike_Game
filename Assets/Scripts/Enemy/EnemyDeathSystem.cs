using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System responsible for spawning experience orbs when enemies die.
    /// Manages a queue of pending spawn positions and instantiates experience entities.
    /// Executes during the simulation phase, after enemy movement calculations.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyDeathSpawner : ISystem
    {
        /// <summary>
        /// Queue that stores positions where experience orbs should be spawned.
        /// Uses persistent allocation to maintain data across multiple frames.
        /// </summary>
        private NativeQueue<float3> mPendingSpawns;

        public void OnCreate(ref SystemState pState)
        {
            // Ensure the ExperienceSpawnerData singleton exists before system runs
            pState.RequireForUpdate<ExperienceSpawnerData>();
            // Allocate a persistent queue to store spawn positions across frames
            mPendingSpawns = new NativeQueue<float3>(Unity.Collections.Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState pState)
        {
            // Check if the queue was created before attempting to dispose
            if (mPendingSpawns.IsCreated) mPendingSpawns.Dispose();
        }

        public void EnqueueSpawn(float3 pPosition)
        {
            // Add the spawn position to the queue for later processing
            mPendingSpawns.Enqueue(pPosition);
        }

        public void OnUpdate(ref SystemState pState)
        {
            // Early exit if no pending spawns remain
            if (mPendingSpawns.Count == 0) return;

            // Retrieve the experience orb prefab from the singleton data
            Entity lPrefab = SystemAPI.GetSingleton<ExperienceSpawnerData>().mPrefab;
            // Exit if no valid prefab is configured
            if (lPrefab == Entity.Null) return;

            // Process all pending spawn positions
            while (mPendingSpawns.TryDequeue(out float3 pPosition))
            {
                // Instantiate a new experience orb entity from the prefab
                Entity lXpEntity = pState.EntityManager.Instantiate(lPrefab);
                // Set the spawn position using LocalTransform component
                pState.EntityManager.SetComponentData(lXpEntity, LocalTransform.FromPosition(pPosition));
                // Configure experience orb properties with default values
                pState.EntityManager.SetComponentData(lXpEntity, new ExperienceData
                {
                    mValue = 1f,              // Standard experience value per orb
                    mAttractionSpeed = 8f,    // Movement speed when attracted to player
                    mIsAttracted = false,     // Orb is not initially attracted to player
                });
                // Enable the ExperienceData component for this entity
                pState.EntityManager.SetComponentEnabled<ExperienceData>(lXpEntity, true);
            }
        }
    }
}