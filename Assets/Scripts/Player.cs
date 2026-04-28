using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class Player : Entity, ISubject
    {
        public ExperienceBar mExperienceBar;

        public Transform mWeaponSlot;

        public float mAttractRadius = 2f;
        public float mCollectRadius = 1f;
        public float CurrentXP { get; private set; }
        public float MaxXP { get; private set; }

        public static Player Instance { get; private set; }


        public Vector3 mCurrentPosition;

        private Rigidbody2D mRigibody;

        private readonly List<IObserver> mObservers = new();

        private bool mIsDead = false;

        public void AddObserver(IObserver pObserver) => mObservers.Add(pObserver);

        public void RemoveObserver(IObserver pObserver) => mObservers.Remove(pObserver);

        public void Notify(string pEventName)
        {
            foreach (var lObserver in mObservers) lObserver.OnNotify(this, pEventName);
        }

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

            if (pWeaponData.mWeaponLogicPrefab == null)
            {
                Debug.LogError("ERROR: The mWeaponLogicPrefab is null in the weapon data!");
                return;
            }

            GameObject lWeaponLogic = Instantiate(pWeaponData.mWeaponLogicPrefab, mWeaponSlot);

            if (lWeaponLogic.TryGetComponent<IWeapon>(out var lWeaponInterface))
            {
                lWeaponInterface.Initialize(pWeaponData);
                WeaponManager.Instance.AddWeapon(lWeaponLogic, pWeaponData);
            }
            else
            {
                Debug.LogError($"Le prefab {pWeaponData.mWeaponLogicPrefab.name} n'a pas de script IWeapon !");
            }
        }

        public void AddExperience(float pAmount)
        {
            if (MaxXP <= 0)
            {
                Debug.LogError("ERROR: MaxXP must be greater than 0 to add experience!");
                return;
            }

            CurrentXP += pAmount;

            while (CurrentXP >= MaxXP)
            {
                CurrentXP -= MaxXP;
                MaxXP = Mathf.Round(MaxXP * 1.1f); 
                Notify("LevelUp"); 
            }
            Notify("Experience"); 
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mIsDead = false;
            Health = 50000f;
            Damage = 100f;
            Speed = 4f;
            MaxXP = 10f;
            CurrentXP = 0f;
        }

        void Start()
        {
            mRigibody = GetComponent<Rigidbody2D>();
            mRigibody.freezeRotation = true;
        }

        void Update()
        {
            if (mIsDead) return;

            mCurrentPosition = transform.position;
            WeaponManager.Instance.UpdateWeapons();

            if (Health <= 0)
            {
                mIsDead = true;
                GameManager.Instance.DisplayGameOver();
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}