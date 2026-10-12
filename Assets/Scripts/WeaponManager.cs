using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    /// <summary>
    /// Singleton manager coordinating all equipped weapons on the player.
    /// Drives weapon update loops each frame, plays attack audio, and maintains weapon HUD icons.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        public List<IWeapon> mEquippedWeapons = new();

        [Header("UI References")]
        public GameObject mWeaponIconPrefab;
        public Transform mWeaponUIContainer;

        public static WeaponManager Instance { get; private set; }

        [Header("Level Pips")]
        [Tooltip("Size of a level pip under a weapon icon, in canvas units.")]
        public float mPipSize = 14f;
        public float mPipSpacing = 4f;
        public Color mPipEmptyColor = new(0f, 0f, 0f, 0.55f);
        public Color mPipFilledColor = new(1f, 1f, 1f, 0.95f);
        [Tooltip("Color of the pips once the weapon has reached its maximum level (mastery).")]
        public Color mPipMasteredColor = new(1f, 0.82f, 0.3f, 1f);

        // One row of level pips per equipped weapon, under its inventory icon
        private readonly Dictionary<IWeapon, Image[]> mLevelPips = new();

        [Header("Stat Boosts Column")]
        [Tooltip("Horizontal gap between the weapon column and the stat boost column, in canvas units.")]
        public float mStatColumnGap = 40f;

        // Level of each stat boost taken, and its pips in the stat column (right of the weapon column)
        private readonly Dictionary<StatBoostData, int> mStatLevels = new();
        private readonly Dictionary<StatBoostData, Image[]> mStatPips = new();
        private Transform mStatUIContainer;

        /// <summary>
        /// Registers a newly equipped weapon, initializes its data, and adds its icon to the UI.
        /// </summary>
        /// <param name="pWeaponObj">GameObject instance containing the weapon component.</param>
        /// <param name="pData">Configuration data for the weapon.</param>
        public void AddWeapon(GameObject pWeaponObj, WeaponUpgradeData pData)
        {
            if (pWeaponObj.TryGetComponent(out IWeapon lWeapon))
            {
                lWeapon.Initialize(pData);
                mEquippedWeapons.Add(lWeapon);
                AddWeaponToUI(lWeapon, pData);
                RefreshWeaponLevels();
            }
            else
            {
                Debug.LogError($"ERROR: The prefab of {pData.Name} does not implement IWeapon!");
            }
        }

        /// <summary>
        /// Instantiates a weapon icon on the HUD UI container.
        /// </summary>
        /// <param name="pData">Weapon data containing the icon sprite.</param>
        private void AddWeaponToUI(IWeapon pWeapon, WeaponUpgradeData pData)
        {
            if (mWeaponIconPrefab != null && mWeaponUIContainer != null)
            {
                GameObject lIconObj = Instantiate(mWeaponIconPrefab, mWeaponUIContainer);

                if (lIconObj.TryGetComponent(out Image lImage))
                {
                    lImage.sprite = pData.Icon;
                }

                mLevelPips[pWeapon] = CreateLevelPips((RectTransform)lIconObj.transform, pData.MaxLevel);
            }
        }

        /// <summary>
        /// Creates a row of level pips under a weapon icon, one per possible upgrade.
        /// </summary>
        /// <param name="pIcon">The weapon icon in the inventory.</param>
        /// <param name="pCount">Number of pips: the maximum level of the weapon.</param>
        /// <returns>The pip images, from left to right.</returns>
        private Image[] CreateLevelPips(RectTransform pIcon, int pCount)
        {
            var lPips = new Image[pCount];
            float lRowWidth = pCount * mPipSize + (pCount - 1) * mPipSpacing;

            for (int i = 0; i < pCount; i++)
            {
                var lPipObject = new GameObject($"LevelPip{i + 1}", typeof(RectTransform), typeof(Image));
                var lRect = (RectTransform)lPipObject.transform;
                lRect.SetParent(pIcon, false);

                // Anchored under the icon, centered horizontally
                lRect.anchorMin = lRect.anchorMax = new Vector2(0.5f, 0f);
                lRect.pivot = new Vector2(0f, 1f);
                lRect.sizeDelta = new Vector2(mPipSize, mPipSize);
                lRect.anchoredPosition = new Vector2(-lRowWidth / 2f + i * (mPipSize + mPipSpacing), -mPipSpacing);

                lPips[i] = lPipObject.GetComponent<Image>();
                lPips[i].raycastTarget = false;
            }
            return lPips;
        }

        /// <summary>
        /// Fills the level pips of every equipped weapon according to its level; all pips turn gold at mastery.
        /// </summary>
        public void RefreshWeaponLevels()
        {
            foreach (var (lWeapon, lPips) in mLevelPips)
            {
                ColorPips(lPips, lWeapon.WeaponLevel, lWeapon.Data != null ? lWeapon.Data.MaxLevel : lPips.Length);
            }
        }

        /// <summary>
        /// Colors a row of pips: filled up to the level, all gold once the maximum level is reached.
        /// </summary>
        private void ColorPips(Image[] pPips, int pLevel, int pMaxLevel)
        {
            bool lMastered = pLevel >= pMaxLevel;
            for (int i = 0; i < pPips.Length; i++)
            {
                pPips[i].color = lMastered ? mPipMasteredColor
                    : i < pLevel ? mPipFilledColor
                    : mPipEmptyColor;
            }
        }

        /// <summary>
        /// Current level of a stat boost: the number of times it has been taken.
        /// </summary>
        /// <param name="pStat">The stat boost.</param>
        /// <returns>Its level, zero if never taken.</returns>
        public int GetStatLevel(StatBoostData pStat)
        {
            return mStatLevels.TryGetValue(pStat, out int lLevel) ? lLevel : 0;
        }

        /// <summary>
        /// Records one more level of a stat boost and shows it in the stat column: its icon appears the first time,
        /// then its pips fill up.
        /// </summary>
        /// <param name="pStat">The stat boost taken.</param>
        /// <returns>True if the boost has just reached its maximum level (mastery).</returns>
        public bool AddStatLevel(StatBoostData pStat)
        {
            int lLevel = GetStatLevel(pStat) + 1;
            mStatLevels[pStat] = lLevel;

            if (!mStatPips.ContainsKey(pStat))
            {
                Transform lContainer = GetStatContainer();
                if (lContainer != null && mWeaponIconPrefab != null)
                {
                    GameObject lIconObj = Instantiate(mWeaponIconPrefab, lContainer);
                    if (lIconObj.TryGetComponent(out Image lImage)) lImage.sprite = pStat.Icon;
                    mStatPips[pStat] = CreateLevelPips((RectTransform)lIconObj.transform, pStat.MaxLevel);
                }
            }

            if (mStatPips.TryGetValue(pStat, out Image[] lPips))
            {
                ColorPips(lPips, lLevel, pStat.MaxLevel);
            }
            return lLevel == pStat.MaxLevel;
        }

        /// <summary>
        /// Returns the stat column, created the first time as a copy of the weapon column (same layout), shifted to
        /// its right.
        /// </summary>
        private Transform GetStatContainer()
        {
            if (mStatUIContainer != null || mWeaponUIContainer == null) return mStatUIContainer;

            GameObject lColumn = Instantiate(mWeaponUIContainer.gameObject, mWeaponUIContainer.parent);
            lColumn.name = "StatInventory";
            foreach (Transform lChild in lColumn.transform)
            {
                Destroy(lChild.gameObject);
            }

            if (lColumn.TryGetComponent(out HorizontalOrVerticalLayoutGroup lLayout))
            {
                float lIconWidth = ((RectTransform)mWeaponIconPrefab.transform).sizeDelta.x;
                lLayout.padding.left += Mathf.RoundToInt(lIconWidth + mStatColumnGap);
            }

            mStatUIContainer = lColumn.transform;
            return mStatUIContainer;
        }

        /// <summary>
        /// Updates cooldowns and attack logic for all equipped weapons.
        /// Plays attack sounds if a weapon executes an attack during the frame.
        /// </summary>
        public void UpdateWeapons()
        {
            float lDeltaTime = Time.deltaTime;

            foreach (var lWeapon in mEquippedWeapons)
            {
                bool lHasAttacked = lWeapon.UpdateWeapon(lDeltaTime);

                if (lHasAttacked && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWeaponSound(lWeapon.AttackSound);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            WeaponUpgradeData lSwordData = LevelUpManager.Instance.mAllAvailableUpgrades
                .OfType<WeaponUpgradeData>()
                .FirstOrDefault(lWeapon => lWeapon.Name == "Sword");

            if (lSwordData != null)
            {
                Player.Instance.EquipWeapon(lSwordData);
            }
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