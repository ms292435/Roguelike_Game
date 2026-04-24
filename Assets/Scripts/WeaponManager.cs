using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    public class WeaponManager : MonoBehaviour
    {
        public List<IWeapon> mEquippedWeapons = new();

        [Header("UI References")]
        public GameObject mWeaponIconPrefab;

        public Transform mWeaponUIContainer;

        public static WeaponManager Instance { get; private set; }

        public void AddWeapon(GameObject pWeaponObj, WeaponUpgradeData pData)
        {
            if (pWeaponObj.TryGetComponent(out IWeapon lWeapon))
            {
                lWeapon.Initialize(pData);
                mEquippedWeapons.Add(lWeapon);

                AddWeaponToUI(pData);

                Debug.Log($"Weapon added to list : {pData.Name}");
            }
            else
            {
                Debug.LogError($"ERROR: The prefab of {pData.Name} does not have an IWeapon script at its root!");
            }
        }

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

        public void UpdateWeapons()
        {
            float lDeltaTime = Time.deltaTime;

            foreach (var lWeapon in mEquippedWeapons)
            {
                // UpdateWeapon will return true if the weapon has performed an attack during this update cycle
                bool lHasAttacked = lWeapon.UpdateWeapon(lDeltaTime);

                if (lHasAttacked)
                {
                    AudioManager.Instance.PlayWeaponSound(lWeapon.AttackSound);
                }
            }
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
            WeaponUpgradeData lSwordData = LevelUpManager.Instance.mAllAvailableUpgrades.OfType<WeaponUpgradeData>().FirstOrDefault(lWeapon => lWeapon.Name == "Sword");

            if (lSwordData != null)
            {
                Player.Instance.EquipWeapon(lSwordData);
            }
        }
    }
}