using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Roguelike
{
    public class Player : Entity
    {
        public ExperienceBar mExperienceBar;

        public Transform mWeaponSlot;

        public float mFireRate = 1f;
        public float mAttractRadius = 2f;
        public float mCollectRadius = 1f;

        public static Player Instance { get; private set; }

        public Vector3 mCurrentPosition;

        private Rigidbody2D mRigibody;

        private readonly List<Experience> mNearbyExpCache = new();
        private readonly List<Experience> mFlyingExperience = new();


        public void EquipWeapon(WeaponUpgradeData pWeaponData)
        {
            if (pWeaponData == null)
            {
                Debug.LogError("ERROR: The weapon is null! The LevelUpManager did not find a weapon with the exact Name 'Sword'. Check the mAllAvailableUpgrades list and the Name field of your Scriptable Object.");
                return;
            }

            if (mWeaponSlot == null)
            {
                Debug.LogError("ERROR: The mWeaponSlot of the Player is null! Drag the WeaponSlot object into the Player script inspector.");
                return;
            }

            if (pWeaponData.mVisualPrefab == null)
            {
                Debug.LogError("ERROR: The mVisualPrefab is null in the weapon data!");
                return;
            }

            GameObject lWeaponLogic = Instantiate(pWeaponData.mVisualPrefab, mWeaponSlot);
            WeaponManager.Instance.AddWeapon(lWeaponLogic, pWeaponData);
        }

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
                Experience lExperience = mFlyingExperience[i];
                float lSqrDist = (mCurrentPosition - lExperience.mCurrentPosition).sqrMagnitude;

                if (lSqrDist < lSqrCollect)
                {
                    lExperience.Collect();
                    mFlyingExperience.RemoveAt(i);
                }
            }
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
            Damage = 25f;
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
            WeaponManager.Instance.UpdateWeapons();
            float lMoveHorizontal = Input.GetAxis("Horizontal");
            float lMoveVertical = Input.GetAxis("Vertical");

            mRigibody.velocity = new Vector3(lMoveHorizontal, lMoveVertical) * Speed;

            HandleExperience();
        }
    }
}