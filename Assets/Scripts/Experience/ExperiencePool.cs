using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class ExperiencePool : MonoBehaviour
    {
        public GameObject mExperiencePrefab;

        public int ExperiencePoolCount { get; set; } = 2000;

        public Queue<Experience> mListExperiencePool = new();

        private int mTotalCreated = 0;
        private int mActiveCount = 0;
        private readonly int mMaxActiveExperience = 100;

        public static ExperiencePool Instance { get; private set; }

        public Experience GetExperience()
        {
            if (mListExperiencePool.Count > 0)
            {
                Experience lExperience = mListExperiencePool.Dequeue();
                lExperience.gameObject.SetActive(true);
                mActiveCount++;
                return lExperience;
            }


            if (mTotalCreated < mMaxActiveExperience)
            {
                Experience lNewExperience = Instantiate(mExperiencePrefab).GetComponent<Experience>();
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
            mActiveCount--;
        }

        void Awake()
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
                lExperience.gameObject.SetActive(false);
                mListExperiencePool.Enqueue(lExperience);
                mTotalCreated++;
            }

        }

        void OnGUI()
        {
            GUIStyle lStyle = new();
            int lWidth = Screen.width, lHeight = Screen.height;
            Rect lRect = new(0, 0, lWidth, 30);
            lStyle.alignment = TextAnchor.UpperRight;
            lStyle.fontSize = lHeight * 2 / 100;
            lStyle.normal.textColor = Color.white;
            GUI.Label(lRect, $"Active Experience: {mActiveCount} / Total Created: {mTotalCreated}", lStyle);
        }
    }
}