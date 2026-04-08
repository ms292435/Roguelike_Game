using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Roguelike
{
    public class LevelUpManager : MonoBehaviour
    {
        public CinemachineVirtualCamera mCamera;

        public List<UpgradeData> mAllAvailableUpgrades;

        public GameObject mUpgradeButtonPrefab;
        public GameObject mLevelUpPanel;

        public Transform mUpgradeUIContainer;

        public static LevelUpManager Instance { get; private set; }

        private int mCurrentLevel = 1;
        public void OnLevelUp()
        {
            mCurrentLevel++;
            Time.timeScale = 0f;
            List<UpgradeData> lSelectedChoices = GetRandomUpgrades(3);
            ShowUpgradeOptions(lSelectedChoices);
            if (mCurrentLevel % 2 == 0)
            {
                StopAllCoroutines(); // Avoid multiple zooms stacking if the player levels up multiple times quickly
                float lTargetSize = mCamera.m_Lens.OrthographicSize * 1.15f; // +15% dezoom
                StartCoroutine(SlowZoomRoutine(lTargetSize, 5.0f)); // 5.0f = seconds 
            }
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
                lGameObject.GetComponent<UpgradeUIElement>().Setup(lData, this);
            }

            mLevelUpPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void ApplyUpgrade(UpgradeData pData)
        {
            if (pData is StatBoostData lStatBoost)
            {
                switch (lStatBoost.type)
                {
                    case "Health":
                        Player.Instance.Health *= lStatBoost.multiplier;
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

        private List<UpgradeData> GetRandomUpgrades(int pCount)
        {
            return mAllAvailableUpgrades
            .Where(lUpgrade =>
            {
                if (!(lUpgrade is WeaponUpgradeData lWeaponData)) return true;

                return !WeaponManager.Instance.mEquippedWeapons.Any(lWeapon => lWeapon.Data == lWeaponData);
            })
            .OrderBy(l => Random.value)
            .Take(pCount)
            .ToList();
        }

        private IEnumerator SlowZoomRoutine(float pTargetSize, float pDuration)
        {
            float lStartSize = mCamera.m_Lens.OrthographicSize;
            float lElapsed = 0f;

            while (lElapsed < pDuration)
            {
                lElapsed += Time.unscaledDeltaTime; // Use unscaled time to ignore Time.timeScale

                // Compute the percentage of completion
                float lPercent = lElapsed / pDuration;

                // SmoothStep for a smoother dezoom effect
                mCamera.m_Lens.OrthographicSize = Mathf.SmoothStep(lStartSize, pTargetSize, lPercent);

                yield return null; // Wait for the next frame
            }

            mCamera.m_Lens.OrthographicSize = pTargetSize;
        }

        void Awake()
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

        void OnGUI()
        {
            GUIStyle lStyle = new();
            int lWidth = Screen.width, lHeight = Screen.height;
            Rect lRect = new(0, 0, lWidth, 30);
            lStyle.alignment = TextAnchor.UpperCenter;
            lStyle.fontSize = lHeight * 2 / 100;
            lStyle.normal.textColor = Color.white;
            GUI.Label(lRect, $"Level: {mCurrentLevel}", lStyle);
        }
    }
}