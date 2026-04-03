using System.Collections.Generic;
using UnityEngine;

public class Player : Entity
{
    public List<Experience> mExperienceList = new List<Experience>();
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

        mRigibody.freezeRotation = true;
        mRigibody.velocity = new Vector3(lMoveHorizontal, lMoveVertical) * Speed;

        HandleExperience();
        HandleShooting();
    }

    private void HandleExperience()
    {
        Vector3 lPlayerPos = transform.position;
        float lSqrAttract = mAttractRadius * mAttractRadius;
        float lSqrCollect = mCollectRadius * mCollectRadius;

        for (int i = mExperienceList.Count - 1; i >= 0; i--)
        {
            Experience lExperience = mExperienceList[i];

            Vector3 lOffset = lPlayerPos - lExperience.transform.position;
            float lSqrDist = lOffset.sqrMagnitude;

            if (!lExperience.IsTrigger && lSqrDist < lSqrAttract)
            {
                lExperience.Trigger();
            }

            if (lExperience.IsTrigger)
            {
                lExperience.transform.position = Vector3.MoveTowards(
                    lExperience.transform.position,
                    lPlayerPos,
                    lExperience.mSpeed * Time.deltaTime
                );
            }

            if (lSqrDist < lSqrCollect)
            {
                lExperience.Collect(mExperienceBar);
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
        Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(transform.position);

        if (lTarget == null) return;

        Vector3 lDirection = (lTarget.transform.position - transform.position).normalized;

        Projectile lProjectile = ProjectilePool.Instance.GetProjectile();
        lProjectile.transform.position = transform.position;
        lProjectile.Init(lDirection, Damage);
    }
}