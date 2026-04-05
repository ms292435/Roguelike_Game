using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class ProjectilePool : MonoBehaviour
    {
        public GameObject mProjectilePrefab;

        public int mPoolSize = 200;
        
        public static ProjectilePool Instance { get; private set; }


        private readonly Queue<Projectile> mProjectileQueue = new();

        public Projectile GetProjectile()
        {
            Projectile lProjectile;

            if (mProjectileQueue.Count > 0)
            {
                lProjectile = mProjectileQueue.Dequeue();
            }
            else
            {
                var lObj = Instantiate(mProjectilePrefab, transform);
                lProjectile = lObj.GetComponent<Projectile>();
            }

            lProjectile.gameObject.SetActive(true);
            return lProjectile;
        }

        public void ReturnProjectile(Projectile pProjectile)
        {
            pProjectile.gameObject.SetActive(false);
            mProjectileQueue.Enqueue(pProjectile);
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
            for (int i = 0; i < mPoolSize; i++)
            {
                Projectile lProjectile = Instantiate(mProjectilePrefab).GetComponent<Projectile>();
                lProjectile.gameObject.SetActive(false);
                mProjectileQueue.Enqueue(lProjectile);
            }
        }
    }
}