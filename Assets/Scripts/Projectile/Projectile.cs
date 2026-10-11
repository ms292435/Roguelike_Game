using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Abstract base class for projectiles fired by the player.
    /// Handles movement, collision detection with enemies, and object pooling.
    /// Uses the EnemyBridge facade for decoupled enemy collision queries and damage.
    /// </summary>
    public abstract class Projectile : MonoBehaviour
    {
        [Header("Base Settings")]
        public float mSpeed = 10f;
        public float mHitRadius = 0.5f;
        public GameObject OriginPrefab { get; set; }
        private Vector3 mDirection;
        private Vector3 mSpawnPosition;
        private float mDamage;

        /// <summary>
        /// Virtual hook called when projectile hits an enemy.
        /// Subclasses can override to implement custom hit effects.
        /// </summary>
        protected virtual void OnHit() { }

        /// <summary>
        /// Initializes projectile with direction and damage values.
        /// Sets up initial position, rotation, and configures fire direction.
        /// </summary>
        /// <param name="pDirection">Fire direction vector (will be normalized).</param>
        /// <param name="pDamage">Damage value to deal to enemies on hit.</param>
        public void Init(Vector3 pDirection, float pDamage)
        {
            // Normalize direction vector for consistent movement speed
            mDirection = pDirection.normalized;

            // Store damage value for collision handling
            mDamage = pDamage;

            // Record spawn position for distance-based despawn
            mSpawnPosition = Player.Instance.mCurrentPosition;

            // Calculate rotation angle from direction vector
            float lAngle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;

            // Apply rotation so projectile visually faces fire direction
            transform.rotation = Quaternion.Euler(0, 0, lAngle);
        }

        private void Move()
        {
            // Move projectile in fire direction at configured speed
            transform.position += mSpeed * Time.deltaTime * mDirection;
        }

        /// <summary>
        /// Checks for collisions with enemies and despawn conditions each frame.
        /// Queries the EnemyBridge facade for efficient hit detection and damage application.
        /// </summary>
        private void CheckCollision()
        {
            var lCurrentPosition = transform.position;

            // Deal damage to all enemies in hit radius via the facade and retrieve hit count in a single query
            int lHits = DOTS.EnemyBridge.DealDamage(lCurrentPosition, mHitRadius, mDamage);

            // --- Distance-Based Despawn Check ---
            // Return projectile to pool if it travels beyond maximum range
            if (Vector3.Distance(lCurrentPosition, mSpawnPosition) > 20f)
            {
                ReturnToPool();
                return;
            }

            // Despawn projectile and invoke hit hook if collision occurred
            if (lHits > 0)
            {
                OnHit();
                ReturnToPool();
            }
        }

        /// <summary>
        /// Returns this projectile to the object pool for reuse.
        /// Prevents allocation/deallocation overhead by reusing projectile instances.
        /// </summary>
        private void ReturnToPool()
        {
            ProjectileManager.Instance.ReturnToPool(this);
        }

        /// <summary>
        /// Unity Update loop called every frame.
        /// Handles projectile movement and collision checks each frame.
        /// </summary>
        void Update()
        {
            // Move projectile along its fire direction
            Move();

            // Check for collisions and despawn conditions
            CheckCollision();
        }
    }
}