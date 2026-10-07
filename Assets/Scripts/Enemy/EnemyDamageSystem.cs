using Unity.Collections;
using Unity.Entities;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Component tag that marks entities as able to deal damage to the player.
    /// Contains the damage value to be applied.
    /// </summary>
    public struct DamagePlayerTag : IComponentData
    {
        public int mValue;
    }

    /// <summary>
    /// System responsible for applying accumulated damage to the player.
    /// Runs during the late simulation phase to process all damage in a single frame.
    /// </summary>
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct DamageApplicationSystem : ISystem
    {
        /// <summary>
        /// Updates damage application each frame.
        /// Collects all damage values from entities tagged with DamagePlayerTag,
        /// applies the total damage to the player, and destroys the damaging entities.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Early exit if player instance is not available
            if (Player.Instance == null) return;

            // Initialize total damage accumulator
            int lTotalDamage = 0;
            // Create a command buffer to queue entity destruction operations
            var lEcb = new EntityCommandBuffer(Allocator.Temp);

            // Query all entities with DamagePlayerTag component
            foreach (var (lTag, lEntity) in
                SystemAPI.Query<DamagePlayerTag>().WithEntityAccess())
            {
                // Accumulate damage value from the current entity
                lTotalDamage += lTag.mValue;
                // Queue entity for destruction after damage is processed
                lEcb.DestroyEntity(lEntity);
            }

            // Execute all queued commands (entity destruction)
            lEcb.Playback(pState.EntityManager); 
            // Clean up the command buffer resources
            lEcb.Dispose();                      

            // Apply accumulated damage to the player if any damage was dealt
            if (lTotalDamage > 0)
                Player.Instance.Health -= lTotalDamage;
        }
    }
}