using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{

    public float mCellSize = 4f; // Taille d'une case (ajuste selon la taille de tes sprites)

    // Dictionnaire associant une pPosition de case (Vector2Int) à une liste d'ennemis
    private Dictionary<Vector2Int, HashSet<Enemy>> mGrid = new Dictionary<Vector2Int, HashSet<Enemy>>();

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

    // Convertit une position du monde en coordonnées de grille
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

    // Récupère les ennemis dans la case actuelle et les 8 cases adjacentes
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
                        // Le foreach sur HashSet est efficace, mais attention aux allocations si appelé trop souvent
                        foreach (Enemy lEnemy in lEnemiesInCell)
                        {
                            float lDistSqr = (pPosition - lEnemy.transform.position).sqrMagnitude;
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

    public HashSet<Enemy> GetEnemiesInCell(Vector2Int pCellPos)
    {
        if (mGrid.TryGetValue(pCellPos, out HashSet<Enemy> lEnemies))
        {
            return lEnemies;
        }
        return null;
    }
}