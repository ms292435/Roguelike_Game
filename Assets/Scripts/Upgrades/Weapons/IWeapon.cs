using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Common interface implemented by all equipped weapons (Sword, FireWand, etc.).
    /// Governs attack execution, update loops, and level progression proposals.
    /// </summary>
    public interface IWeapon
    {
        /// <summary>
        /// Source ScriptableObject configuration for this weapon.
        /// </summary>
        WeaponUpgradeData Data { get; }

        /// <summary>
        /// Audio clip triggered when the weapon executes an attack.
        /// </summary>
        AudioClip AttackSound { get; }

        /// <summary>
        /// Current upgrade level of the weapon.
        /// </summary>
        int WeaponLevel { get; set; }

        /// <summary>
        /// Executes the weapon's primary attack logic.
        /// </summary>
        void Attack();

        /// <summary>
        /// Updates cooldown timer and triggers an attack if ready.
        /// </summary>
        /// <param name="pDeltaTime">Frame delta time in seconds.</param>
        /// <returns>True if an attack was performed this frame, false otherwise.</returns>
        bool UpdateWeapon(float pDeltaTime);

        /// <summary>
        /// Initializes the weapon with configuration data.
        /// </summary>
        /// <param name="pData">The source weapon data.</param>
        void Initialize(WeaponUpgradeData pData);

        /// <summary>
        /// Generates the next upgrade proposal offered to the player when leveling up this weapon.
        /// </summary>
        /// <returns>A proposal containing an effect description and application callback.</returns>
        WeaponUpgradeProposal GetNextUpgradeProposal();
    }
}