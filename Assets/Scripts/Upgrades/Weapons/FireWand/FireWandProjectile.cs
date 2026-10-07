using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Specialized projectile spawned by the FireWand weapon.
    /// Inherits base linear movement, range despawn, and ECS hit detection from Projectile.
    /// </summary>
    public class FireWandProjectile : Projectile
    {
        /// <summary>
        /// Hook called upon collision with an enemy.
        /// Currently relies on base destruction and proxy damage.
        /// </summary>
        protected override void OnHit()
        {
            base.OnHit();
        }
    }
}