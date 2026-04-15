using UnityEngine;
using UnityEngine.VFX;

namespace Roguelike
{
    public class Sword : MonoBehaviour, IWeapon
    {
        public float mDamage = 20f;
        public float mAttackInterval = 1f;
        public float mAttackRadius = 1.5f;

        public WeaponUpgradeData Data => mData;

        [Header("Visuals")]
        public GameObject mSlashVfxPrefab; 

        private float mTimer;

        private WeaponUpgradeData mData;

        private Vector3 mCurrentPosition;
        private Vector3 mAttackOffset = new(2f, 0f, 0f);

        private int mAttackCount;
        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
            mAttackCount = 3;
        }
        public void Attack()
        {
            PerformCircularAttack(mAttackCount);
        }

        private void PerformCircularAttack(int pCount)
        {
            float lRadius = mAttackOffset.magnitude;

            for (int i = 0; i < pCount; i++)
            {
                // Compute angle for this attack (evenly spaced around the circle)
                float lAngle = Mathf.PI + (i * (Mathf.PI * 2f / pCount));

                // Convert polar coordinates to Cartesian
                float lX = Mathf.Cos(lAngle) * lRadius;
                float lY = Mathf.Sin(lAngle) * lRadius;

                Vector3 lAttackPos = mCurrentPosition + new Vector3(lX, lY, 0);

                // Rotate the VFX to face outward
                float lAngleDeg = lAngle * Mathf.Rad2Deg;
                Quaternion lRotation = Quaternion.Euler(0, 0, lAngleDeg);

                // Instantiation and Damage
                MeleeAttacksManager.Instance.GetVfx(mSlashVfxPrefab, lAttackPos, lRotation);

                ApplyDamageAtPosition(lAttackPos);
            }
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
                lEnemy.TakeDamage(mDamage * Player.Instance.Damage);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mCurrentPosition + mAttackOffset, mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition - mAttackOffset, mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition + new Vector3(0, mAttackOffset.x, 0), mAttackRadius);
            Gizmos.DrawWireSphere(mCurrentPosition - new Vector3(0, mAttackOffset.x, 0), mAttackRadius);
        }
    }
}