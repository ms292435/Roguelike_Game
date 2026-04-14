using UnityEngine;

namespace Roguelike
{
    public abstract class Projectile : MonoBehaviour
    {
        [Header("Base Settings")]
        public float mSpeed = 10f;
        public float mHitRadius = 0.5f;

        public GameObject OriginPrefab { get; set; }

        private Vector3 mDirection;
        private Vector3 mSpawnPosition;

        private float mDamage;


        private readonly float mRebounds = 0.0f;

        protected abstract void OnHit(Enemy pEnemy, float pDamage);

        public void Init(Vector3 pDirection, float pDamage)
        {
            mDirection = pDirection.normalized;
            mDamage = pDamage;
            mSpawnPosition = Player.Instance.mCurrentPosition;

            float lAngle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, lAngle);
        }

        private void Move()
        {
            transform.position += mSpeed * Time.deltaTime * mDirection;
        }

        private void CheckCollision()
        {
            var lCurrentPosition = transform.position;
            // Get closest enemy in the grid
            Enemy lClosestEnemy = SpatialGrid.Instance.GetClosestEnemyInGrid(lCurrentPosition);

            if (lClosestEnemy == null) return;

            // Test squared distance to avoid sqrt calculation
            float lSqrDist = (lCurrentPosition - lClosestEnemy.transform.position).sqrMagnitude;

            // If the closest enemy is within hit radius, apply damage
            if (lSqrDist < mHitRadius * mHitRadius)
            {
                OnHit(lClosestEnemy, mDamage);
                if (Mathf.Abs(mRebounds - 0.0f) < Mathf.Epsilon)
                {
                    ReturnToPool();
                }
            }

            // Return to pool if out of bounds
            if (Vector3.Distance(lCurrentPosition, mSpawnPosition) > 20f)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            ProjectileManager.Instance.ReturnToPool(this);
        }

        void Update()
        {
            Move();
            CheckCollision();
        }
    }
}