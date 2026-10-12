using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Singleton manager handling player level-up progression, choice generation, and modal UI display.
    /// Implements IObserver to listen for "LevelUp" events from the Player subject.
    /// </summary>
    public class LevelUpManager : MonoBehaviour, IObserver
    {
        public TextMeshProUGUI mLevelText;
        public List<UpgradeData> mAllAvailableUpgrades;

        public GameObject mUpgradeButtonPrefab;
        public GameObject mLevelUpPanel;
        public Transform mUpgradeUIContainer;

        public static LevelUpManager Instance { get; private set; }

        private int mCurrentLevel = 1;
        private readonly Dictionary<UpgradeData, System.Action> mPendingActions = new();

        /// <summary>
        /// Observer callback invoked when an observed subject (Player) raises an event.
        /// </summary>
        /// <param name="pSubject">The event subject.</param>
        /// <param name="pEventName">The event identifier.</param>
        public void OnNotify(ISubject pSubject, string pEventName)
        {
            if (pEventName == "LevelUp")
            {
                OnLevelUp();
            }
        }

        /// <summary>
        /// Handles level increment, pauses game timescale and rolls random upgrade choices.
        /// </summary>
        public void OnLevelUp()
        {
            mCurrentLevel++;

            if (mLevelText != null)
                mLevelText.text = $"Level: {mCurrentLevel}";

            Time.timeScale = 0f;
            List<UpgradeData> lSelectedChoices = GetRandomUpgrades(3);
            ShowUpgradeOptions(lSelectedChoices);
        }

        /// <summary>
        /// Instantiates upgrade card buttons for the rolled choices and displays the level-up panel.
        /// </summary>
        /// <param name="pChoices">List of upgrade options to present.</param>
        public void ShowUpgradeOptions(List<UpgradeData> pChoices)
        {
            mPendingActions.Clear();
            foreach (Transform lChild in mUpgradeUIContainer)
            {
                Destroy(lChild.gameObject);
            }

            foreach (UpgradeData lData in pChoices)
            {
                GameObject lGameObject = Instantiate(mUpgradeButtonPrefab, mUpgradeUIContainer);
                var lUIElement = lGameObject.GetComponent<UpgradeUIElement>();

                string lDisplayDescription = lData.Description;

                if (lData is WeaponUpgradeData lWeaponData)
                {
                    var lExisting = WeaponManager.Instance.mEquippedWeapons
                        .FirstOrDefault(w => w.Data == lWeaponData);

                    if (lExisting != null)
                    {
                        var lProposal = lExisting.GetNextUpgradeProposal();
                        lDisplayDescription = lProposal.Description;
                        mPendingActions[lData] = lProposal.ApplyAction;
                    }
                }
                lUIElement.Setup(lData, this, lDisplayDescription);
            }

            mLevelUpPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Applies the selected upgrade (stat boost, new weapon, or weapon level-up) and resumes gameplay.
        /// </summary>
        /// <param name="pData">The chosen UpgradeData.</param>
        public void ApplyUpgrade(UpgradeData pData)
        {
            if (mPendingActions.ContainsKey(pData))
            {
                mPendingActions[pData].Invoke();
            }
            else if (pData is StatBoostData lStatBoost)
            {
                switch (lStatBoost.type)
                {
                    case "Health":
                        Player.Instance.BoostMaxHealth(lStatBoost.multiplier);
                        break;
                    case "Damage":
                        Player.Instance.Damage *= lStatBoost.multiplier;
                        break;
                    case "Speed":
                        Player.Instance.Speed *= lStatBoost.multiplier;
                        break;
                }
            }
            else if (pData is WeaponUpgradeData lWeaponUpgrade)
            {
                Player.Instance.EquipWeapon(lWeaponUpgrade);
            }

            mLevelUpPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Selects a randomized subset of available upgrades, filtering out max-level weapons.
        /// </summary>
        /// <param name="pCount">Number of choices to return.</param>
        /// <returns>Filtered and randomized upgrade list.</returns>
        private List<UpgradeData> GetRandomUpgrades(int pCount)
        {
            return mAllAvailableUpgrades
                .Where(lUpgrade =>
                {
                    if (lUpgrade is WeaponUpgradeData lWeaponData)
                    {
                        var lEquipped = WeaponManager.Instance.mEquippedWeapons
                            .FirstOrDefault(w => w.Data == lWeaponData);

                        return lEquipped == null || lEquipped.WeaponLevel < lWeaponData.MaxLevel;
                    }
                    return true;
                })
                .OrderBy(l => Random.value)
                .Take(pCount)
                .ToList();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mLevelText != null)
                mLevelText.text = $"Level: {mCurrentLevel}";
        }

        private void Start()
        {
            if (Player.Instance != null)
                Player.Instance.AddObserver(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
