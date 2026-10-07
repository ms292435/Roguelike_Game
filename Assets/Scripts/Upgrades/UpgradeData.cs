using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Base ScriptableObject representing an upgrade option offered to the player during level up.
    /// Derived by StatBoostData for passive buffs and WeaponUpgradeData for active weapons.
    /// </summary>
    public abstract class UpgradeData : ScriptableObject
    {
        /// <summary>
        /// Display name of the upgrade shown on the level-up card.
        /// </summary>
        public string Name;

        /// <summary>
        /// Visual icon displayed on the UI card and weapon HUD slot.
        /// </summary>
        public Sprite Icon;

        /// <summary>
        /// Textual description summarizing the effect of this upgrade.
        /// </summary>
        [TextArea] public string Description;
    }
}