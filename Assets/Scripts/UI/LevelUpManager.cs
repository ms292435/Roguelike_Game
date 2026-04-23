using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Roguelike
{
    public class LevelUpManager : MonoBehaviour, IObserver
    {
        public CinemachineVirtualCamera mCamera;

        public TextMeshProUGUI mLevelText;

        public List<UpgradeData> mAllAvailableUpgrades;

        public GameObject mUpgradeButtonPrefab;
        public GameObject mLevelUpPanel;

        public Transform mUpgradeUIContainer;

        public static LevelUpManager Instance { get; private set; }

        private int mCurrentLevel = 1;

        private readonly Dictionary<UpgradeData, System.Action> mPendingActions = new();

        private static float mMaxCameraDezoom = 30f;
        public void OnNotify(ISubject pSubject, string pEventName)
        {
            if (pEventName == "LevelUp")
            {
                OnLevelUp();
            }
        }
        public void OnLevelUp()
        {
            mCurrentLevel++;

            if (mLevelText != null)
                mLevelText.text = $"Level: {mCurrentLevel}";

            Time.timeScale = 0f;
            List<UpgradeData> lSelectedChoices = GetRandomUpgrades(3);
            ShowUpgradeOptions(lSelectedChoices);

            if (mCurrentLevel % 2 == 0)
            {
                StopAllCoroutines(); // Avoid multiple zooms stacking if the player levels up multiple times quickly
                if (mCamera.m_Lens.OrthographicSize < mMaxCameraDezoom)
                {
                    float lTargetSize = mCamera.m_Lens.OrthographicSize * 1.15f; // +15% dezoom
                    StartCoroutine(SlowZoomRoutine(lTargetSize, 5.0f)); // 5.0f = seconds 
                }
            }
        }

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
                if (lUpgrade is WeaponUpgradeData lWeaponData)
                {
                    // Search if we already have this weapon equipped
                    var lEquipped = WeaponManager.Instance.mEquippedWeapons
                                    .FirstOrDefault(w => w.Data == lWeaponData);

                    // Keep it only if we don't have it or if it's not max level yet
                    return lEquipped == null || lEquipped.WeaponLevel < lWeaponData.MaxLevel;
                }
                return true;
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
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
            if (mLevelText != null)
                mLevelText.text = $"Level: {mCurrentLevel}";
        }
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        void Start()
        {
            Player.Instance.AddObserver(this);
        }
    }
}