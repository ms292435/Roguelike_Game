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

        [Header("Game Mode Stats")]
        public float mStartHealth = 100f;
        public float mStartMaxXP = 10f;

        [Tooltip("Seconds of invulnerability after taking contact damage.")]
        public float mInvulnerabilityDuration = 0.5f;

        [Tooltip("Maximum damage taken in a single hit, however many enemies touch the player in the same frame.")]
        public float mMaxDamagePerHit = 15f;

        public float CurrentXP { get; private set; }
        public float MaxXP { get; private set; }
        public float MaxHealth { get; private set; }

        public static Player Instance { get; private set; }

        public Vector3 mCurrentPosition;

        private Rigidbody2D mRigidbody;
        private readonly List<IObserver> mObservers = new();
        private bool mIsDead = false;
        private float mInvulnerableUntil;

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
        /// Resets the player stats for the mode of the new session.
        /// Benchmarks use an unreachable health and experience threshold, so that the player never dies and
        /// level-up menus never pause the measurements.
        /// </summary>
        /// <param name="pBenchmark">True for a benchmark session, false for a regular game.</param>
        public void ApplyModeStats(bool pBenchmark)
        {
            MaxHealth = pBenchmark ? 5000000f : mStartHealth;
            Health = MaxHealth;
            MaxXP = pBenchmark ? 100000f : mStartMaxXP;
            CurrentXP = 0f;
            mInvulnerableUntil = 0f;
            Notify("Experience");
        }

        /// <summary>
        /// Applies the damage of the enemies that touched the player this frame.
        /// The player is briefly invulnerable after each hit, and a single hit is capped,
        /// so a dense horde wears the player down instead of killing them instantly.
        /// </summary>
        /// <param name="pDamage">Total damage of the enemies that touched the player this frame.</param>
        public void TakeContactDamage(float pDamage)
        {
            if (mIsDead || pDamage <= 0f || Time.time < mInvulnerableUntil) return;

            Health -= Mathf.Min(pDamage, mMaxDamagePerHit);
            mInvulnerableUntil = Time.time + mInvulnerabilityDuration;
        }

        /// <summary>
        /// Increases the maximum health by a multiplier, and heals the player by the amount gained.
        /// </summary>
        /// <param name="pMultiplier">Multiplier applied to the maximum health.</param>
        public void BoostMaxHealth(float pMultiplier)
        {
            float lGained = MaxHealth * (pMultiplier - 1f);
            MaxHealth += lGained;
            Health = Mathf.Min(Health + lGained, MaxHealth);
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
            MaxHealth = mStartHealth;
            Health = MaxHealth;
            Damage = 1f;   // Multiplier applied to every weapon's damage, raised by the Damage Up boost
            Speed = 4f;
            MaxXP = mStartMaxXP;
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