using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public float mSpeed = 10f;
    public float mHitRadius = 0.5f;

    private Vector3 mDirection;
    private float mDamage;

    public void Init(Vector3 pDirection, float pDamage)
    {
        mDirection = pDirection.normalized;
        mDamage = pDamage;

        // Rotation du sprite
        float angle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        Move();
        CheckCollision();
    }

    void Move()
    {
        transform.position += mDirection * mSpeed * Time.deltaTime;
    }

    void CheckCollision()
    {
        // 1. On récupère l'UNIQUE ennemi le plus proche via la grille
        Enemy closestEnemy = SpatialGrid.Instance.GetClosestEnemyInGrid(transform.position);

        // 2. Si aucun ennemi n'est dans le secteur, on arrête
        if (closestEnemy == null) return;

        // 3. Test de distance (Optimisé avec sqrMagnitude pour éviter la racine carrée)
        float sqrDist = (transform.position - closestEnemy.transform.position).sqrMagnitude;

        // On compare avec le rayon de collision au carré
        if (sqrDist < mHitRadius * mHitRadius)
        {
            closestEnemy.TakeDamage(mDamage);
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        ProjectilePool.Instance.ReturnProjectile(this);
    }
}