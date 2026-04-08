using UnityEngine;

namespace Roguelike
{
    public class Sword : MonoBehaviour, IWeapon
    {
        public float mDamage = 50f;
        public float mAttackInterval = 1.0f;
        public float mAttackRadius = 1.5f;

        public WeaponUpgradeData Data => mData;

        [Header("Visuals")]
        public GameObject mSlashVfxPrefab; 

        private float mTimer;

        private WeaponUpgradeData mData;

        private Vector3 mCurrentPosition;
        private Vector3 mAttackOffset = new(2f, 0f, 0f);

        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
        }
        public void Attack()
        {
            Vector3 lRightPos = mCurrentPosition + mAttackOffset;
            Vector3 lLeftPos = mCurrentPosition - mAttackOffset;

            Instantiate(mSlashVfxPrefab, lRightPos, Quaternion.identity);
            Instantiate(mSlashVfxPrefab, lLeftPos, Quaternion.Euler(0, 180f, 0));

            ApplyDamageAtPosition(lRightPos);
            ApplyDamageAtPosition(lLeftPos);
        }

        public void UpdateWeapon(float pDeltaTime)
        {
            mCurrentPosition = transform.position;
            mTimer += pDeltaTime;

            if (mTimer >= mAttackInterval)
            {
                Attack();
                mTimer = 0f;
            }
        }

        private void ApplyDamageAtPosition(Vector3 pPosition)
        {
            var lEnemiesInRange = SpatialGrid.Instance.GetEnemiesInRadius(pPosition, mAttackRadius);
            foreach (var lEnemy in lEnemiesInRange)
            {
                lEnemy.TakeDamage(mDamage);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mCurrentPosition + mAttackOffset, mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition - mAttackOffset, mAttackRadius);
        }
    }
}