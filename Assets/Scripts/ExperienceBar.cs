using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    public class ExperienceBar : MonoBehaviour
    {
        public Image mXpFill;

        private float mCurrentXP = 0;
        private float mMaxXP = 1;

        public static ExperienceBar Instance { get; private set; }

        public void AddExperience(float pAmount)
        {
            mCurrentXP += pAmount;

            while (mCurrentXP >= mMaxXP)
            {
                mCurrentXP -= mMaxXP;
                LevelUp();
            }

            UpdateExperienceBar();
        }

        private void UpdateExperienceBar()
        {
            mXpFill.fillAmount = mCurrentXP / mMaxXP;
        }

        private void LevelUp()
        {
            mMaxXP *= 1.5f;
            mMaxXP = Mathf.Round(mMaxXP);
            LevelUpManager.Instance.OnLevelUp();
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
            UpdateExperienceBar();
        }

        void OnGUI()
        {
            GUIStyle lStyle = new();

            int lWidth = Screen.width, lHeight = Screen.height;

            Rect lRect = new(0, lHeight - 50, lWidth, 30);
            lStyle.alignment = TextAnchor.MiddleCenter;
            lStyle.fontSize = lHeight * 2 / 100;
            lStyle.normal.textColor = Color.white;


            GUI.Label(lRect, $"XP: {mCurrentXP} / {mMaxXP}", lStyle);
        }
    }
}