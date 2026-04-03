using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUIElement : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;

    private UpgradeData assignedData;
    private LevelUpManager manager;

    public void Setup(UpgradeData data, LevelUpManager mgr)
    {
        assignedData = data;
        manager = mgr;

        titleText.text = data.Name;
        descriptionText.text = data.Description;
        iconImage.sprite = data.Icon;
    }

    public void OnClickSelect()
    {
        manager.ApplyUpgrade(assignedData);
    }
}