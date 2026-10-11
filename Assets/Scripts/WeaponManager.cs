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
                AddWeaponToUI(pData);
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
        private void AddWeaponToUI(WeaponUpgradeData pData)
        {
            if (mWeaponIconPrefab != null && mWeaponUIContainer != null)
            {
                GameObject lIconObj = Instantiate(mWeaponIconPrefab, mWeaponUIContainer);

                if (lIconObj.TryGetComponent(out Image lImage))
                {
                    lImage.sprite = pData.Icon;
                }
            }
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