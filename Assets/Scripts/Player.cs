using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : Entity
{
    private List<Experience> mNearbyExpCache = new List<Experience>();
    private List<Experience> mFlyingExperience = new List<Experience>();
    public ExperienceBar mExperienceBar;

    public float mFireRate = 1f;


    public float mAttractRadius = 2f;
    public float mCollectRadius = 1f;

    private Rigidbody2D mRigibody;

    private float mFireTimer = 0f;

    public static Player Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Health = 100f;
        Damage = 50f;
        Speed = 4f;
    }

    private void Start()
    {
        mRigibody = GetComponent<Rigidbody2D>();
        mRigibody.freezeRotation = true;
    }

    private void Update()
    {
        float lMoveHorizontal = Input.GetAxis("Horizontal");
        float lMoveVertical = Input.GetAxis("Vertical");

        mRigibody.velocity = new Vector3(lMoveHorizontal, lMoveVertical) * Speed;

        HandleExperience();
        HandleShooting();
    }

    private void HandleExperience()
    {
        Vector3 lPlayerPos = transform.position;
        float lSqrAttract = mAttractRadius * mAttractRadius;
        float lSqrCollect = mCollectRadius * mCollectRadius;

        SpatialGrid.Instance.GetNearbyExperienceNonAlloc(lPlayerPos, mAttractRadius, mNearbyExpCache);

        for (int i = mNearbyExpCache.Count - 1; i >= 0; i--)
        {
            Experience lExperience = mNearbyExpCache[i];

            Vector3 lOffset = lPlayerPos - lExperience.mCurrentPosition;
            float lSqrDist = lOffset.sqrMagnitude;

            if (!lExperience.IsTrigger && lSqrDist < lSqrAttract)
            {
                lExperience.Trigger();
                mFlyingExperience.Add(lExperience); 
            }
        }

        for (int i = mFlyingExperience.Count - 1; i >= 0; i--)
        {
            Experience lExp = mFlyingExperience[i];
            float lSqrDist = (lPlayerPos - lExp.mCurrentPosition).sqrMagnitude;

            if (lSqrDist < lSqrCollect)
            {
                lExp.Collect(mExperienceBar);
                mFlyingExperience.RemoveAt(i);
            }
        }
    }

    void HandleShooting()
    {
        mFireTimer += Time.deltaTime;

        if (mFireTimer >= 1f / mFireRate)
        {
            Shoot();
            mFireTimer = 0f;
        }
    }

    void Shoot()
    {
        var lPlayerPosition = transform.position;
        Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(lPlayerPosition);

        if (lTarget == null) return;

        Vector3 lDirection = (lTarget.transform.position - lPlayerPosition).normalized;

        Projectile lProjectile = ProjectilePool.Instance.GetProjectile();
        lProjectile.transform.position = lPlayerPosition;
        lProjectile.Init(lDirection, Damage);
    }
}