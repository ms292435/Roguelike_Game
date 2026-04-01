using UnityEngine;

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
        float lAngle = Mathf.Atan2(mDirection.y, mDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, lAngle);
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
        Enemy lClosestEnemy = SpatialGrid.Instance.GetClosestEnemyInGrid(transform.position);

        // 2. Si aucun ennemi n'est dans le secteur, on arrête
        if (lClosestEnemy == null) return;

        // 3. Test de distance (Optimisé avec sqrMagnitude pour éviter la racine carrée)
        float lSqrDist = (transform.position - lClosestEnemy.transform.position).sqrMagnitude;

        // On compare avec le rayon de collision au carré
        if (lSqrDist < mHitRadius * mHitRadius)
        {
            lClosestEnemy.TakeDamage(mDamage);
        }

        if ( transform.position.magnitude > 20f) // Si le projectile sort d'une zone raisonnable, on le retourne au pool
        {
            ReturnToPool();
        }
    }

    void ReturnToPool()
    {
        ProjectilePool.Instance.ReturnProjectile(this);
    }
}