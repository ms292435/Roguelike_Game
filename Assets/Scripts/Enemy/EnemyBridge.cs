using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Architectural Bridge / Facade between high-level MonoBehaviour gameplay systems and DOTS/ECS.
    /// Encapsulates all unmanaged ECS queries, spatial grid traversals, and entity command buffers,
    /// shielding gameplay scripts (Weapons, Projectiles, etc.) from ECS implementation details.
    /// </summary>
    public static class EnemyBridge
    {
        /// <summary>
        /// Deals damage to all enemies within an area of effect, creates experience orbs, and destroys entities.
        /// </summary>
        /// <param name="pPosition">Center of the impact area in world coordinates.</param>
        /// <param name="pRadius">Radius of the impact circle.</param>
        /// <param name="pDamage">Damage amount (reserved for health system).</param>
        /// <returns>Number of enemy entities hit within the radius.</returns>
        public static int DealDamage(Vector3 pPosition, float pRadius, float pDamage)
        {
            var lWorld = World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return 0;

            var lHashSystemHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            if (lHashSystemHandle == SystemHandle.Null) return 0;

            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashSystemHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return 0;

            var lEcbSystem = lWorld.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            var lEcb = lEcbSystem.CreateCommandBuffer();
            var lEntityManager = lWorld.EntityManager;

            float lCellSize = EnemyHashSystem.CellSize;
            float lSqrRadius = pRadius * pRadius;
            float3 lPos = new float3(pPosition.x, pPosition.y, 0f);

            int2 lCenterCell = new int2(
                (int)math.floor(pPosition.x / lCellSize),
                (int)math.floor(pPosition.y / lCellSize)
            );

            int lCellRange = math.max(1, (int)math.ceil(pRadius / lCellSize));
            int lHitCount = 0;

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCheckCell = lCenterCell + new int2(x, y);

                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCheckCell, out EnemyGridData lData, out var lIt))
                    {
                        do
                        {
                            float3 lEnemyPos = lData.mPosition;
                            lEnemyPos.z = 0f;

                            if (math.lengthsq(lEnemyPos - lPos) <= lSqrRadius)
                            {
                                // Skip enemies already destroyed or already queued for destruction this frame
                                if (lEntityManager.Exists(lData.mEntity)
                                    && !lEntityManager.IsComponentEnabled<EnemyDeadTag>(lData.mEntity))
                                {
                                    lEntityManager.SetComponentEnabled<EnemyDeadTag>(lData.mEntity, true);
                                    Entity lXpEntity = lEcb.CreateEntity();
                                    lEcb.AddComponent(lXpEntity, new SpawnExperienceTag { mPosition = lEnemyPos });
                                    lEcb.DestroyEntity(lData.mEntity);
                                    lHitCount++;
                                }
                            }
                        }
                        while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref lIt));
                    }
                }
            }

            return lHitCount;
        }

        /// <summary>
        /// Queries the spatial hash grid to find the nearest enemy position within a maximum search radius.
        /// </summary>
        /// <param name="pFrom">Origin point of the search in world coordinates.</param>
        /// <param name="pMaxRadius">Maximum distance to search for an enemy.</param>
        /// <param name="pTargetPosition">Outputs the world position of the nearest enemy if found.</param>
        /// <returns>True if an enemy was found within range, false otherwise.</returns>
        public static bool TryGetClosestEnemy(Vector3 pFrom, float pMaxRadius, out Vector3 pTargetPosition)
        {
            pTargetPosition = Vector3.zero;

            var lWorld = World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return false;

            var lHashHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            if (lHashHandle == SystemHandle.Null) return false;

            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return false;

            float lCellSize = EnemyHashSystem.CellSize;
            float3 lFrom = new float3(pFrom.x, pFrom.y, 0f);

            int2 lCenterCell = new int2(
                (int)math.floor(pFrom.x / lCellSize),
                (int)math.floor(pFrom.y / lCellSize)
            );

            float lBestSqr = pMaxRadius * pMaxRadius;
            bool lFound = false;
            int lCellRange = math.max(1, (int)math.ceil(pMaxRadius / lCellSize));

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCell = lCenterCell + new int2(x, y);

                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCell, out EnemyGridData lData, out var lIt))
                    {
                        do
                        {
                            float3 lEnemyPos = lData.mPosition;
                            lEnemyPos.z = 0f;

                            float lSqr = math.lengthsq(lEnemyPos - lFrom);
                            if (lSqr < lBestSqr)
                            {
                                lBestSqr = lSqr;
                                pTargetPosition = new Vector3(lData.mPosition.x, lData.mPosition.y, 0f);
                                lFound = true;
                            }
                        }
                        while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref lIt));
                    }
                }
            }

            return lFound;
        }

        /// <summary>
        /// Checks whether any active enemy entity intersects with a given circle in world space.
        /// </summary>
        /// <param name="pPosition">Center of the query circle.</param>
        /// <param name="pRadius">Radius of the query circle.</param>
        /// <returns>True if at least one enemy is within the circle, false otherwise.</returns>
        public static bool CheckCollision(Vector3 pPosition, float pRadius)
        {
            var lWorld = World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return false;

            var lHashHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            if (lHashHandle == SystemHandle.Null) return false;

            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return false;

            float lCellSize = EnemyHashSystem.CellSize;
            float3 lPos = new float3(pPosition.x, pPosition.y, 0f);
            float lSqrRadius = pRadius * pRadius;

            int2 lCenterCell = new int2(
                (int)math.floor(pPosition.x / lCellSize),
                (int)math.floor(pPosition.y / lCellSize)
            );

            int lCellRange = math.max(1, (int)math.ceil(pRadius / lCellSize));

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCell = lCenterCell + new int2(x, y);

                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCell, out EnemyGridData lData, out var lIt))
                    {
                        do
                        {
                            float3 lEnemyPos = lData.mPosition;
                            lEnemyPos.z = 0f;

                            if (math.lengthsq(lEnemyPos - lPos) <= lSqrRadius)
                            {
                                return true;
                            }
                        }
                        while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref lIt));
                    }
                }
            }

            return false;
        }
    }
}
