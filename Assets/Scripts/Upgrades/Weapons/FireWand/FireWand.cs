using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Ranged magic weapon that periodically targets and fires projectiles at the closest enemy.
    /// Interacts with the unmanaged ECS spatial hash grid to locate enemy entities.
    /// </summary>
    public class FireWand : MonoBehaviour, IWeapon
    {
        public WeaponUpgradeData Data => mData;
        public int WeaponLevel { get; set; }
        public AudioClip AttackSound => mAttackSound;

        private WeaponUpgradeData mData;
        private float mDamage = 50f;
        private float mFireRate = 0.5f;
        private float mFireTimer = 0f;
        private const float mTargetRange = 18f;

        [SerializeField] private AudioClip mAttackSound;

        /// <summary>
        /// Initializes the weapon with configuration data.
        /// </summary>
        /// <param name="pData">The weapon upgrade configuration data.</param>
        public void Initialize(WeaponUpgradeData pData) => mData = pData;

        /// <summary>
        /// Advances weapon cooldown timer and executes an attack if ready.
        /// </summary>
        /// <param name="pDeltaTime">Elapsed frame time.</param>
        /// <returns>True if an attack was triggered this frame, false otherwise.</returns>
        public bool UpdateWeapon(float pDeltaTime)
        {
            mFireTimer += pDeltaTime;
            if (mFireTimer >= mFireRate)
            {
                Attack();
                mFireTimer = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the closest enemy using the EnemyBridge facade and launches a projectile towards it.
        /// </summary>
        public void Attack()
        {
            var lPlayerPosition = Player.Instance.mCurrentPosition;

            if (!DOTS.EnemyBridge.TryGetClosestEnemy(lPlayerPosition, mTargetRange, out Vector3 lTargetPos))
            {
                return;
            }

            Vector3 lDirection = (lTargetPos - lPlayerPosition).normalized;
            Projectile lProjectile = ProjectileManager.Instance.GetProjectile(mData.mProjectilePrefab, lPlayerPosition);
            lProjectile.Init(lDirection, mDamage * Player.Instance.Damage);
        }

        /// <summary>
        /// Generates an upgrade proposal for the level up selection screen.
        /// </summary>
        /// <returns>A proposed upgrade option with an execution callback.</returns>
        public WeaponUpgradeProposal GetNextUpgradeProposal()
        {
            int lRandomChoice = Random.Range(0, 2);
            var lProposal = new WeaponUpgradeProposal();

            switch (lRandomChoice)
            {
                case 0:
                    lProposal.Description = "Damage +20%";
                    lProposal.ApplyAction = () => { mDamage *= 1.2f; WeaponLevel++; };
                    break;
                case 1:
                    lProposal.Description = "Fire rate +15%";
                    lProposal.ApplyAction = () => { mFireRate *= 0.85f; WeaponLevel++; };
                    break;
            }
            return lProposal;
        }
    }
}