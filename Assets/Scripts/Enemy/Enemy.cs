using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
    private Transform mTarget;

    private Vector3 mLastPos;
    private bool mIsDead = false;

    private float mAnimationOffset; // Pour désynchroniser les ennemis
    private float mTiltAngle = 5f;    // L'angle max du balancement
    private float mTiltSpeed = 5f;    // La vitesse du balancement

    private float mSeparationRadius = 1f; // Distance à laquelle ils commencent à se pousser
    private float mSeparationStrength = 5f; // Force de la poussée

    // Cache pour éviter de recalculer des propriétés répétitives
    private static int mGlobalUpdateIndex = 0;
    private int mInstanceUpdateOrder;
    public void Init(Transform pTarget)
    {
        mTarget = pTarget;

        Health = 50f;
        Speed = 0.7f;

        float lRadius = 20f;
        float lAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        Vector3 lSpawnPos = mTarget.position + new Vector3(Mathf.Cos(lAngle), Mathf.Sin(lAngle), 0f) * lRadius;

        transform.position = lSpawnPos;

        mLastPos = transform.position;
        SpatialGrid.Instance.AddEnemy(this, SpatialGrid.Instance.GetGridPos(mLastPos));

        mAnimationOffset = UnityEngine.Random.Range(0f, 10f);
    }

    public void OnEnable()
    {
        mIsDead = false;
        mInstanceUpdateOrder = mGlobalUpdateIndex++;
    }
    public void TakeDamage(float pDamage)
    {
        Health -= pDamage;

        if (Health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (mIsDead) return;
        SpatialGrid.Instance.RemoveEnemy(this, SpatialGrid.Instance.GetGridPos(transform.position));
        mIsDead = true;

        SpawnExperience();
        EnemyPool.Instance.ReturnEnemy(this);
    }

    void SpawnExperience()
    {
        if (ExperiencePool.Instance == null) return;

        Experience lExperience = ExperiencePool.Instance.GetExperience();
        if (lExperience != null)
        {
            lExperience.transform.position = transform.position;
        }
    }

    private void HandleSeparation()
    {
        Vector2Int lGridPosition = SpatialGrid.Instance.GetGridPos(transform.position);
        Vector3 lSeparationForce = Vector3.zero;
        float lSqrmSeparationRadius = mSeparationRadius * mSeparationRadius;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int lNeighborCell = new Vector2Int(lGridPosition.x + x, lGridPosition.y + y);
                // On récupère le HashSet via la nouvelle méthode de SpatialGrid
                HashSet<Enemy> lNeighbors = SpatialGrid.Instance.GetEnemiesInCell(lNeighborCell);

                if (lNeighbors == null) continue;

                // Obligé d'utiliser foreach avec un HashSet
                foreach (Enemy lOther in lNeighbors)
                {
                    if (lOther == this) continue;

                    Vector3 lDiff = transform.position - lOther.transform.position;
                    float lSqrDist = lDiff.sqrMagnitude;

                    // Optimisation : On compare les carrés pour éviter le Mathf.Sqrt
                    if (lSqrDist < lSqrmSeparationRadius && lSqrDist > 0.0001f)
                    {
                        // On normalise manuellement de façon optimisée
                        // La force est inversement proportionnelle à la distance
                        float lDist = Mathf.Sqrt(lSqrDist);
                        lSeparationForce += (lDiff / lDist) * (mSeparationRadius - lDist);
                    }
                }
            }
        }
        // Application de la force (x4 car exécuté 1 frame sur 4)
        transform.position += lSeparationForce * (mSeparationStrength * 4f) * Time.deltaTime;
    }

    public void Update()
    {
        if (mTarget == null) return;

        Vector3 lDirection = (mTarget.position - transform.position).normalized;
        transform.position += lDirection * Speed * Time.deltaTime;

        if ((Time.frameCount + mInstanceUpdateOrder) % 4 == 0)
        {
            HandleSeparation();
        }
        // Mise à jour dans la grille
        SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPos, transform.position);
        mLastPos = transform.position;

        // Animation de marche 
        float lTilt = Mathf.Sin((Time.time + mAnimationOffset) * mTiltSpeed) * mTiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, lTilt);

        if (Vector3.Distance(transform.position, mTarget.position) < 0.5f)
        {
            Die();
        }
    }
}
