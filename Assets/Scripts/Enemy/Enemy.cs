using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Roguelike
{
    public class Enemy : Entity
    {
        public Vector3 mCurrentPosition;

        public AudioClip mDeathSound;

        private Vector3 mLastPosition;
        private Vector3 mSeparationForce;

        private Transform mTarget;

        private bool mIsDead = false;

        private float mAnimationOffset; // Desync ennemies
        private readonly float mTiltAngle = 5f;    // Max angle of the tilt
        private readonly float mTiltSpeed = 5f;    // Tilt speed

        private readonly float mSeparationRadius = 1f; // Min distance to maintain from other enemies

        private static int mGlobalUpdateIndex = 0;
        private int mInstanceUpdateOrder;

        private Vector3 mSmoothedSeparation;

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
            if (mIsDead || !gameObject.activeInHierarchy) return;

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

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyDeathSound(mDeathSound);
            }

            SpatialGrid.Instance.RemoveEnemy(this, SpatialGrid.Instance.GetGridPos(transform.position));
            mIsDead = true;

            SpawnExperience();
            EnemyPool.Instance.ReturnEnemy(this);
        }


        public void Tick()
        {
            if (mTarget == null) return;

            Profiler.BeginSample("1_Enemy_Maths_Boids");

            mCurrentPosition = transform.position;
            var lTargetPosition = mTarget.position;

            // Compute the base direction towards the player
            Vector3 lTargetDirection = (lTargetPosition - mCurrentPosition).normalized;

            if ((Time.frameCount + mInstanceUpdateOrder) % 4 == 0) // Spread separation calculations over 4 frames to reduce CPU load
            {
                HandleSeparation();
            }

            // Interpolate the separation force to prevent jittering and teleporting effects
            mSmoothedSeparation = Vector3.Lerp(mSmoothedSeparation, mSeparationForce, Time.deltaTime * 10f);

            // Combine target seeking and separation. 
            // Separation is weighted heavily (x4.0) to prioritize avoiding neighbors and prevent clumping
            Vector3 lFinalDirection = (lTargetDirection + (mSmoothedSeparation * 4.0f)).normalized;

            // Apply movement using the strict base speed to maintain a steady, fluid horde pace
            mCurrentPosition += Speed * Time.deltaTime * lFinalDirection;

            // Update the enemy's position within the spatial grid for accurate neighbor detection
            SpatialGrid.Instance.UpdateEnemyPosition(this, mLastPosition, mCurrentPosition);
            mLastPosition = mCurrentPosition;

            // Apply a procedural tilt effect based on time to animate the sprite
            float lTilt = Mathf.Sin((Time.time + mAnimationOffset) * mTiltSpeed) * mTiltAngle;

            Profiler.EndSample();

            Profiler.BeginSample("2_Enemy_Apply_Transform");

            transform.SetPositionAndRotation(mCurrentPosition, Quaternion.Euler(0, 0, lTilt));

            Profiler.EndSample();

            Profiler.BeginSample("3_Enemy_Collision_Check");

            // Check if the enemy reached the target (using squared magnitude to avoid expensive square root operations)
            if ((mCurrentPosition - lTargetPosition).sqrMagnitude < 0.25f) // 0.5f * 0.5f
            {
                Player.Instance.Health -= 5;
                Die();
            }

            Profiler.EndSample();
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

            if (mSeparationForce.sqrMagnitude > 1f)
            {
                mSeparationForce.Normalize();
            }
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
            mSmoothedSeparation = Vector3.zero;

            if (mRenderer != null) mRenderer.color = Color.white;
            mFlashCoroutine = null;
        }
    }
}