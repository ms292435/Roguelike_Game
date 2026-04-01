using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public GameObject mEnemyPrefab;
    public int EnemyPoolCount = 1000;

    // On utilise uniquement la Stack pour les inactifs (O(1))
    private Stack<Enemy> mInactives = new Stack<Enemy>();
    // On garde cette liste uniquement si tu as besoin d'itérer sur les ennemis actifs hors SpatialGrid
    public List<Enemy> mActiveEnemiesList = new List<Enemy>();

    public static EnemyPool Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < EnemyPoolCount; i++)
        {
            Enemy lEnemy = Instantiate(mEnemyPrefab).GetComponent<Enemy>();
            lEnemy.gameObject.SetActive(false);
            mInactives.Push(lEnemy);
        }
    }

    public Enemy GetEnemy()
    {
        if (mInactives.Count > 0)
        {
            Enemy lEnemy = mInactives.Pop();
            lEnemy.gameObject.SetActive(true);
            mActiveEnemiesList.Add(lEnemy); 
            return lEnemy;
        }
        Debug.LogWarning("Pool d'ennemis vide !");
        return null;
    }

    public void ReturnEnemy(Enemy pEnemy)
    {
        pEnemy.gameObject.SetActive(false);
        mActiveEnemiesList.Remove(pEnemy);
        mInactives.Push(pEnemy); // Retour immédiat dans la Stack (O(1))
    }

    public int GetActiveEnemyCount()
    {
        return mActiveEnemiesList.Count;
    }

    // Cette méthode devient obsolète si tu utilises la SpatialGrid, 
    // mais gardons-la au cas où pour du debug.
    public Enemy GetClosestEnemy(Vector3 pPosition)
    {
        Enemy lClosest = null;
        float lMinDistSq = Mathf.Infinity;

        for (int i = 0; i < mActiveEnemiesList.Count; i++)
        {
            float lDistSq = (pPosition - mActiveEnemiesList[i].transform.position).sqrMagnitude;
            if (lDistSq < lMinDistSq)
            {
                lMinDistSq = lDistSq;
                lClosest = mActiveEnemiesList[i];
            }
        }
        return lClosest;
    }
}