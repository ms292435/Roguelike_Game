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
    private float mSeparationStrength = 10f; // Force de la poussée

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
        Vector3 lPosition = transform.position;
        SpatialGrid lGrid = SpatialGrid.Instance;
        Vector2Int lGridPosition = lGrid.GetGridPos(lPosition);

        Vector3 lSeparationForce = Vector3.zero;
        float lSqrSeparationRadius = mSeparationRadius * mSeparationRadius;

        int lGx = lGridPosition.x;
        int lGy = lGridPosition.y;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                HashSet<Enemy> lNeighbors = lGrid.GetEnemiesInCell(lGx + x, lGy + y);

                if (lNeighbors == null) continue;

                foreach (Enemy lOther in lNeighbors)
                {
                    if (lOther == this) continue;

                    Vector3 lDiff = lPosition - lOther.transform.position;
                    float lSqrDist = lDiff.sqrMagnitude;

                    if (lSqrDist < lSqrSeparationRadius && lSqrDist > 0.0001f)
                    {
                        float lDist = Mathf.Sqrt(lSqrDist);
                        lSeparationForce += (lDiff / lDist) * (mSeparationRadius - lDist);
                    }
                }
            }
        }

        transform.position = lPosition + lSeparationForce * (mSeparationStrength * 4f) * Time.deltaTime;
    }

    public void Update()
    {
        if (mTarget == null) return;

        Vector3 lDirection = (mTarget.position - transform.position).normalized;
        transform.position += lDirection * Speed * Time.deltaTime;

        SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPos, transform.position);
        mLastPos = transform.position;

        if ((Time.frameCount + mInstanceUpdateOrder) % 4 == 0)
        {
            HandleSeparation();
        }
        
        float lTilt = Mathf.Sin((Time.time + mAnimationOffset) * mTiltSpeed) * mTiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, lTilt);

        if (Vector3.Distance(transform.position, mTarget.position) < 0.5f)
        {
            Die();
        }
    }
}
