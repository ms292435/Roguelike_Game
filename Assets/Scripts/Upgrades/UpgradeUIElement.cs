using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    public class UpgradeUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mTitleText;
        [SerializeField] private TextMeshProUGUI mDescriptionText;

        [SerializeField] private Image mIconImage;

        private UpgradeData mAssignedData;

        private LevelUpManager mManager;

        public void Setup(UpgradeData pData, LevelUpManager pManager)
        {
            mAssignedData = pData;
            mManager = pManager;

            mTitleText.text = pData.Name;
            mDescriptionText.text = pData.Description;
            mIconImage.sprite = pData.Icon;
        }

        public void OnClickSelect()
        {
            mManager.ApplyUpgrade(mAssignedData);
        }
    }
}