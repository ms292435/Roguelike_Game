using Unity.Entities;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Health of the final boss, read by the HUD to draw the boss health bar.
    /// </summary>
    public static class BossStatus
    {
        public static bool IsAlive;
        public static float Health;
        public static float MaxHealth;

        /// <summary>
        /// Radius of the boss (half its scale), used to widen the weapons' grid queries during the boss fight.
        /// </summary>
        public static float Radius;
    }

    /// <summary>
    /// Copies the health of the living boss into BossStatus every frame, for the HUD.
    /// Runs at the start of the simulation, before any job writing the enemies' health is scheduled.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnerSystem))]
    public partial struct EnemyBossTrackingSystem : ISystem
    {
        public void OnUpdate(ref SystemState pState)
        {
            BossStatus.IsAlive = false;

            foreach (var (lHealth, lBoss, lTransform) in
                SystemAPI.Query<RefRO<EnemyHealthComponent>, RefRO<EnemyBossTag>, RefRO<LocalTransform>>()
                    .WithDisabled<EnemyDeadTag>())
            {
                BossStatus.IsAlive = true;
                BossStatus.Health = lHealth.ValueRO.mValue;
                BossStatus.MaxHealth = lBoss.ValueRO.mMaxHealth;
                BossStatus.Radius = 0.5f * lTransform.ValueRO.Scale;
            }
        }
    }
}
