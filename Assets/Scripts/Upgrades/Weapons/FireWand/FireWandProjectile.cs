using UnityEngine;

namespace Roguelike
{
    public class FireWandProjectile : Projectile
    {
        protected override void OnHit(Enemy pEnemy, float pDamage)
        {
            pEnemy.TakeDamage(pDamage);
        }
    }
}