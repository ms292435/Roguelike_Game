using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class SpatialGrid : MonoBehaviour
    {
        public float mCellSize = 1f;
        public static SpatialGrid Instance { get; private set; }

        private readonly Dictionary<Vector2Int, HashSet<Enemy>> mGrid = new();
        private readonly Dictionary<Vector2Int, HashSet<Experience>> mExperienceGrid = new();

        public Vector2Int GetGridPos(Vector3 pWorldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(pWorldPos.x / mCellSize),Mathf.FloorToInt(pWorldPos.y / mCellSize));
        }

        public void UpdateEnemyPosition(Enemy pEnemy, Vector3 pOldPos, Vector3 pNewPos)
        {
            Vector2Int lOldGridPos = GetGridPos(pOldPos);
            Vector2Int lNewGridPos = GetGridPos(pNewPos);

            if (lOldGridPos != lNewGridPos)
            {
                RemoveEnemy(pEnemy, lOldGridPos);
                AddEnemy(pEnemy, lNewGridPos);
            }
        }

        public void AddEnemy(Enemy pEnemy, Vector2Int pGridPos)
        {
            if (!mGrid.ContainsKey(pGridPos))
            {
                mGrid[pGridPos] = new HashSet<Enemy>();
            }
            mGrid[pGridPos].Add(pEnemy);
        }

        public void RemoveEnemy(Enemy pEnemy, Vector2Int pGridPos)
        {
            if (mGrid.TryGetValue(pGridPos, out HashSet<Enemy> lCell))
            {
                lCell.Remove(pEnemy);

                if (lCell.Count == 0)
                {
                    mGrid.Remove(pGridPos);
                }
            }
        }

        public Enemy GetClosestEnemyInGrid(Vector3 pPosition, int pMaxCellRadius = 5)
        {
            Vector2Int lCenterCell = GetGridPos(pPosition);
            Enemy lClosest = null;
            float lMinDistSqr = Mathf.Infinity;

            for (int lLayer = 0; lLayer <= pMaxCellRadius; lLayer++)
            {
                // Look for best candidate in the current layer
                Enemy lCandidate = GetClosestInLayer(pPosition, lCenterCell, lLayer, ref lMinDistSqr);

                if (lCandidate != null)
                    lClosest = lCandidate;

                // If we found a candidate and it's closer than the next layer's minimum distance, we can stop searching
                if (lClosest != null && IsCloserThanNextLayer(lMinDistSqr, lLayer))
                    return lClosest;
            }

            return lClosest;
        }

        public HashSet<Enemy> GetEnemiesInRadius(Vector3 pPosition, float pRadius)
        {
            HashSet<Enemy> lResult = new();
            Vector2Int lCenterCell = GetGridPos(pPosition);
            float lSqrRadius = pRadius * pRadius;

            // On calcule combien de cellules on doit vérifier autour du centre
            int lCellRange = Mathf.CeilToInt(pRadius / mCellSize);

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    // On récupère les ennemis de la cellule via ta méthode existante
                    HashSet<Enemy> lCell = GetEnemiesInCell(lCenterCell.x + x, lCenterCell.y + y);

                    if (lCell != null)
                    {
                        foreach (Enemy lEnemy in lCell)
                        {
                            // On vérifie la distance réelle entre le point et l'ennemi
                            float lDistSqr = (pPosition - lEnemy.mCurrentPosition).sqrMagnitude;

                            if (lDistSqr <= lSqrRadius)
                            {
                                lResult.Add(lEnemy);
                            }
                        }
                    }
                }
            }

            return lResult;
        }

        public HashSet<Enemy> GetEnemiesInCell(int pX, int pY)
        {
            if (mGrid.TryGetValue(new Vector2Int(pX, pY), out HashSet<Enemy> lEnemies))
            {
                return lEnemies;
            }
            return lEnemies; // May be null
        }

        public void AddExperience(Experience pExp, Vector2Int pGridPos)
        {
            if (!mExperienceGrid.ContainsKey(pGridPos)) mExperienceGrid[pGridPos] = new HashSet<Experience>();
            mExperienceGrid[pGridPos].Add(pExp);
        }

        public void RemoveExperience(Experience pExp, Vector2Int pGridPos)
        {
            if (mExperienceGrid.TryGetValue(pGridPos, out HashSet<Experience> lCell))
            {
                lCell.Remove(pExp);
                if (lCell.Count == 0)
                {
                    mExperienceGrid.Remove(pGridPos);
                }
            }
        }

        public void GetNearbyExperience(Vector3 pPosition, float pRadius, List<Experience> pResultList)
        {
            pResultList.Clear();
            Vector2Int lCenterCell = GetGridPos(pPosition);
            float lSqrRadius = pRadius * pRadius;

            int lCellRange = Mathf.CeilToInt(pRadius / mCellSize);

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    if (mExperienceGrid.TryGetValue(lCenterCell + new Vector2Int(x, y), out var lCell))
                    {
                        foreach (var exp in lCell)
                        {
                            if ((pPosition - exp.mCurrentPosition).sqrMagnitude < lSqrRadius)
                            {
                                pResultList.Add(exp);
                            }
                        }
                    }
                }
            }
        }

        private Enemy GetClosestInLayer(Vector3 pOrigin, Vector2Int pCenter, int pLayer, ref float pMinDistSqr)
        {
            Enemy lBestInLayer = null;

            for (int x = -pLayer; x <= pLayer; x++)
            {
                for (int y = -pLayer; y <= pLayer; y++)
                {
                    // Only check the cells that are exactly on the current layer's border (except layer 0 which is the center cell)
                    if (pLayer > 0 && Mathf.Abs(x) != pLayer && Mathf.Abs(y) != pLayer)
                        continue;

                    Vector2Int lCell = pCenter + new Vector2Int(x, y);
                    Enemy lFound = GetClosestInCell(pOrigin, lCell, ref pMinDistSqr);

                    if (lFound != null)
                        lBestInLayer = lFound;
                }
            }
            return lBestInLayer;
        }

        private Enemy GetClosestInCell(Vector3 pOrigin, Vector2Int pCell, ref float pMinDistSqr)
        {
            if (!mGrid.TryGetValue(pCell, out HashSet<Enemy> lEnemies))
                return null;

            Enemy lClosest = null;
            foreach (Enemy lEnemy in lEnemies)
            {
                float lDistSqr = (pOrigin - lEnemy.mCurrentPosition).sqrMagnitude;
                if (lDistSqr < pMinDistSqr)
                {
                    pMinDistSqr = lDistSqr;
                    lClosest = lEnemy;
                }
            }
            return lClosest;
        }

        private bool IsCloserThanNextLayer(float pMinDistSqr, int pLayer)
        {
            float lDistToNextLayer = (pLayer + 1) * mCellSize;
            return pMinDistSqr < (lDistToNextLayer * lDistToNextLayer);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mGrid.Clear();
            mExperienceGrid.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}