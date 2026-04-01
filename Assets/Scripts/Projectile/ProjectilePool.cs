using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    public GameObject mProjectilePrefab;
    public int mPoolSize = 200;

    private List<Projectile> mPool = new List<Projectile>();

    public static ProjectilePool Instance { get; private set; }

    private void Awake()
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
            mPool.Add(lProjectile);

        }
    }

    public Projectile GetProjectile()
    {
        foreach (Projectile lProjectile in mPool)
        {
            if (!lProjectile.gameObject.activeInHierarchy)
            {
                lProjectile.gameObject.SetActive(true);
                return lProjectile;
            }
        }

        // optionnel : expand pool si plein
        Projectile lNewProjectile = Instantiate(mProjectilePrefab).GetComponent<Projectile>();
        mPool.Add(lNewProjectile);
        return lNewProjectile;
    }

    public void ReturnProjectile(Projectile pProjectile)
    {
        pProjectile.gameObject.SetActive(false);
    }
}