using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Marker component that tags phantom entities created when an enemy dies.
    /// These phantom entities are created via entity command buffer (ECB), not the enemy itself.
    /// This approach avoids conflicts: the enemy is destroyed separately, and phantom entities
    /// carry spawn position data without blocking the enemy destruction.
    /// Phantom entities are temporary and destroyed after spawning experience orbs.
    /// </summary>
    public struct SpawnExperienceTag : IComponentData
    {
        public float3 mPosition;
    }

    /// <summary>
    /// System that processes phantom experience spawn events and instantiates experience orbs.
    /// Reads phantom entities tagged with SpawnExperienceTag, spawns experience orbs at their positions,
    /// then destroys the phantom entities.
    /// Executes in LateSimulationSystemGroup to ensure all entity command buffers have been played back.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct ExperienceSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState pState)
        {
            // Require that experience spawner data exists (prefab configuration)
            pState.RequireForUpdate<ExperienceSpawnerData>();
            
            // Require that at least one spawn experience tag exists (phantom entity to process)
            pState.RequireForUpdate<SpawnExperienceTag>();
        }

        /// <summary>
        /// Main update loop that processes all pending experience orb spawns.
        /// Iterates through phantom entities, instantiates experience orbs, and cleans up.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Retrieve the experience orb prefab from singleton configuration
            Entity lPrefab = SystemAPI.GetSingleton<ExperienceSpawnerData>().mPrefab;
            
            // Exit early if prefab is invalid or not configured
            if (lPrefab == Entity.Null) return;

            // Create an entity command buffer for deferred entity operations
            var lEcbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var lEcb = lEcbSingleton.CreateCommandBuffer(pState.WorldUnmanaged);

            // --- Process All Pending Spawn Events ---
            // Query all phantom entities marked with SpawnExperienceTag
            foreach (var (lTag, lEntity) in
                SystemAPI.Query<RefRO<SpawnExperienceTag>>().WithEntityAccess())
            {
                // --- Experience Orb Instantiation ---
                // Instantiate a new experience orb from the prefab
                Entity lXp = lEcb.Instantiate(lPrefab);
                
                // Set the experience orb's position to the spawn location from phantom entity
                lEcb.SetComponent(lXp, LocalTransform.FromPosition(lTag.ValueRO.mPosition));
                
                // Configure experience orb properties with default values
                lEcb.SetComponent(lXp, new ExperienceData
                {
                    mValue = 1f,              // Standard experience value per orb
                    mAttractionSpeed = 8f,    // Movement speed when attracted to player
                    mIsAttracted = false,     // Orb is not initially attracted to player
                });
                
                // Enable the ExperienceData component so the orb becomes active
                lEcb.SetComponentEnabled<ExperienceData>(lXp, true);

                // --- Cleanup: Destroy Phantom Entity ---
                // Destroy the phantom entity after it has served its purpose
                // The phantom entity was a temporary carrier for spawn position information
                lEcb.DestroyEntity(lEntity);
            }
        }
    }
}