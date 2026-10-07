using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Singleton player character component managing health, experience progression, weapon equipping, and observer notifications.
    /// Acts as an ISubject notifying observers upon LevelUp and Experience gains.
    /// </summary>
    public class Player : RoguelikeEntity, ISubject
    {
        public ExperienceBar mExperienceBar;
        public Transform mWeaponSlot;

        public float mAttractRadius = 2f;
        public float mCollectRadius = 1f;

        public float CurrentXP { get; private set; }
        public float MaxXP { get; private set; }

        public static Player Instance { get; private set; }

        public Vector3 mCurrentPosition;

        private Rigidbody2D mRigidbody;
        private readonly List<IObserver> mObservers = new();
        private bool mIsDead = false;

        /// <summary>
        /// Registers an observer to receive player notifications.
        /// </summary>
        /// <param name="pObserver">The observer instance.</param>
        public void AddObserver(IObserver pObserver) => mObservers.Add(pObserver);

        /// <summary>
        /// Removes an observer from player notifications.
        /// </summary>
        /// <param name="pObserver">The observer instance to unregister.</param>
        public void RemoveObserver(IObserver pObserver) => mObservers.Remove(pObserver);

        /// <summary>
        /// Broadcasts an event name to all subscribed observers.
        /// </summary>
        /// <param name="pEventName">Event identifier (e.g. "LevelUp", "Experience").</param>
        public void Notify(string pEventName)
        {
            foreach (var lObserver in mObservers) lObserver.OnNotify(this, pEventName);
        }

        /// <summary>
        /// Instantiates and equips a weapon logic prefab onto the player's weapon slot.
        /// </summary>
        /// <param name="pWeaponData">The weapon upgrade configuration data.</param>
        public void EquipWeapon(WeaponUpgradeData pWeaponData)
        {
            if (pWeaponData == null)
            {
                Debug.LogError("ERROR: The weapon is null! Check the mAllAvailableUpgrades list.");
                return;
            }

            if (mWeaponSlot == null)
            {
                Debug.LogError("ERROR: The mWeaponSlot of the Player is null! Assign it in the inspector.");
                return;
            }

            if (pWeaponData.mWeaponLogicPrefab == null)
            {
                Debug.LogError("ERROR: The mWeaponLogicPrefab is null in weapon data!");
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
                Debug.LogError($"Prefab {pWeaponData.mWeaponLogicPrefab.name} does not implement IWeapon!");
            }
        }

        /// <summary>
        /// Adds experience points to the player and triggers level-up events when threshold is reached.
        /// </summary>
        /// <param name="pAmount">Amount of experience gained.</param>
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mIsDead = false;
            Health = 5000000f;
            Damage = 100f;
            Speed = 4f;
            MaxXP = 100000f;
            CurrentXP = 0f;
        }

        private void Start()
        {
            mRigidbody = GetComponent<Rigidbody2D>();
            mRigidbody.freezeRotation = true;
        }

        private void Update()
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

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}