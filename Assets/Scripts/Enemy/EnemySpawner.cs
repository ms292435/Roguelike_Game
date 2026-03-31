using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public Transform mTarget;
    public SpatialGrid mSpatialGrid;
    public float mSpawnInterval = 0.5f;
    public float mEnemyPerTick = 1;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= mSpawnInterval)
        {
            timer = 0f;

            for (int i = 0; i < mEnemyPerTick; i++)
            {
                if (EnemyPool.Instance.GetActiveEnemyCount() < EnemyPool.Instance.EnemyPoolCount)
                {
                    Enemy enemy = EnemyPool.Instance.GetEnemy();
                    if (enemy != null)
                    {
                        enemy.Init(mTarget);
                    }
                }
            }
        }
    }
}
