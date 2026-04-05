using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class Player : Entity
    {
        public ExperienceBar mExperienceBar;

        public float mFireRate = 1f;
        public float mAttractRadius = 2f;
        public float mCollectRadius = 1f;

        public static Player Instance { get; private set; }

        private Rigidbody2D mRigibody;

        private float mFireTimer = 0f;

        private readonly List<Experience> mNearbyExpCache = new();
        private readonly List<Experience> mFlyingExperience = new();

        private Vector3 mCurrentPosition;

        private void HandleExperience()
        {
            float lSqrAttract = mAttractRadius * mAttractRadius;
            float lSqrCollect = mCollectRadius * mCollectRadius;

            SpatialGrid.Instance.GetNearbyExperience(mCurrentPosition, mAttractRadius, mNearbyExpCache);

            for (int i = mNearbyExpCache.Count - 1; i >= 0; i--)
            {
                Experience lExperience = mNearbyExpCache[i];

                Vector3 lOffset = mCurrentPosition - lExperience.mCurrentPosition;
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
                float lSqrDist = (mCurrentPosition - lExp.mCurrentPosition).sqrMagnitude;

                if (lSqrDist < lSqrCollect)
                {
                    lExp.Collect();
                    mFlyingExperience.RemoveAt(i);
                }
            }
        }

        private void HandleShooting()
        {
            mFireTimer += Time.deltaTime;

            if (mFireTimer >= 1f / mFireRate)
            {
                Shoot();
                mFireTimer = 0f;
            }
        }

        private void Shoot()
        {
            Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(mCurrentPosition);

            if (lTarget == null) return;

            Vector3 lDirection = (lTarget.transform.position - mCurrentPosition).normalized;

            Projectile lProjectile = ProjectilePool.Instance.GetProjectile();
            lProjectile.transform.position = mCurrentPosition;
            lProjectile.Init(lDirection, Damage);
        }

        void Awake()
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

        void Start()
        {
            mRigibody = GetComponent<Rigidbody2D>();
            mRigibody.freezeRotation = true;
        }

        void Update()
        {
            mCurrentPosition = transform.position;

            float lMoveHorizontal = Input.GetAxis("Horizontal");
            float lMoveVertical = Input.GetAxis("Vertical");

            mRigibody.velocity = new Vector3(lMoveHorizontal, lMoveVertical) * Speed;

            HandleExperience();
            HandleShooting();
        }
    }
}