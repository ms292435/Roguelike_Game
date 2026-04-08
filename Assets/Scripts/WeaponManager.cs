using Roguelike;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Roguelike
{
    public class WeaponManager : MonoBehaviour
    {
        public List<IWeapon> mEquippedWeapons = new();

        public static WeaponManager Instance { get; private set; }

        public void AddWeapon(GameObject pWeaponObj, WeaponUpgradeData pData)
        {
            if (pWeaponObj.TryGetComponent(out IWeapon lWeapon))
            {
                lWeapon.Initialize(pData);
                mEquippedWeapons.Add(lWeapon);
                Debug.Log($"Weapon added to list : {pData.Name}");
            }
            else
            {
                Debug.LogError($"ERROR: The prefab of {pData.Name} does not have an IWeapon script at its root!");
            }
        }

        public void UpdateWeapons()
        {
            float lDeltaTime = Time.deltaTime;

            foreach (var lWeapon in mEquippedWeapons)
            {
                lWeapon.UpdateWeapon(lDeltaTime);
            }
        }

        private void DrawWeaponList(GUIStyle pStyle)
        {
            float lIconSize = 100f;
            float lSpacing = 10f; 
            float lStartX = 20f;   
            float lStartY = 60f;   

            pStyle.alignment = TextAnchor.UpperLeft;
            GUI.Label(new Rect(lStartX, lStartY, 200, 30), "Weapons :", pStyle);

            for (int i = 0; i < mEquippedWeapons.Count; i++)
            {
                IWeapon lWeapon = mEquippedWeapons[i];

                if (lWeapon.Data != null && lWeapon.Data.Icon != null)
                {
                    WeaponUpgradeData lData = lWeapon.Data;
                    Rect lIconRect = new(lStartX + (i * (lIconSize + lSpacing)), lStartY + 50, lIconSize, lIconSize);

                    Texture2D lTexture = lData.Icon.texture;
                    Rect lSpriteRect = lData.Icon.rect;

                    Rect lTexCoords = new(
                        lSpriteRect.x / lTexture.width,
                        lSpriteRect.y / lTexture.height,
                        lSpriteRect.width / lTexture.width,
                        lSpriteRect.height / lTexture.height
                    );

                    GUI.DrawTextureWithTexCoords(lIconRect, lTexture, lTexCoords);
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
            Player.Instance.EquipWeapon(lSwordData);
        }

        void OnGUI()
        {
            GUIStyle lStyle = new();
            int lHeight = Screen.height;
            lStyle.alignment = TextAnchor.UpperCenter;
            lStyle.fontSize = lHeight * 2 / 100;
            lStyle.normal.textColor = Color.white;

            DrawWeaponList(lStyle);
        }
    }
}