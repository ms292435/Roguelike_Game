using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Architectural Bridge / Facade between high-level MonoBehaviour gameplay systems and DOTS/ECS.
    /// Encapsulates all unmanaged ECS queries and spatial grid traversals,
    /// shielding gameplay scripts (Weapons, Projectiles, etc.) from ECS implementation details.
    /// </summary>
    public static class EnemyBridge
    {
        /// <summary>
        /// Kills all enemies within an area of effect by enabling their EnemyDeadTag.
        /// The EnemyDeathSystem destroys them and drops their experience orbs at the start of the simulation.
        /// </summary>
        /// <param name="pPosition">Center of the impact area in world coordinates.</param>
        /// <param name="pRadius">Radius of the impact circle.</param>
        /// <param name="pDamage">Damage amount (reserved for health system).</param>
        /// <returns>Number of enemy entities hit within the radius.</returns>
        public static int DealDamage(Vector3 pPosition, float pRadius, float pDamage)
        {
            if (!TryGetGrid(out EnemySpatialGrid lGrid, out EntityManager lEntityManager)) return 0;

            float3 lPos = new float3(pPosition.x, pPosition.y, 0f);
            float lSqrRadius = pRadius * pRadius;
            int2 lCenterCell = EnemySpatialGrid.GetCell(lPos, EnemyHashSystem.CellSize);
            int lCellRange = math.max(1, (int)math.ceil(pRadius / EnemyHashSystem.CellSize));
            int lHitCount = 0;

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCell = lCenterCell + new int2(x, y);
                    lGrid.GetCellRange(lCell, out int lStart, out int lEnd);

                    for (int i = lStart; i < lEnd; i++)
                    {
                        EnemyGridData lData = lGrid.mEntries[i];

                        // Skip enemies of other cells sharing the bucket
                        if (!math.all(lData.mCell == lCell)) continue;

                        float3 lEnemyPos = lData.mPosition;
                        lEnemyPos.z = 0f;

                        if (math.lengthsq(lEnemyPos - lPos) <= lSqrRadius)
                        {
                            // Skip enemies already destroyed or already dead this frame
                            if (lEntityManager.Exists(lData.mEntity)
                                && !lEntityManager.IsComponentEnabled<EnemyDeadTag>(lData.mEntity))
                            {
                                lEntityManager.SetComponentEnabled<EnemyDeadTag>(lData.mEntity, true);
                                lHitCount++;
                            }
                        }
                    }
                }
            }

            return lHitCount;
        }

        /// <summary>
        /// Queries the spatial grid to find the nearest enemy position within a maximum search radius.
        /// </summary>
        /// <param name="pFrom">Origin point of the search in world coordinates.</param>
        /// <param name="pMaxRadius">Maximum distance to search for an enemy.</param>
        /// <param name="pTargetPosition">Outputs the world position of the nearest enemy if found.</param>
        /// <returns>True if an enemy was found within range, false otherwise.</returns>
        public static bool TryGetClosestEnemy(Vector3 pFrom, float pMaxRadius, out Vector3 pTargetPosition)
        {
            pTargetPosition = Vector3.zero;
            if (!TryGetGrid(out EnemySpatialGrid lGrid, out _)) return false;

            float3 lFrom = new float3(pFrom.x, pFrom.y, 0f);
            int2 lCenterCell = EnemySpatialGrid.GetCell(lFrom, EnemyHashSystem.CellSize);
            int lCellRange = math.max(1, (int)math.ceil(pMaxRadius / EnemyHashSystem.CellSize));
            float lBestSqr = pMaxRadius * pMaxRadius;
            bool lFound = false;

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCell = lCenterCell + new int2(x, y);
                    lGrid.GetCellRange(lCell, out int lStart, out int lEnd);

                    for (int i = lStart; i < lEnd; i++)
                    {
                        EnemyGridData lData = lGrid.mEntries[i];

                        // Skip enemies of other cells sharing the bucket
                        if (!math.all(lData.mCell == lCell)) continue;

                        float3 lEnemyPos = lData.mPosition;
                        lEnemyPos.z = 0f;

                        float lSqr = math.lengthsq(lEnemyPos - lFrom);
                        if (lSqr < lBestSqr)
                        {
                            lBestSqr = lSqr;
                            pTargetPosition = new Vector3(lEnemyPos.x, lEnemyPos.y, 0f);
                            lFound = true;
                        }
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
            if (!TryGetGrid(out EnemySpatialGrid lGrid, out _)) return false;

            float3 lPos = new float3(pPosition.x, pPosition.y, 0f);
            float lSqrRadius = pRadius * pRadius;
            int2 lCenterCell = EnemySpatialGrid.GetCell(lPos, EnemyHashSystem.CellSize);
            int lCellRange = math.max(1, (int)math.ceil(pRadius / EnemyHashSystem.CellSize));

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    int2 lCell = lCenterCell + new int2(x, y);
                    lGrid.GetCellRange(lCell, out int lStart, out int lEnd);

                    for (int i = lStart; i < lEnd; i++)
                    {
                        EnemyGridData lData = lGrid.mEntries[i];

                        // Skip enemies of other cells sharing the bucket
                        if (!math.all(lData.mCell == lCell)) continue;

                        float3 lEnemyPos = lData.mPosition;
                        lEnemyPos.z = 0f;

                        if (math.lengthsq(lEnemyPos - lPos) <= lSqrRadius)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves the spatial grid built by the EnemyHashSystem, ready to be read on the main thread.
        /// </summary>
        /// <param name="pGrid">The spatial grid, valid until the next simulation update.</param>
        /// <param name="pEntityManager">The entity manager of the default world.</param>
        /// <returns>False if the world or the grid is not available, or if the grid is empty.</returns>
        private static bool TryGetGrid(out EnemySpatialGrid pGrid, out EntityManager pEntityManager)
        {
            pGrid = default;
            pEntityManager = default;

            var lWorld = World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return false;

            var lHashSystemHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<EnemyHashSystem>();
            if (lHashSystemHandle == SystemHandle.Null) return false;

            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<EnemyHashSystem>(lHashSystemHandle);

            // The grid jobs are not waited for during their frame: complete them before reading on the main thread
            lHashSystem.mGridJobHandle.Complete();

            pGrid = lHashSystem.mGrid;
            pEntityManager = lWorld.EntityManager;
            return pGrid.mCount > 0;
        }
    }
}
