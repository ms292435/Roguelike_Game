using UnityEngine;

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

        public void Initialize(WeaponUpgradeData pData)
        {
            mData = pData;
        }
        public void Attack()
        {
            int lAttackCount = 4;
            PerformCircularAttack(lAttackCount);
        }

        private void PerformCircularAttack(int pCount)
        {
            float lRadius = mAttackOffset.magnitude;

            for (int i = 0; i < pCount; i++)
            {
                // Calcul de l'angle : On part de PI, et on ajoute une fraction de cercle (2*PI / nombre d'attaques)
                float lAngle = Mathf.PI + (i * (Mathf.PI * 2f / pCount));

                // Conversion coordonnées polaires -> cartésiennes (x, y)
                float lX = Mathf.Cos(lAngle) * lRadius;
                float lY = Mathf.Sin(lAngle) * lRadius;

                Vector3 lAttackPos = mCurrentPosition + new Vector3(lX, lY, 0);

                // Rotation du VFX pour qu'il "regarde" vers l'extérieur du cercle
                float lAngleDeg = lAngle * Mathf.Rad2Deg;
                Quaternion lRotation = Quaternion.Euler(0, 0, lAngleDeg);

                // Instanciation et Dégâts
                Instantiate(mSlashVfxPrefab, lAttackPos, lRotation);
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