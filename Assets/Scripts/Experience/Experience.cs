using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Experience : MonoBehaviour
{
    private Transform mPlayerTransform;
    private bool mIsTrigger;

    public float mValue = 1f;
    public float mSpeed = 8f;

    public bool IsTrigger => mIsTrigger; // Accesseur pour le Player

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

    public void Collect(ExperienceBar xpBar)
    {
        xpBar.AddExperience(mValue);
        ExperiencePool.Instance.ReturnExperience(this);
    }

    void OnEnable()
    {
        mIsTrigger = false;
        // Accès direct sans recherche Scan-scène
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
