using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// ScriptableObject configuring weapon upgrades and equipping data.
    /// Holds references to the instantiated weapon logic prefab, projectile prefab, and progression limits.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponUpgrade", menuName = "Roguelike/Upgrades/Weapon")]
    public class WeaponUpgradeData : UpgradeData
    {
        /// <summary>
        /// Prefab instantiated as a child of the player's weapon slot containing the weapon logic component.
        /// </summary>
        public GameObject mWeaponLogicPrefab;

        /// <summary>
        /// Optional projectile prefab fired by ranged weapons (e.g. FireWand).
        /// </summary>
        public GameObject mProjectilePrefab;

        /// <summary>
        /// Category identifier for the weapon type (e.g. "Sword", "FireWand").
        /// </summary>
        public string Type;

        /// <summary>
        /// Maximum upgrade level achievable for this weapon before it is excluded from level-up choices.
        /// </summary>
        public int MaxLevel = 10;
    }
}