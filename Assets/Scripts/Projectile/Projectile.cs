using UnityEngine;

namespace Roguelike
{
    public class Projectile : MonoBehaviour
    {
        public float mSpeed = 10f;
        public float mHitRadius = 0.5f;

        private Vector3 mDirection;

        private float mDamage;
        private float mSpawnPosition;

        public void Init(Vector3 pDirection, float pDamage)
        {
            mDirection = pDirection.normalized;
            mDamage = pDamage;
            mSpawnPosition = transform.position.magnitude;

            float lAngle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, lAngle);
        }

        private void Move()
        {
            transform.position += mSpeed * Time.deltaTime * mDirection;
        }

        private void CheckCollision()
        {
            // Get closest enemy in the grid
            Enemy lClosestEnemy = SpatialGrid.Instance.GetClosestEnemyInGrid(transform.position);

            if (lClosestEnemy == null) return;

            // Test squared distance to avoid sqrt calculation
            float lSqrDist = (transform.position - lClosestEnemy.transform.position).sqrMagnitude;

            // If the closest enemy is within hit radius, apply damage
            if (lSqrDist < mHitRadius * mHitRadius)
            {
                lClosestEnemy.TakeDamage(mDamage);
                ReturnToPool();
            }

            // Return to pool if out of bounds
            //if (transform.position.magnitude > mSpawnPosition + 20f)
            //{
            //    ReturnToPool();
            //}
        }

        private void ReturnToPool()
        {
            ProjectilePool.Instance.ReturnProjectile(this);
        }

        void Update()
        {
            Move();
            CheckCollision();
        }
    }
}