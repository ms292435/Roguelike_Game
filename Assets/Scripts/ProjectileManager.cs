using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        private readonly Dictionary<GameObject, Queue<Projectile>> mPools = new();

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

        public void ReturnToPool(Projectile pProjectile)
        {
            pProjectile.gameObject.SetActive(false);
            mPools[pProjectile.OriginPrefab].Enqueue(pProjectile);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            mPools.Clear();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}