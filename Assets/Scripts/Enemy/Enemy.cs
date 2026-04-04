using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
    private Transform mTarget;

    private Vector3 mLastPos;
    private bool mIsDead = false;

    private float mAnimationOffset; // Desync ennemies
    private float mTiltAngle = 5f;    // Max angle of the tilt
    private float mTiltSpeed = 5f;    // Tilt speed

    private float mSeparationRadius = 1f; // Min distance to maintain from other enemies
    private float mSeparationStrength = 10f; // Push strength to maintain separation

    private static int mGlobalUpdateIndex = 0;
    private int mInstanceUpdateOrder;

    public Vector3 mCurrentPosition;
    private Vector3 mTargetPosition; 
    public void Init(Transform pTarget)
    {
        mTarget = pTarget;
        Health = 50f;
        Speed = 0.7f;

        Camera lCam = Camera.main;
        float lHeight = lCam.orthographicSize;
        float lWidth = lHeight * lCam.aspect;

        float lMargin = 2f;
        float lSpawnX, lSpawnY;

        int lSide = UnityEngine.Random.Range(0, 4);

        switch (lSide)
        {
            case 0:
                lSpawnX = UnityEngine.Random.Range(-lWidth, lWidth);
                lSpawnY = lHeight + lMargin;
                break;
            case 1:
                lSpawnX = UnityEngine.Random.Range(-lWidth, lWidth);
                lSpawnY = -lHeight - lMargin;
                break;
            case 2:
                lSpawnX = -lWidth - lMargin;
                lSpawnY = UnityEngine.Random.Range(-lHeight, lHeight);
                break;
            default:
                lSpawnX = lWidth + lMargin;
                lSpawnY = UnityEngine.Random.Range(-lHeight, lHeight);
                break;
        }

        Vector3 lRelativePos = new Vector3(lSpawnX, lSpawnY, 0);
        transform.position = lCam.transform.position + lRelativePos;
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f); 

        mLastPos = transform.position;
        SpatialGrid.Instance.AddEnemy(this, SpatialGrid.Instance.GetGridPos(mLastPos));
        mAnimationOffset = Random.Range(0f, 10f);
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
            lExperience.transform.position = this.transform.position;
            lExperience.Init();
        }
    }

    private void HandleSeparation()
    {
        SpatialGrid lGrid = SpatialGrid.Instance;
        Vector2Int lGridPosition = lGrid.GetGridPos(mCurrentPosition);

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

                    Vector3 lDiff = mCurrentPosition - lOther.mCurrentPosition;
                    float lSqrDist = lDiff.sqrMagnitude;

                    if (lSqrDist < lSqrSeparationRadius && lSqrDist > 0.0001f)
                    {
                        float lForce = 1f - (lSqrDist / lSqrSeparationRadius);
                        lSeparationForce += lDiff.normalized * lForce;
                    }
                }
            }
        }

        mCurrentPosition += lSeparationForce * (mSeparationStrength * 4f) * Time.deltaTime;
    }

    public void Tick()
    {
        if (mTarget == null) return;

        mCurrentPosition = transform.position;
        mTargetPosition = mTarget.position;

        Vector3 lDirection = (mTargetPosition - mCurrentPosition).normalized;
        mCurrentPosition += lDirection * Speed * Time.deltaTime;

        SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPos, mCurrentPosition);
        mLastPos = mCurrentPosition;

        if ((Time.frameCount + mInstanceUpdateOrder) % 4 == 0)
        {
            HandleSeparation();
        }

        transform.position = mCurrentPosition;

        float lTilt = Mathf.Sin((Time.time + mAnimationOffset) * mTiltSpeed) * mTiltAngle;
        transform.rotation = Quaternion.Euler(0, 0, lTilt);

        if ((mCurrentPosition - mTargetPosition).sqrMagnitude < 0.25f) // 0.5f * 0.5f
        {
            Die();
        }
    }
}
