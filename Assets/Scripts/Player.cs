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

            if (pWeaponData.mVisualPrefab == null)
            {
                Debug.LogError("ERROR: The mVisualPrefab is null in the weapon data!");
                return;
            }

            GameObject lWeaponLogic = Instantiate(pWeaponData.mVisualPrefab, mWeaponSlot);
            WeaponManager.Instance.AddWeapon(lWeaponLogic, pWeaponData);
        }

        public void AddExperience(float pAmount)
        {
            CurrentXP += pAmount;

            while (CurrentXP >= MaxXP)
            {
                CurrentXP -= MaxXP;
                MaxXP = Mathf.Round(MaxXP * 1.5f); 
                Notify("LevelUp"); 
            }
            Notify("Experience"); 
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
            Damage = 1f;
            Speed = 4f;
            MaxXP = 10f;
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
        }

    }
}