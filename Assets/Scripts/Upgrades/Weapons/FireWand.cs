using Roguelike;
using UnityEngine;

namespace Roguelike
{
    public class FireWand : MonoBehaviour, IWeapon
    {
        public WeaponUpgradeData Data => mData;

        private WeaponUpgradeData mData;

        private float mDamage = 50f; 
        private float mFireRate = 0.5f; 
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
            Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(Player.Instance.mCurrentPosition);

            if (lTarget == null) return;

            Vector3 lDirection = (lTarget.transform.position - Player.Instance.mCurrentPosition).normalized;
            Projectile lProjectile = ProjectilePool.Instance.GetProjectile();
            lProjectile.transform.position = Player.Instance.mCurrentPosition;

            lProjectile.Init(lDirection, mDamage * Player.Instance.Damage);
            lProjectile.gameObject.SetActive(true);
        }
    }
}