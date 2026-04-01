using UnityEngine;

public class Experience : MonoBehaviour
{
    private Transform mPlayerTransform;
    private bool mIsTrigger;
    public float mValue = 1f;
    public float mSpeed = 8f;

    public bool IsTrigger => mIsTrigger;

    public void Init()
    {
        mIsTrigger = false;
    }
    public void SetTarget(Transform pPlayerTransform)
    {
        mPlayerTransform = pPlayerTransform;
    }

    public void Trigger()
    {
        mIsTrigger = true;
    }

    public void Collect(ExperienceBar pExperienceBar)
    {
        pExperienceBar.AddExperience(mValue);
        ExperiencePool.Instance.ReturnExperience(this);
    }

    void OnEnable()
    {
        mIsTrigger = false;
        if (Player.Instance != null)
            Player.Instance.mExperienceList.Add(this);
    }

    void OnDisable()
    {
        if (Player.Instance != null)
            Player.Instance.mExperienceList.Remove(this);

        mPlayerTransform = null;
    }
}
