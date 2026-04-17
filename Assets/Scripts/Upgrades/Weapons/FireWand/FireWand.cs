using Roguelike;
using UnityEngine;

namespace Roguelike
{
    public class FireWand : MonoBehaviour, IWeapon
    {
        public WeaponUpgradeData Data => mData;

        public int WeaponLevel { get; set; }

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
            var lPlayerPosition = Player.Instance.mCurrentPosition;
            Enemy lTarget = SpatialGrid.Instance.GetClosestEnemyInGrid(lPlayerPosition);

            if (lTarget == null) return;

            Vector3 lDirection = (lTarget.transform.position - lPlayerPosition).normalized;
            Projectile lProjectile = ProjectileManager.Instance.GetProjectile(mData.mProjectilePrefab, lPlayerPosition);
            lProjectile.Init(lDirection, mDamage * Player.Instance.Damage);
        }

        public void Upgrade()
        {
            WeaponLevel++;
            int lRandomChoice = Random.Range(0, 3);

            switch (lRandomChoice)
            {
                case 0: mDamage *= 1.25f; break;
                case 1: mFireRate *= 0.85f; break;
                case 2:
                    break;
            }
        }

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