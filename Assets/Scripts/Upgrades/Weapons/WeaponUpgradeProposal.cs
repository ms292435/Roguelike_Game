namespace Roguelike
{
    /// <summary>
    /// Represents a specific level-up upgrade offered for an existing weapon.
    /// Bundles a UI description with an action delegate that applies the modification.
    /// </summary>
    public class WeaponUpgradeProposal
    {
        /// <summary>
        /// Description of the stat or behavior change (e.g. "+20% Damage").
        /// </summary>
        public string Description;

        /// <summary>
        /// Action callback invoked to apply the stat changes to the weapon instance.
        /// </summary>
        public System.Action ApplyAction;
    }
}