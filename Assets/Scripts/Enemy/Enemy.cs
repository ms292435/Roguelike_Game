using System;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : Entity
{
    private Transform mTarget;

    private Vector3 mLastPos;
    private bool mIsDead = false;

    public void Init(Transform pTarget)
    {
        mTarget = pTarget;

        Health = 50f;
        Speed = 1f;

        float lRadius = 20f;
        float lAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        Vector3 lSpawnPos = mTarget.position + new Vector3(Mathf.Cos(lAngle), Mathf.Sin(lAngle), 0f) * lRadius;

        transform.position = lSpawnPos;

        mLastPos = transform.position;
        SpatialGrid.Instance.AddEnemy(this, SpatialGrid.Instance.GetGridPos(mLastPos));
    }

    public void OnEnable()
    {
        mIsDead = false;
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

    public void Update()
    {
        if (mTarget == null) return;

        Vector3 lDirection = (mTarget.position - transform.position).normalized;
        transform.position += lDirection * Speed * Time.deltaTime;

        // Mise à jour dans la grille
        SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPos, transform.position);
        mLastPos = transform.position;

        if (Vector3.Distance(transform.position, mTarget.position) < 0.5f)
        {
            Die();
        }

    }

}
