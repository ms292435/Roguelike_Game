using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Area weapon releasing, at regular intervals, a shockwave that expands from the player and strongly
    /// knocks back every enemy it crosses. Each frame of the expansion damages the ring swept since the previous
    /// frame, so every enemy is hit once by the wave.
    /// The damage is applied in a parallel job through EnemyBridge.QueueAreaDamage, so it scales to dense hordes.
    /// </summary>
    public class Nova : MonoBehaviour, IWeapon
    {
        public float mDamage = 40f;
        public float mMaxRadius = 7f;
        public float mCooldown = 4f;

        [Tooltip("Time for the wave to reach its maximum radius, in seconds.")]
        public float mExpandDuration = 0.45f;

        [Tooltip("Initial speed of the knockback pushing enemies away from the player.")]
        public float mKnockback = 12f;

        [Header("Visuals")]
        public Sprite mSprite;
        public Color mColor = new(1f, 0.8f, 0.35f, 0.9f);

        [Tooltip("Sorting layer of the wave, drawn above the player and the enemies.")]
        public string mSortingLayer = "Entities";

        [SerializeField] private AudioClip mAttackSound;

        public int WeaponLevel { get; set; }
        public WeaponUpgradeData Data => mData;
        public AudioClip AttackSound => mAttackSound;

        private WeaponUpgradeData mData;
        private SpriteRenderer mVisual;
        private float mTimer;
        private float mWaveTime = -1f;
        private float mWaveRadius;
        private int mWaveId;

        /// <summary>
        /// Last wave identifier given, shared by every Nova so that two waves never share an identifier.
        /// </summary>
        private static int sLastWaveId;

        /// <summary>
        /// Stores the weapon configuration data.
        /// </summary>
        /// <param name="pData">The weapon upgrade data.</param>
        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
        }

        /// <summary>
        /// Releases a new shockwave from the player.
        /// </summary>
        public void Attack()
        {
            mWaveId = ++sLastWaveId;
            mWaveTime = 0f;
            mWaveRadius = 0f;
            if (mVisual != null) mVisual.enabled = true;
        }

        /// <summary>
        /// Releases a wave when the cooldown is over, and expands the current wave.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time.</param>
        /// <returns>True when a new wave was released this frame.</returns>
        public bool UpdateWeapon(float pDeltaTime)
        {
            bool lHasAttacked = false;

            mTimer += pDeltaTime;
            if (mTimer >= mCooldown)
            {
                Attack();
                mTimer = 0f;
                lHasAttacked = true;
            }

            if (mWaveTime >= 0f)
            {
                ExpandWave(pDeltaTime);
            }

            return lHasAttacked;
        }

        /// <summary>
        /// Grows the wave, damages the ring it swept this frame and updates its visual.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time.</param>
        private void ExpandWave(float pDeltaTime)
        {
            mWaveTime += pDeltaTime;
            float lProgress = Mathf.Clamp01(mWaveTime / mExpandDuration);

            // Ease out: fast at first, slowing down at the end
            float lRadius = mMaxRadius * (1f - (1f - lProgress) * (1f - lProgress));

            // The shockwave carries the enemies it crosses to its edge, and hits each of them only once
            DOTS.EnemyBridge.QueueAreaDamage(transform.position, mWaveRadius, lRadius,
                mDamage * Player.Instance.Damage, mKnockback, mMaxRadius, mWaveId);
            mWaveRadius = lRadius;

            if (mVisual != null && mSprite != null)
            {
                // Scale the sprite so that its width matches the wave diameter, fading out as it expands
                mVisual.transform.localScale = Vector3.one * (lRadius * 2f / mSprite.bounds.size.x);
                Color lColor = mColor;
                lColor.a *= 1f - lProgress;
                mVisual.color = lColor;
            }

            if (lProgress >= 1f)
            {
                mWaveTime = -1f;
                if (mVisual != null) mVisual.enabled = false;
            }
        }

        /// <summary>
        /// Rolls a random upgrade for the next level: damage, radius or cooldown.
        /// </summary>
        /// <returns>The upgrade proposal.</returns>
        public WeaponUpgradeProposal GetNextUpgradeProposal()
        {
            var lProposal = new WeaponUpgradeProposal();

            switch (Random.Range(0, 3))
            {
                case 0:
                    lProposal.Description = "Damage +25%";
                    lProposal.ApplyAction = () => { mDamage *= 1.25f; WeaponLevel++; };
                    break;
                case 1:
                    lProposal.Description = "Radius +20%";
                    lProposal.ApplyAction = () => { mMaxRadius *= 1.2f; WeaponLevel++; };
                    break;
                case 2:
                    lProposal.Description = "Cooldown -15%";
                    lProposal.ApplyAction = () => { mCooldown *= 0.85f; WeaponLevel++; };
                    break;
            }
            return lProposal;
        }

        private void Awake()
        {
            var lVisualObject = new GameObject("NovaVisual");
            lVisualObject.transform.SetParent(transform, false);

            mVisual = lVisualObject.AddComponent<SpriteRenderer>();
            mVisual.sprite = mSprite;
            mVisual.color = mColor;
            mVisual.sortingLayerName = mSortingLayer;
            mVisual.sortingOrder = 2;
            mVisual.enabled = false;
        }
    }
}
