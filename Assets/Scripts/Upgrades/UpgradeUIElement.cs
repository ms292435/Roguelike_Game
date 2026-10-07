using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    /// <summary>
    /// UI component representing a single selectable upgrade card on the level-up interface.
    /// Displays the title, description, icon, and notifies LevelUpManager when selected.
    /// </summary>
    public class UpgradeUIElement : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI mTitleText;
        [SerializeField] private TextMeshProUGUI mDescriptionText;
        [SerializeField] private Image mIconImage;

        private UpgradeData mAssignedData;
        private LevelUpManager mManager;

        /// <summary>
        /// Configures the card view with upgrade data and a reference to the LevelUpManager.
        /// </summary>
        /// <param name="pData">The upgrade data associated with this button.</param>
        /// <param name="pManager">Reference to the active LevelUpManager instance.</param>
        /// <param name="pDescription">Optional dynamic description overriding default text.</param>
        public void Setup(UpgradeData pData, LevelUpManager pManager, string pDescription = null)
        {
            mAssignedData = pData;
            mManager = pManager;

            mTitleText.text = pData.Name;
            mDescriptionText.text = !string.IsNullOrEmpty(pDescription) ? pDescription : pData.Description; 
            mIconImage.sprite = pData.Icon;
        }

        /// <summary>
        /// Event handler called by the UI Button click to select and apply this upgrade.
        /// </summary>
        public void OnClickSelect()
        {
            mManager.ApplyUpgrade(mAssignedData);
        }
    }
}