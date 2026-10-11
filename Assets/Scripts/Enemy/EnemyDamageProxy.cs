using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Legacy proxy wrapper for backward compatibility.
    /// Forwards damage requests directly to the centralized EnemyBridge facade.
    /// </summary>
    public static class EnemyDamageProxy
    {
        /// <summary>
        /// Deals damage to all enemies within a radius around a given position via EnemyBridge.
        /// </summary>
        /// <param name="pPosition">Center of the impact area</param>
        /// <param name="pRadius">Impact radius</param>
        /// <param name="pDamage">Damage dealt</param>
        public static void DealDamage(Vector3 pPosition, float pRadius, float pDamage)
        {
            EnemyBridge.DealDamage(pPosition, pRadius, pDamage);
        }
    }
}