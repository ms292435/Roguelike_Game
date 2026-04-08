using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class EnemyPool : MonoBehaviour
    {
        public GameObject mEnemyPrefab;

        public int EnemyPoolCount = 1000;

        public static EnemyPool Instance { get; private set; }

        private readonly Stack<Enemy> mInactives = new();

        private readonly List<Enemy> mActiveEnemiesList = new();

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

        void Update()
        {
            for (int i = 0; i < mActiveEnemiesList.Count; i++)
            {
                mActiveEnemiesList[i].Tick();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            for (int i = 0; i < EnemyPoolCount; i++)
            {
                Enemy lEnemy = Instantiate(mEnemyPrefab).GetComponent<Enemy>();
                lEnemy.gameObject.SetActive(false);
                mInactives.Push(lEnemy);
            }
        }
    }
}