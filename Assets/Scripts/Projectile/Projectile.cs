using Unity.Entities;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Abstract base class for projectiles fired by the player.
    /// Handles movement, collision detection with enemies, and object pooling.
    /// Uses the ECS spatial hash grid for efficient enemy collision queries.
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
        private readonly float mRebounds = 0.0f;

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
        /// Queries ECS spatial hash grid for efficient hit detection.
        /// </summary>
        private void CheckCollision()
        {
            var lCurrentPosition = transform.position;

            // Deal damage to all enemies in hit radius at current position
            DOTS.EnemyDamageProxy.DealDamage(lCurrentPosition, mHitRadius, mDamage);

            // --- Distance-Based Despawn Check ---
            // Return projectile to pool if it travels beyond maximum range
            if (Vector3.Distance(lCurrentPosition, mSpawnPosition) > 20f)
            {
                ReturnToPool();
                return;
            }

            // --- Rebound and Hit Detection Logic ---
            // Currently no bouncing (mRebounds = 0), but reserved for future expansion
            // Projectiles despawn after hitting an enemy or exceeding max distance
            if (Mathf.Abs(mRebounds - 0.0f) < Mathf.Epsilon)
            {
                // Check if projectile hit an enemy and return to pool
                // This uses the ECS spatial grid for efficient hit detection
                CheckHitAndReturn(lCurrentPosition);
            }
        }

        /// <summary>
        /// Checks for enemy collision using the ECS spatial hash grid.
        /// Returns projectile to pool if collision detected within hit radius.
        /// Uses cached EnemyHashSystem spatial grid for fast proximity queries.
        /// </summary>
        /// <param name="pPosition">Current projectile position for collision check.</param>
        private void CheckHitAndReturn(Vector3 pPosition)
        {
            // --- ECS World Access ---
            // Get reference to the default ECS world containing enemy entities
            var lWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return;

            // --- Spatial Hash System Access ---
            // Retrieve the EnemyHashSystem that maintains the spatial grid
            var lHashSystemHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<DOTS.EnemyHashSystem>();
            if (lHashSystemHandle == SystemHandle.Null) return;

            // Get unsafe reference to the hash system for direct grid access
            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<DOTS.EnemyHashSystem>(lHashSystemHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return;

            // --- Grid Cell Calculation ---
            // Size of each grid cell (must match EnemyHashSystem configuration)
            float lCellSize = DOTS.EnemyHashSystem.CellSize;
            
            // Convert world position to grid cell coordinates
            Unity.Mathematics.float3 lPos = pPosition;
            Unity.Mathematics.int2 lCell = new(
                (int)Mathf.Floor(pPosition.x / lCellSize),
                (int)Mathf.Floor(pPosition.y / lCellSize)
            );

            // Pre-calculate squared hit radius to avoid expensive square root in loop
            float lSqrRadius = mHitRadius * mHitRadius;

            // --- Neighboring Cell Traversal ---
            // Check all 9 cells (current + 8 neighbors) for enemy presence
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // Calculate neighbor cell coordinates
                    var lCheckCell = lCell + new Unity.Mathematics.int2(x, y);
                    
                    // Attempt to retrieve first enemy in this cell
                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCheckCell, out DOTS.EnemyGridData lData, out var it))
                    {
                        // Iterate through all enemies in this cell
                        do
                        {
                            // Get enemy position and normalize to 2D space
                            Unity.Mathematics.float3 lEp = lData.mPosition;
                            lEp.z = 0f;
                            lPos.z = 0f;
                            
                            // Check if enemy is within hit radius using squared distance
                            if (Unity.Mathematics.math.lengthsq(lEp - lPos) <= lSqrRadius)
                            {
                                // Hit detected: return projectile to pool
                                ReturnToPool();
                                return;
                            }
                        } while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref it));
                    }
                }
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