using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    public GameObject mEnemyPrefab;
    public int EnemyPoolCount = 1000;

    private Stack<Enemy> mInactives = new Stack<Enemy>();
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
        return null;
    }

    public void ReturnEnemy(Enemy pEnemy)
    {
        pEnemy.gameObject.SetActive(false);
        int lIndex = mActiveEnemiesList.IndexOf(pEnemy);
        if (lIndex != -1)
        {
            int lastIndex = mActiveEnemiesList.Count - 1;
            mActiveEnemiesList[lIndex] = mActiveEnemiesList[lastIndex];
            mActiveEnemiesList.RemoveAt(lastIndex);
        }
        mInactives.Push(pEnemy);
    }

    public int GetActiveEnemyCount()
    {
        return mActiveEnemiesList.Count;
    }

    public void Update()
    {
        for (int i = 0; i < mActiveEnemiesList.Count; i++)
        {
            mActiveEnemiesList[i].Tick();
        }
    }
}