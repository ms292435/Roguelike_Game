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
            Projectile proj = Instantiate(mProjectilePrefab).GetComponent<Projectile>();
            proj.gameObject.SetActive(false);
            mPool.Add(proj);
        }
    }

    public Projectile GetProjectile()
    {
        foreach (Projectile proj in mPool)
        {
            if (!proj.gameObject.activeInHierarchy)
            {
                proj.gameObject.SetActive(true);
                return proj;
            }
        }

        // optionnel : expand pool si plein
        Projectile newProj = Instantiate(mProjectilePrefab).GetComponent<Projectile>();
        mPool.Add(newProj);
        return newProj;
    }

    public void ReturnProjectile(Projectile proj)
    {
        proj.gameObject.SetActive(false);
    }
}