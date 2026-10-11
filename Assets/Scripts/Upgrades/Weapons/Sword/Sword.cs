using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Melee weapon that executes circular slashing attacks around the player.
    /// Spawns visual slash VFX instances and applies damage to ECS enemies via EnemyDamageProxy.
    /// </summary>
    public class Sword : MonoBehaviour, IWeapon
    {
        public float mDamage = 20f;
        public float mAttackInterval = 1f;
        public float mAttackRadius = 1.5f;

        public int WeaponLevel { get; set; }
        public WeaponUpgradeData Data => mData;
        public AudioClip AttackSound => mAttackSound;

        [Header("Visuals")]
        public GameObject mSlashVfxPrefab;

        private float mVisualScaleMultiplier = 1f;
        private float mTimer;
        private WeaponUpgradeData mData;
        private Vector3 mCurrentPosition;
        private Vector3 mAttackOffset = new(2f, 0f, 0f);
        private int mAttackCount;

        [SerializeField] private AudioClip mAttackSound;

        /// <summary>
        /// Initializes weapon configuration data and base attack count.
        /// </summary>
        /// <param name="pData">The weapon upgrade configuration data.</param>
        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
            mAttackCount = 3;
        }

        /// <summary>
        /// Executes the weapon attack by triggering circular slashes.
        /// </summary>
        public void Attack()
        {
            PerformCircularAttack(mAttackCount);
        }

        /// <summary>
        /// Distributes slash attacks evenly in a circle around the player.
        /// Instantiates visual slash effects and deals area damage through the ECS damage proxy.
        /// </summary>
        /// <param name="pCount">Number of slash strikes in the circular pattern.</param>
        private void PerformCircularAttack(int pCount)
        {
            float lRadius = mAttackOffset.magnitude;

            for (int i = 0; i < pCount; i++)
            {
                // Compute evenly spaced angle around the circle
                float lAngle = Mathf.PI + (i * (Mathf.PI * 2f / pCount));
                float lX = Mathf.Cos(lAngle) * lRadius;
                float lY = Mathf.Sin(lAngle) * lRadius;

                Vector3 lAttackPos = mCurrentPosition + new Vector3(lX, lY, 0f);
                float lAngleDeg = lAngle * Mathf.Rad2Deg;
                Quaternion lRotation = Quaternion.Euler(0f, 0f, lAngleDeg);

                // Spawn and configure pooled slash VFX
                SwordSlash lVfxInstance = MeleeAttacksManager.Instance.GetVfx(mSlashVfxPrefab, lAttackPos, lRotation);
                lVfxInstance.transform.localScale = new Vector3(3f, 3f, 1f) * mVisualScaleMultiplier;

                // Deal area damage to ECS enemies via EnemyBridge facade
                DOTS.EnemyBridge.DealDamage(lAttackPos, mAttackRadius, mDamage * Player.Instance.Damage);
            }
        }

        /// <summary>
        /// Updates the weapon cooldown timer and triggers attacks when ready.
        /// </summary>
        /// <param name="pDeltaTime">Elapsed frame time in seconds.</param>
        /// <returns>True if an attack was executed this frame, false otherwise.</returns>
        public bool UpdateWeapon(float pDeltaTime)
        {
            mCurrentPosition = transform.position;
            mTimer += pDeltaTime;

            if (mTimer >= mAttackInterval)
            {
                Attack();
                mTimer = 0f;
                return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mCurrentPosition + mAttackOffset, mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition - mAttackOffset, mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition + new Vector3(0, mAttackOffset.x, 0), mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition - new Vector3(0, mAttackOffset.x, 0), mAttackRadius);
        }

        /// <summary>
        /// Generates a randomized upgrade proposal for the level up screen.
        /// </summary>
        /// <returns>Weapon upgrade proposal with description and application callback.</returns>
        public WeaponUpgradeProposal GetNextUpgradeProposal()
        {
            int lRandomChoice = Random.Range(0, 3);
            var lProposal = new WeaponUpgradeProposal();

            switch (lRandomChoice)
            {
                case 0:
                    lProposal.Description = "Damage +20%";
                    lProposal.ApplyAction = () => { mDamage *= 1.2f; WeaponLevel++; };
                    break;
                case 1:
                    lProposal.Description = "Range +15%";
                    lProposal.ApplyAction = () =>
                    {
                        mAttackRadius *= 1.15f;
                        mVisualScaleMultiplier *= 1.15f;
                        mAttackOffset *= 1.15f;
                        WeaponLevel++;
                    };
                    break;
                case 2:
                    lProposal.Description = "+1 Attack";
                    lProposal.ApplyAction = () => { mAttackCount++; WeaponLevel++; };
                    break;
            }
            return lProposal;
        }
    }
}