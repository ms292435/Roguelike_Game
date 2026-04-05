using UnityEngine;

namespace Roguelike
{
    public class EnemySpawner : MonoBehaviour
    {
        public Transform mTarget;

        public float mSpawnInterval = 0.5f;
        public float mEnemyPerTick = 1;

        private float mtimer = 0f;

        void Update()
        {
            mtimer += Time.deltaTime;

            if (mtimer >= mSpawnInterval)
            {
                mtimer = 0f;

                for (int i = 0; i < mEnemyPerTick; i++)
                {
                    if (EnemyPool.Instance.GetActiveEnemyCount() < EnemyPool.Instance.EnemyPoolCount)
                    {
                        Enemy lEnemy = EnemyPool.Instance.GetEnemy();
                        if (lEnemy != null)
                        {
                            lEnemy.Init(mTarget);
                        }
                    }
                }
            }
        }
    }
}