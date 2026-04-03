using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    public List<UpgradeData> mAllAvailableUpgrades; 
    public GameObject mUpgradeButtonPrefab;
    public Transform mUpgradeUIContainer; 
    public GameObject levelUpPanel;

    public static LevelUpManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void OnLevelUp()
    {
        Time.timeScale = 0f;
        List<UpgradeData> lSelectedChoices = GetRandomUpgrades(3);
        ShowUpgradeOptions(lSelectedChoices);
    }

    private List<UpgradeData> GetRandomUpgrades(int pCount)
    {
        return mAllAvailableUpgrades.OrderBy(lRandomValue => Random.value).Take(pCount).ToList();
    }

    public void ShowUpgradeOptions(List<UpgradeData> pChoices)
    {
        foreach (Transform lChild in mUpgradeUIContainer)
        {
            Destroy(lChild.gameObject);
        }

        foreach (UpgradeData lData in pChoices)
        {
            GameObject lGameObject = Instantiate(mUpgradeButtonPrefab, mUpgradeUIContainer);
            UpgradeUIElement lUIElement = lGameObject.GetComponent<UpgradeUIElement>();
            lGameObject.GetComponent<UpgradeUIElement>().Setup(lData, this);
        }

        levelUpPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ApplyUpgrade(UpgradeData pData)
    {
        if (pData.GetType() == typeof(StatBoostData))
        {
            var lStatBoostData = (StatBoostData)pData;
            switch (lStatBoostData.type)
            {
                case "Health":
                    Player.Instance.Health *= lStatBoostData.multiplier;
                    break;
                case "Damage":
                    Player.Instance.Damage *= lStatBoostData.multiplier;
                    break;
                case "Speed":
                    Player.Instance.Speed *= lStatBoostData.multiplier;
                    break;
            }
        }
        else if (pData.GetType() == typeof(WeaponUpgradeData))
        {
            switch (pData.Name)
            {
                default:
                    Debug.LogWarning("Upgrade not implemented yet : " + pData.Name);
                    break;
            }
        }

        levelUpPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
