using System.Security.Cryptography;
using UnityEngine;

public class Experience : MonoBehaviour
{
    private Transform mPlayerTransform;
    public bool mIsTrigger;
    public float mValue = 1f;
    public float mSpeed = 8f;
    private Vector3 mLastPosition; 
    public Vector3 mCurrentPosition;
    public bool IsTrigger => mIsTrigger;

    public void Init()
    {
        mIsTrigger = false;
        mCurrentPosition = transform.position;
        mLastPosition = mCurrentPosition;

        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.AddExperience(this, SpatialGrid.Instance.GetGridPos(mCurrentPosition));
        }
    }

    private void Update()
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
    public void SetTarget(Transform pPlayerTransform)
    {
        mPlayerTransform = pPlayerTransform;
    }

    public void Trigger()
    {
        mIsTrigger = true;
        SpatialGrid.Instance.RemoveExperience(this, SpatialGrid.Instance.GetGridPos(mCurrentPosition));
    }

    public void Collect(ExperienceBar pExperienceBar)
    {
        if (pExperienceBar != null)
        {
            pExperienceBar.AddExperience(mValue);
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
}
