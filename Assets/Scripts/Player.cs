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

        [Header("Progression")]
        [Tooltip("Each level requires the previous requirement times this factor...")]
        public float mXPGrowthFactor = 1.1f;

        [Tooltip("...plus this flat amount, which slows down the mid-game levels.")]
        public float mXPGrowthFlat = 2f;

        [Tooltip("Maximum movement speed, as a multiple of the base speed, reachable with Speed Up boosts.")]
        public float mMaxSpeedMultiplier = 1.5f;

        public float CurrentXP { get; private set; }
        public float MaxXP { get; private set; }
        public float MaxHealth { get; private set; }

        public static Player Instance { get; private set; }

        public Vector3 mCurrentPosition;

        private Rigidbody2D mRigidbody;
        private readonly List<IObserver> mObservers = new();
        private bool mIsDead = false;
        private float mInvulnerableUntil;

        // Base stats: stat boosts add a fraction of these, so that bonuses stack additively instead of compounding
        private const float BaseDamage = 1f;
        private const float BaseSpeed = 4f;
        private float mBaseAttractRadius;

        // Mastery bonuses, granted once when a stat boost reaches its maximum level
        private const float HealthMasteryBonus = 50f;          // Flat maximum health, plus a full heal
        private const float DamageMasteryBonus = 0.25f;        // Fraction of the base damage
        private const float SpeedMasteryBonus = 0.1f;          // Fraction of the base speed (reaches the speed cap)
        private const float PickupMasteryBonus = 0.5f;         // Fraction of the base pickup range

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
        /// Restores health, without exceeding the maximum health.
        /// </summary>
        /// <param name="pAmount">Health restored.</param>
        public void Heal(float pAmount)
        {
            if (mIsDead) return;
            Health = Mathf.Min(Health + pAmount, MaxHealth);
        }

        /// <summary>
        /// Applies a stat boost chosen on level up. Each boost adds a fraction of the base stat, so that boosts stack
        /// additively: five +10% damage boosts give +50%, instead of +61% if they multiplied each other.
        /// </summary>
        /// <param name="pStat">Boosted stat: "Health", "Damage" or "Speed".</param>
        /// <param name="pBonus">Fraction of the base stat added, e.g. 0.1 for +10%.</param>
        public void ApplyStatBoost(string pStat, float pBonus)
        {
            switch (pStat)
            {
                case "Health":
                    // Raises the maximum health, and heals the player by the amount gained
                    float lGained = mStartHealth * pBonus;
                    MaxHealth += lGained;
                    Health = Mathf.Min(Health + lGained, MaxHealth);
                    break;
                case "Damage":
                    Damage += BaseDamage * pBonus;
                    break;
                case "Speed":
                    Speed = Mathf.Min(Speed + BaseSpeed * pBonus, BaseSpeed * mMaxSpeedMultiplier);
                    break;
                case "Pickup":
                    // Range at which experience orbs start flying to the player
                    mAttractRadius += mBaseAttractRadius * pBonus;
                    break;
            }
        }

        /// <summary>
        /// Applies the mastery bonus of a stat, granted once when its boost reaches the maximum level.
        /// </summary>
        /// <param name="pStat">Mastered stat: "Health", "Damage", "Speed" or "Pickup".</param>
        public void ApplyStatMastery(string pStat)
        {
            switch (pStat)
            {
                case "Health":
                    MaxHealth += HealthMasteryBonus;
                    Health = MaxHealth;
                    break;
                case "Damage":
                    Damage += BaseDamage * DamageMasteryBonus;
                    break;
                case "Speed":
                    Speed = Mathf.Min(Speed + BaseSpeed * SpeedMasteryBonus, BaseSpeed * mMaxSpeedMultiplier);
                    break;
                case "Pickup":
                    mAttractRadius += mBaseAttractRadius * PickupMasteryBonus;
                    break;
            }
        }

        /// <summary>
        /// Describes the mastery bonus of a stat, shown on the level-up card of its last boost.
        /// </summary>
        /// <param name="pStat">Stat: "Health", "Damage", "Speed" or "Pickup".</param>
        /// <returns>Short description of the bonus.</returns>
        public static string GetStatMasteryDescription(string pStat)
        {
            return pStat switch
            {
                "Health" => $"+{HealthMasteryBonus:0} Max Health & full heal",
                "Damage" => $"+{DamageMasteryBonus * 100f:0}% Damage",
                "Speed" => $"+{SpeedMasteryBonus * 100f:0}% Speed",
                "Pickup" => $"+{PickupMasteryBonus * 100f:0}% Pickup Range",
                _ => "big bonus",
            };
        }

        /// <summary>
        /// Tells whether a stat can still be boosted, so that maxed-out boosts are no longer offered on level up.
        /// </summary>
        /// <param name="pStat">Stat to check: "Health", "Damage" or "Speed".</param>
        /// <returns>False when the stat has reached its cap.</returns>
        public bool CanBoostStat(string pStat)
        {
            return pStat != "Speed" || Speed < BaseSpeed * mMaxSpeedMultiplier - 0.001f;
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
                MaxXP = Mathf.Round(MaxXP * mXPGrowthFactor + mXPGrowthFlat);
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
            mBaseAttractRadius = mAttractRadius;
            MaxHealth = mStartHealth;
            Health = MaxHealth;
            Damage = 1f;   // Multiplier applied to every weapon's damage, raised by the Damage Up boost
            Speed = BaseSpeed;
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