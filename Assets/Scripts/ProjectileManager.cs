using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Singleton object pool manager for projectile instances.
    /// Manages independent queues per projectile prefab type to reuse instances without runtime allocations.
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<Projectile>> mPools = new();

        /// <summary>
        /// Retrieves an inactive pooled projectile or instantiates a new one if the pool is empty.
        /// </summary>
        /// <param name="pPrefab">The projectile prefab to fetch or instantiate.</param>
        /// <param name="pPosition">Initial spawn position.</param>
        /// <returns>An activated Projectile instance.</returns>
        public Projectile GetProjectile(GameObject pPrefab, Vector3 pPosition)
        {
            if (!mPools.ContainsKey(pPrefab))
            {
                mPools[pPrefab] = new Queue<Projectile>();
            }

            Projectile lProjectile;

            if (mPools[pPrefab].Count > 0)
            {
                lProjectile = mPools[pPrefab].Dequeue();
            }
            else
            {
                lProjectile = Instantiate(pPrefab, transform).GetComponent<Projectile>();
                // Inject reference to the prefab for returning to pool
                lProjectile.OriginPrefab = pPrefab;
            }

            lProjectile.transform.position = pPosition;
            lProjectile.gameObject.SetActive(true);
            return lProjectile;
        }

        /// <summary>
        /// Deactivates and enqueues a projectile back into its corresponding prefab pool.
        /// </summary>
        /// <param name="pProjectile">The projectile instance to recycle.</param>
        public void ReturnToPool(Projectile pProjectile)
        {
            pProjectile.gameObject.SetActive(false);
            mPools[pProjectile.OriginPrefab].Enqueue(pProjectile);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mPools.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}