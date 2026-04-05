using UnityEngine;

namespace Roguelike
{
    public class Experience : MonoBehaviour
    {
        public bool mIsTrigger;

        public float mValue = 1f;
        public float mSpeed = 8f;

        public Vector3 mCurrentPosition;
        public bool IsTrigger => mIsTrigger;

        private Vector3 mLastPosition;

        public void Init()
        {
            mIsTrigger = false;
            transform.position = mCurrentPosition;
            mLastPosition = mCurrentPosition;

            if (SpatialGrid.Instance != null)
            {
                SpatialGrid.Instance.AddExperience(this, SpatialGrid.Instance.GetGridPos(mCurrentPosition));
            }
        }


        public void Trigger()
        {
            mIsTrigger = true;
            SpatialGrid.Instance.RemoveExperience(this, SpatialGrid.Instance.GetGridPos(mCurrentPosition));
        }

        public void Collect()
        {
            if (ExperienceBar.Instance != null)
            {
                ExperienceBar.Instance.AddExperience(mValue);
            }

            if (!mIsTrigger && SpatialGrid.Instance != null)
            {
                SpatialGrid.Instance.RemoveExperience(this, SpatialGrid.Instance.GetGridPos(mCurrentPosition));
            }

            ExperiencePool.Instance.ReturnExperience(this);
        }

        void OnDisable()
        {
            if (SpatialGrid.Instance != null)
            {
                SpatialGrid.Instance.RemoveExperience(this, SpatialGrid.Instance.GetGridPos(mLastPosition));
            }
        }

        void Update()
        {
            if (mIsTrigger)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    Player.Instance.transform.position,
                    mSpeed * Time.deltaTime
                );
                mCurrentPosition = transform.position;
                mLastPosition = mCurrentPosition;
            }
        }
    }
}