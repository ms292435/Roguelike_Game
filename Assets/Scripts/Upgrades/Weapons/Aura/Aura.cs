using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Area weapon dealing damage at regular intervals to every enemy within a radius around the player.
    /// The damage is applied in a parallel job through EnemyBridge.QueueAreaDamage, so it scales to dense hordes.
    /// Draws its range under the enemies and the player, slowly spinning, at a size that only changes when the aura
    /// is upgraded.
    /// </summary>
    public class Aura : MonoBehaviour, IWeapon
    {
        public float mDamage = 8f;
        public float mRadius = 2.5f;

        [Tooltip("Seconds between two damage ticks.")]
        public float mTickInterval = 0.5f;

        [Tooltip("Initial speed of the knockback, kept small so that enemies stay inside the aura.")]
        public float mKnockback = 2f;

        [Header("Visuals")]
        public Sprite mSprite;
        public Color mColor = new(1f, 1f, 1f, 0.35f);

        [Tooltip("Rotation speed of the aura visual, in degrees per second. Purely visual.")]
        public float mRotationSpeed = 12f;

        [Tooltip("Sorting layer of the aura, below the player (Entities layer). Enemies are rendered by Entities " +
                 "Graphics, which ignores sorting layers: the render queue keeps the aura under them.")]
        public string mSortingLayer = "Default";

        [Tooltip("Depth offset placing the aura behind the enemies, which are between z = 0.05 (boss) and 0.51.")]
        public float mDepthOffset = 0.6f;

        [Tooltip("Render queue of the aura: after the ground (2000), before the enemies and orbs (2450 and more), so that " +
                 "they are always drawn over it, whatever the depth testing of the sprite shader.")]
        public int mRenderQueue = 2001;

        public int WeaponLevel { get; set; }
        public WeaponUpgradeData Data => mData;

        // The aura ticks continuously: no sound, it would play twice per second
        public AudioClip AttackSound => null;

        private WeaponUpgradeData mData;
        private SpriteRenderer mVisual;
        private float mTimer;

        /// <summary>
        /// Stores the weapon configuration data.
        /// </summary>
        /// <param name="pData">The weapon upgrade data.</param>
        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
        }

        /// <summary>
        /// Damages every enemy within the aura radius.
        /// </summary>
        public void Attack()
        {
            DOTS.EnemyBridge.QueueAreaDamage(transform.position, 0f, mRadius, mDamage * Player.Instance.Damage, mKnockback);
        }

        /// <summary>
        /// Ticks the damage timer and animates the aura visual.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time.</param>
        /// <returns>Always false: the aura has no attack sound.</returns>
        public bool UpdateWeapon(float pDeltaTime)
        {
            mTimer += pDeltaTime;
            if (mTimer >= mTickInterval)
            {
                Attack();
                mTimer = 0f;
            }

            if (mVisual != null && mSprite != null)
            {
                // Scale the sprite so that its width matches the aura diameter, whatever its size
                mVisual.transform.localScale = Vector3.one * (mRadius * 2f / mSprite.bounds.size.x);

                // Applied every frame so that the color and opacity can be tuned live in the Inspector
                mVisual.color = mColor;

                // Slow spin on itself; scaled delta time, so it stops when the game is paused
                mVisual.transform.Rotate(0f, 0f, mRotationSpeed * pDeltaTime);
            }

            return false;
        }

        /// <summary>
        /// Rolls a random upgrade for the next level: damage, radius or tick rate.
        /// </summary>
        /// <returns>The upgrade proposal.</returns>
        public WeaponUpgradeProposal GetNextUpgradeProposal()
        {
            var lProposal = new WeaponUpgradeProposal();

            switch (Random.Range(0, 3))
            {
                case 0:
                    lProposal.Description = "Damage +15%";
                    lProposal.ApplyAction = () => { mDamage *= 1.15f; WeaponLevel++; };
                    break;
                case 1:
                    lProposal.Description = "Radius +10%";
                    lProposal.ApplyAction = () => { mRadius *= 1.1f; WeaponLevel++; };
                    break;
                case 2:
                    lProposal.Description = "Tick rate +8%";
                    lProposal.ApplyAction = () => { mTickInterval *= 0.92f; WeaponLevel++; };
                    break;
            }
            return lProposal;
        }

        private void Awake()
        {
            var lVisualObject = new GameObject("AuraVisual");
            lVisualObject.transform.SetParent(transform, false);
            lVisualObject.transform.localPosition = new Vector3(0f, 0f, mDepthOffset);

            mVisual = lVisualObject.AddComponent<SpriteRenderer>();
            mVisual.sprite = mSprite;
            mVisual.color = mColor;

            // Drawn above the ground and below the enemies (render queue), and below the player (Entities layer)
            mVisual.sortingLayerName = mSortingLayer;
            mVisual.sortingOrder = 0;
            mVisual.material.renderQueue = mRenderQueue;
        }

        /// <summary>
        /// Mastery bonus applied once at the maximum level: boosts every stat of the weapon.
        /// </summary>
        public void ApplyMastery()
        {
            mDamage *= 1.5f;
            mRadius *= 1.25f;
            mTickInterval *= 0.8f;
        }
    }
}
