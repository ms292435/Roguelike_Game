using UnityEngine;

namespace Roguelike
{
    public class EnemySpawner : MonoBehaviour
    {
        public AnimationCurve mDifficultyCurve;

        public Transform mTarget;

        public float mBaseSpawnInterval = 2f;
        public int mBaseEnemiesPerWave = 1;

        private float mTimer = 0f;
        private float mGameTimer = 0f;

        void Update()
        {
            mGameTimer += Time.deltaTime;

            // Evaluate the difficulty curve to determine the current difficulty level
            float lDifficulty = mDifficultyCurve.Evaluate(mGameTimer);

            float lCurrentInterval = mBaseSpawnInterval / lDifficulty;
            mTimer += Time.deltaTime;

            if (mTimer >= lCurrentInterval)
            {
                mTimer = 0f;

                // Compute the number of enemies to spawn based on the difficulty curve
                int lCount = Mathf.FloorToInt(mBaseEnemiesPerWave * lDifficulty);

                Vector3 lClusterCenter = CalculateSpawnPosition();

                for (int i = 0; i < lCount; i++)
                {
                    if (EnemyPool.Instance.GetActiveEnemyCount() < EnemyPool.Instance.EnemyPoolCount)
                    {
                        Enemy lEnemy = EnemyPool.Instance.GetEnemy();
                        if (lEnemy != null)
                        {
                            Vector3 lOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                            lEnemy.transform.position = lClusterCenter + lOffset;
                            lEnemy.Init(mTarget);
                        }
                    }
                }
            }
        }

        static private Vector3 CalculateSpawnPosition()
        {
            int lSide = UnityEngine.Random.Range(0, 4);

            Camera lCam = Camera.main;

            Vector3 lCamPos = lCam.transform.position;

            float lSpawnX, lSpawnY;
            float lHeight = lCam.orthographicSize;
            float lWidth = lHeight * lCam.aspect;
            float lMargin = 2f;

            switch (lSide)
            {
                case 0: // UP 
                    lSpawnX = UnityEngine.Random.Range(-lWidth, lWidth);
                    lSpawnY = lHeight + lMargin;
                    break;
                case 1: // DOWN
                    lSpawnX = UnityEngine.Random.Range(-lWidth, lWidth);
                    lSpawnY = -lHeight - lMargin;
                    break;
                case 2: // LEFT
                    lSpawnX = -lWidth - lMargin;
                    lSpawnY = UnityEngine.Random.Range(-lHeight, lHeight);
                    break;
                default: // RIGHT
                    lSpawnX = lWidth + lMargin;
                    lSpawnY = UnityEngine.Random.Range(-lHeight, lHeight);
                    break;
            }

            return new Vector3(lCamPos.x + lSpawnX, lCamPos.y + lSpawnY, 0);
        }
    }
}