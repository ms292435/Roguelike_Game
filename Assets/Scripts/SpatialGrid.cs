using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{

    public float mCellSize = 4f;

    private Dictionary<Vector2Int, HashSet<Enemy>> mGrid = new Dictionary<Vector2Int, HashSet<Enemy>>();
    private Dictionary<Vector2Int, HashSet<Experience>> mExperienceGrid = new Dictionary<Vector2Int, HashSet<Experience>>();
    public static SpatialGrid Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public Vector2Int GetGridPos(Vector3 pWorldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(pWorldPos.x / mCellSize),
            Mathf.FloorToInt(pWorldPos.y / mCellSize)
        );
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
            mGrid[pGridPos] = new HashSet<Enemy>();

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

    public Enemy GetClosestEnemyInGrid(Vector3 pPosition, float pMaxCellRadius = 5)
    {
        Vector2Int lCenterCell = GetGridPos(pPosition);
        Enemy lClosest = null;
        float lMinDistSqr = Mathf.Infinity;

        for (int lLayer = 0; lLayer <= pMaxCellRadius; lLayer++)
        {
            bool lFoundSomethingInThisLayer = false;

            for (int x = -lLayer; x <= lLayer; x++)
            {
                for (int y = -lLayer; y <= lLayer; y++)
                {
                    if (lLayer > 0 && Mathf.Abs(x) != lLayer && Mathf.Abs(y) != lLayer) continue;

                    Vector2Int lCell = lCenterCell + new Vector2Int(x, y);
                    if (mGrid.TryGetValue(lCell, out HashSet<Enemy> lEnemiesInCell))
                    {
                        foreach (Enemy lEnemy in lEnemiesInCell)
                        {
                            float lDistSqr = (pPosition - lEnemy.mCurrentPosition).sqrMagnitude;
                            if (lDistSqr < lMinDistSqr)
                            {
                                lMinDistSqr = lDistSqr;
                                lClosest = lEnemy;
                                lFoundSomethingInThisLayer = true;
                            }
                        }
                    }
                }
            }

            if (lFoundSomethingInThisLayer)
            {
                float lDistanceToNextLayer = (lLayer + 1) * mCellSize;
                if (lMinDistSqr < lDistanceToNextLayer * lDistanceToNextLayer)
                {
                    return lClosest;
                }
            }
        }
        return lClosest;
    }

    public HashSet<Enemy> GetEnemiesInCell(int pX, int pY)
    {
        if (mGrid.TryGetValue(new Vector2Int(pX, pY), out HashSet<Enemy> lEnemies))
        {
            return lEnemies;
        }
        return null;
    }

    public void UpdateExperiencePosition(Experience pExp, Vector3 pOldPos, Vector3 pNewPos)
    {
        Vector2Int lOldGridPos = GetGridPos(pOldPos);
        Vector2Int lNewGridPos = GetGridPos(pNewPos);
        if (lOldGridPos != lNewGridPos)
        {
            RemoveExperience(pExp, lOldGridPos);
            AddExperience(pExp, lNewGridPos);
        }
    }

    // 2. Ajoute les méthodes pour gérer l'expérience (copie de AddEnemy/RemoveEnemy)
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
        }
    }

    // 3. Correction de GetNearbyExperience (pour qu'elle renvoie une LISTE et pas une seule gemme)
    public void GetNearbyExperienceNonAlloc(Vector3 pPos, float pRadius, List<Experience> pResultList)
    {
        pResultList.Clear();
        Vector2Int lCenterCell = GetGridPos(pPos);
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
                        if ((pPos - exp.mCurrentPosition).sqrMagnitude < lSqrRadius)
                        {
                            pResultList.Add(exp);
                        }
                    }
                }
            }
        }
    }
}