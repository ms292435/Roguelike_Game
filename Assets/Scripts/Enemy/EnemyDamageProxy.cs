using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Static bridge between MonoBehaviour and ECS.
    /// Used by weapons to deal damage to ECS enemies
    /// within a given radius, without needing direct World access from a MonoBehaviour.
    ///
    /// Uses the SpatialGrid from EnemyHashSystem to find enemies
    /// in the area, then adds SpawnExperienceTag + DestroyEntity via ECB.
    /// </summary>
    public static class EnemyDamageProxy
    {
        /// <summary>
        /// Deals damage to all enemies within a radius around a given position.
        /// Killed enemies will spawn an XP orb via ExperienceSpawnSystem.
        /// </summary>
        /// <param name="pPosition">Center of the impact area</param>
        /// <param name="pRadius">Impact radius</param>
        /// <param name="pDamage">Damage dealt (currently unused: one-shot kill)</param>
        public static void DealDamage(Vector3 pPosition, float pRadius, float pDamage)
        {
            // Get the default ECS world; abort if it doesn't exist yet
            var lWorld = World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return;

            // Retrieve the EnemyHashSystem handle from the unmanaged world
            var lHashSystemHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            if (lHashSystemHandle == SystemHandle.Null) return;

            // Get a direct reference to the EnemyHashSystem to access the SpatialGrid
            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashSystemHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return;

            // Retrieve the ECB from the EndSimulationEntityCommandBufferSystem (managed)
            var lEcbSystem = lWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var lEcb = lEcbSystem.CreateCommandBuffer();

            // Cell size must match the value used in EnemyHashSystem when building the grid
            float lCellSize = EnemyHashSystem.CellSize;

            // Pre-compute squared radius to avoid sqrt in distance checks
            float lSqrRadius = pRadius * pRadius;

            // Flatten the impact position to 2D (z = 0) to match the grid's coordinate space
            float3 lPos = new float3(pPosition.x, pPosition.y, 0f);

            // Determine which grid cell the impact position falls into
            int2 lCell = new int2(
                (int)math.floor(pPosition.x / lCellSize),
                (int)math.floor(pPosition.y / lCellSize)
            );

            // Iterate over the 3x3 neighbourhood of cells around the impact cell
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int2 lCheckCell = lCell + new int2(x, y);

                    // Try to get the first enemy stored in this grid cell
                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCheckCell, out EnemyGridData lData, out var lIt))
                    {
                        do
                        {
                            // Flatten the enemy position to 2D so the distance check stays in XY space
                            float3 lEnemyPos = lData.mPosition;
                            lEnemyPos.z = 0f;

                            // Check whether the enemy is within the impact radius
                            if (math.lengthsq(lEnemyPos - lPos) <= lSqrRadius)
                            {
                                // Create a separate ghost entity to carry the XP spawn request.
                                // This avoids the AddComponent / DestroyEntity conflict on the same entity.
                                Entity lXpEntity = lEcb.CreateEntity();
                                lEcb.AddComponent(lXpEntity, new SpawnExperienceTag { mPosition = lEnemyPos });

                                // Schedule the enemy entity for destruction at end of simulation
                                lEcb.DestroyEntity(lData.mEntity);
                            }
                        }
                        while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref lIt));
                    }
                }
            }
        }
    }
}