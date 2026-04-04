using UnityEngine;
using UnityEngine.UI;

public class ExperienceBar : MonoBehaviour
{
    public Image mXpFill;

    private float mCurrentXP = 0;    
    private float mMaxXP = 1;

    public static ExperienceBar Instance { get; private set; }
    private void Start()
    {
        UpdateExperiencePBar();
    }

    public void AddExperience(float pAmount)
    {
        mCurrentXP += pAmount;

        while (mCurrentXP >= mMaxXP)
        {
            mCurrentXP -= mMaxXP;
            LevelUp();
        }

        UpdateExperiencePBar();
    }

    void UpdateExperiencePBar()
    {
        mXpFill.fillAmount = mCurrentXP / mMaxXP;
    }

    void LevelUp()
    {
        mMaxXP *= 1.5f;
        mMaxXP = Mathf.Round(mMaxXP);
        LevelUpManager.Instance.OnLevelUp();
    }

    private void OnGUI()
    {
        GUIStyle lStyle = new GUIStyle();

        int lWidth = Screen.width, lHeight = Screen.height;

        Rect lRect = new Rect(0, lHeight - 50, lWidth, 30);
        lStyle.alignment = TextAnchor.MiddleCenter;
        lStyle.fontSize = lHeight * 2 / 100;
        lStyle.normal.textColor = Color.white;


        GUI.Label(lRect, $"XP: {mCurrentXP} / {mMaxXP}", lStyle);
    }
}
