using Roguelike;
using UnityEngine;

namespace Roguelike
{
    public class FireWand : MonoBehaviour, IWeapon
    {
        public WeaponUpgradeData Data => mData;
        private WeaponUpgradeData mData;

        private readonly float mDamage = 50f; 
        private readonly float mFireRate = 0.5f;
        private float mFireTimer = 0f;

        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
        }

        public void UpdateWeapon(float pDeltaTime)
        {
            mFireTimer += pDeltaTime;

            if (mFireTimer >= mFireRate)
            {
                Attack();
                mFireTimer = 0f;
            }
        }

        public void Attack()
        {
            var lPlayerPosition = Player.Instance.mCurrentPosition;
            Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(lPlayerPosition);

            if (lTarget == null) return;

            Vector3 lDirection = (lTarget.transform.position - lPlayerPosition).normalized;
            Projectile lProjectile = ProjectileManager.Instance.GetProjectile(mData.mProjectilePrefab, lPlayerPosition);
            lProjectile.Init(lDirection, mDamage * Player.Instance.Damage);
        }
    }
}