using System.Collections.Generic;
using UnityEngine;

public class SpatialGrid : MonoBehaviour
{

    public float cellSize = 16f; // Taille d'une case (ajuste selon la taille de tes sprites)

    // Dictionnaire associant une position de case (Vector2Int) à une liste d'ennemis
    private Dictionary<Vector2Int, List<Enemy>> grid = new Dictionary<Vector2Int, List<Enemy>>();

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
    public Vector2Int GetGridPos(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / cellSize),
            Mathf.FloorToInt(worldPos.y / cellSize)
        );
    }

    public void UpdateEnemyPosition(Enemy enemy, Vector3 oldPos, Vector3 newPos)
    {
        Vector2Int oldGridPos = GetGridPos(oldPos);
        Vector2Int newGridPos = GetGridPos(newPos);

        if (oldGridPos != newGridPos)
        {
            RemoveEnemy(enemy, oldGridPos);
            AddEnemy(enemy, newGridPos);
        }
    }

    public void AddEnemy(Enemy enemy, Vector2Int gridPos)
    {
        if (!grid.ContainsKey(gridPos))
            grid[gridPos] = new List<Enemy>();

        grid[gridPos].Add(enemy);
    }

    public void RemoveEnemy(Enemy enemy, Vector2Int gridPos)
    {
        if (grid.ContainsKey(gridPos))
        {
            grid[gridPos].Remove(enemy);
        }
    }

    // Récupère les ennemis dans la case actuelle et les 8 cases adjacentes
    public Enemy GetClosestEnemyInGrid(Vector3 position, float maxCellRadius = 5)
    {
        Vector2Int centerCell = GetGridPos(position);
        Enemy closest = null;
        float minDist = Mathf.Infinity;

        // On cherche par couches (0 = cellule actuelle, 1 = les 8 autour, etc.)
        for (int layer = 0; layer <= maxCellRadius; layer++)
        {
            bool foundInLayer = false;

            for (int x = -layer; x <= layer; x++)
            {
                for (int y = -layer; y <= layer; y++)
                {
                    // On ne vérifie que le périmètre de la couche actuelle
                    if (Mathf.Abs(x) != layer && Mathf.Abs(y) != layer) continue;

                    Vector2Int cell = centerCell + new Vector2Int(x, y);
                    if (grid.ContainsKey(cell))
                    {
                        foreach (Enemy enemy in grid[cell])
                        {
                            float dist = Vector2.Distance(position, enemy.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                closest = enemy;
                                foundInLayer = true;
                            }
                        }
                    }
                }
            }

            // Si on a trouvé un ennemi dans cette couche, on peut s'arrêter 
            // (car les couches suivantes sont forcément plus loin)
            if (foundInLayer) return closest;
        }

        return null;
    }
}