using System.Collections.Generic;
using UnityEngine;

public class ExperiencePool : MonoBehaviour
{
    public GameObject mExperiencePrefab;
    public int ExperiencePoolCount { get; set; } = 1000;
    public Queue<Experience> mListExperiencePool = new Queue<Experience>();
    private int mTotalCreated = 0;

    private int mMaxActiveExperience = 100;

    public static ExperiencePool Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        for (int i = 0; i < ExperiencePoolCount; i++)
        {
            Experience lExperience = Instantiate(mExperiencePrefab).GetComponent<Experience>();
            lExperience.Init();
            lExperience.gameObject.SetActive(false);
            mListExperiencePool.Enqueue(lExperience);
            mTotalCreated++;
        }

    }

    public Experience GetExperience()
    {
        if (mListExperiencePool.Count > 0)
        {
            Experience lExperience = mListExperiencePool.Dequeue();
            lExperience.gameObject.SetActive(true);
            return lExperience;
        }


        if (mTotalCreated < mMaxActiveExperience)
        {
            Experience lNewExperience = Instantiate(mExperiencePrefab).GetComponent<Experience>();
            lNewExperience.Init();

            mTotalCreated++; 
            return lNewExperience;
        }

        return null;
    }

    public int GetActiveExperienceCount()
    {
        int lCount = 0;
        foreach (Experience lExperience in mListExperiencePool)
        {
            if (lExperience.gameObject.activeInHierarchy)
                lCount++;
        }
        return lCount;
    }

    public void ReturnExperience(Experience pExperience)
    {
        pExperience.gameObject.SetActive(false);
        mListExperiencePool.Enqueue(pExperience);
    }
}
