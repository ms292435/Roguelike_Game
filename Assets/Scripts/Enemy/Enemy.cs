using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class Enemy : Entity
    {
        public Vector3 mCurrentPosition;

        private Vector3 mLastPosition;
        private Vector3 mSeparationForce;

        private Transform mTarget;

        private bool mIsDead = false;

        private float mAnimationOffset; // Desync ennemies
        private readonly float mTiltAngle = 5f;    // Max angle of the tilt
        private readonly float mTiltSpeed = 5f;    // Tilt speed

        private readonly float mSeparationRadius = 1f; // Min distance to maintain from other enemies
        private readonly float mSeparationStrength = 10f; // Push strength to maintain separation

        private int mGlobalUpdateIndex = 0;
        private int mInstanceUpdateOrder;

        private SpriteRenderer mRenderer;
        private Coroutine mFlashCoroutine;
        [SerializeField] private Color mHitColor = Color.red;
        [SerializeField] private float mFlashDuration = 0.1f;

        public void Init(Transform pTarget)
        {
            mRenderer = GetComponent<SpriteRenderer>();

            mTarget = pTarget;
            Health = 50f;
            Speed = 1f;

            mLastPosition = transform.position;

            SpatialGrid.Instance.AddEnemy(this, SpatialGrid.Instance.GetGridPos(mLastPosition));
            mAnimationOffset = Random.Range(0f, 10f);
        }


        public void TakeDamage(float pDamage)
        {
            Health -= pDamage;

            if (mFlashCoroutine != null)
            {
                StopCoroutine(mFlashCoroutine);
            }

            mFlashCoroutine = StartCoroutine(FlashRoutine());


            if (Health <= 0)
            {
                Die();
            }
        }

        public void Die()
        {
            if (mIsDead) return;
            SpatialGrid.Instance.RemoveEnemy(this, SpatialGrid.Instance.GetGridPos(transform.position));
            mIsDead = true;

            SpawnExperience();
            EnemyPool.Instance.ReturnEnemy(this);
        }


        public void Tick()
        {
            if (mTarget == null) return;

            mCurrentPosition = transform.position;
            var lTargetPosition = mTarget.position;

            Vector3 lDirection = (lTargetPosition - mCurrentPosition).normalized;
            mCurrentPosition += Speed * Time.deltaTime * lDirection;

            SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPosition, mCurrentPosition);
            mLastPosition = mCurrentPosition;

            if ((Time.frameCount + mInstanceUpdateOrder) % 4 == 0) // Spread separation calculations over 4 frames to reduce CPU load
            {
                HandleSeparation();
            }

            transform.position = mCurrentPosition;

            float lTilt = Mathf.Sin((Time.time + mAnimationOffset) * mTiltSpeed) * mTiltAngle;
            transform.rotation = Quaternion.Euler(0, 0, lTilt);

            if ((mCurrentPosition - lTargetPosition).sqrMagnitude < 0.25f) // 0.5f * 0.5f
            {
                Die();
            }
        }

        private void SpawnExperience()
        {
            if (ExperiencePool.Instance == null) return;

            Experience lExperience = ExperiencePool.Instance.GetExperience();
            if (lExperience != null)
            {
                lExperience.mCurrentPosition = mCurrentPosition;
                lExperience.Init();
            }
        }

        private void HandleSeparation()
        {
            SpatialGrid lGrid = SpatialGrid.Instance;
            Vector2Int lGridPosition = lGrid.GetGridPos(mCurrentPosition);
            mSeparationForce = Vector3.zero;

            int lGx = lGridPosition.x;
            int lGy = lGridPosition.y;

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    HashSet<Enemy> lNeighbors = lGrid.GetEnemiesInCell(lGx + x, lGy + y);

                    if (lNeighbors == null) continue;

                    HandleNeighbors(lNeighbors);
                }
            }
            mCurrentPosition += (mSeparationStrength * 4f) * Time.deltaTime * mSeparationForce;
        }

        private void HandleNeighbors(HashSet<Enemy> pNeighbors)
        {
            float lSqrSeparationRadius = mSeparationRadius * mSeparationRadius;

            foreach (Enemy lOther in pNeighbors)
            {
                if (lOther == this) continue;

                Vector3 lDiff = mCurrentPosition - lOther.mCurrentPosition;
                float lSqrDist = lDiff.sqrMagnitude;

                if (lSqrDist < lSqrSeparationRadius && lSqrDist > 0.0001f)
                {
                    float lForce = 1f - (lSqrDist / lSqrSeparationRadius);
                    mSeparationForce += lDiff.normalized * lForce;
                }
            }
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            mRenderer.color = mHitColor;
            yield return new WaitForSeconds(mFlashDuration);
            mRenderer.color = Color.white;
            mFlashCoroutine = null;
        }

        void OnEnable()
        {
            mIsDead = false;
            mInstanceUpdateOrder = mGlobalUpdateIndex++;

            if (mRenderer != null) mRenderer.color = Color.white;
            mFlashCoroutine = null;
        }
    }
}