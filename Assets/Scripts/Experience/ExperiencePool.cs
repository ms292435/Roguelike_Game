using System.Collections;
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

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < ExperiencePoolCount; i++)
        {
            Experience exp = Instantiate(mExperiencePrefab).GetComponent<Experience>();
            exp.Init();
            exp.gameObject.SetActive(false);
            mListExperiencePool.Enqueue(exp);
            mTotalCreated++;
        }

    }

    public Experience GetExperience()
    {
        if (mListExperiencePool.Count > 0)
        {
            Experience exp = mListExperiencePool.Dequeue();
            exp.gameObject.SetActive(true);
            return exp;
        }


        if (mTotalCreated < mMaxActiveExperience)
        {
            Experience newExp = Instantiate(mExperiencePrefab).GetComponent<Experience>();
            newExp.Init();

            mTotalCreated++; 
            return newExp;
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

    public void ReturnExperience(Experience exp)
    {
        exp.gameObject.SetActive(false);
        mListExperiencePool.Enqueue(exp);
    }
}
