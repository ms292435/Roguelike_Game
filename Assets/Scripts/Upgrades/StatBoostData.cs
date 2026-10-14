using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// ScriptableObject defining passive stat bonuses (Health, Damage, Speed, Pickup range) applied upon level up.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatBoost", menuName = "Roguelike/Upgrades/Stat Boost")]
    public class StatBoostData : UpgradeData
    {
        /// <summary>
        /// Boost expressed as a multiplier of the base stat (e.g. 1.1 for +10% of the base value).
        /// Boosts add up: they are applied to the base stat, not to the already boosted value.
        /// </summary>
        public float multiplier = 1f;

        /// <summary>
        /// Stat category targeted by this boost ("Health", "Damage", "Speed" or "Pickup").
        /// </summary>
        public string type;

        /// <summary>
        /// Number of times this boost can be taken. The last one also grants a bigger mastery bonus.
        /// </summary>
        public int MaxLevel = 5;
    }
}