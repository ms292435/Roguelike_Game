using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// ScriptableObject defining passive stat bonuses (Health, Damage, Speed) applied upon level up.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStatBoost", menuName = "Roguelike/Upgrades/Stat Boost")]
    public class StatBoostData : UpgradeData
    {
        /// <summary>
        /// Multiplier applied to the target stat (e.g. 1.25 for +25%).
        /// </summary>
        public float multiplier = 1f;

        /// <summary>
        /// Stat category targeted by this boost ("Health", "Damage", or "Speed").
        /// </summary>
        public string type;
    }
}