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

        /// <summary>
        /// True while the level-up choice is displayed (the game is paused).
        /// </summary>
        public bool IsChoosingUpgrade => mLevelUpPanel != null && mLevelUpPanel.activeSelf;

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

            List<UpgradeData> lSelectedChoices = GetRandomUpgrades(3);

            // Every upgrade is maxed out: nothing to choose, so the game goes on without opening the panel
            if (lSelectedChoices.Count == 0) return;

            Time.timeScale = 0f;
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

                        // The last upgrade of a weapon also unlocks its mastery bonus
                        if (lExisting.WeaponLevel == lWeaponData.MaxLevel - 1)
                        {
                            lDisplayDescription += "\nMAX LEVEL: all stats boosted!";
                        }
                        mPendingActions[lData] = lProposal.ApplyAction;
                    }
                }
                else if (lData is StatBoostData lStatData
                         && WeaponManager.Instance.GetStatLevel(lStatData) == lStatData.MaxLevel - 1)
                {
                    // The last boost of a stat also unlocks its mastery bonus
                    lDisplayDescription += "\nMAX LEVEL: " + Player.GetStatMasteryDescription(lStatData.type);
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

                // A weapon reaching its maximum level gets its mastery bonus, once
                if (pData is WeaponUpgradeData lUpgradedWeapon)
                {
                    var lWeapon = WeaponManager.Instance.mEquippedWeapons.FirstOrDefault(w => w.Data == lUpgradedWeapon);
                    if (lWeapon != null && lWeapon.WeaponLevel == lUpgradedWeapon.MaxLevel)
                    {
                        lWeapon.ApplyMastery();
                    }

                    WeaponManager.Instance.RefreshWeaponLevels();
                }
            }
            else if (pData is StatBoostData lStatBoost)
            {
                Player.Instance.ApplyStatBoost(lStatBoost.type, lStatBoost.multiplier - 1f);

                // A stat boost reaching its maximum level grants its mastery bonus, once
                if (WeaponManager.Instance.AddStatLevel(lStatBoost))
                {
                    Player.Instance.ApplyStatMastery(lStatBoost.type);
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

                    // Stat boosts at their maximum level, or capped stats (speed), are no longer offered
                    if (lUpgrade is StatBoostData lStatBoost)
                    {
                        return WeaponManager.Instance.GetStatLevel(lStatBoost) < lStatBoost.MaxLevel
                               && Player.Instance.CanBoostStat(lStatBoost.type);
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
