using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Authoring component that allows designers to configure enemy properties in the Unity Editor.
    /// Gets baked into ECS components at runtime for high-performance data-oriented processing.
    /// </summary>
    public class EnemyAuthoring : MonoBehaviour
    {
        /// <summary>
        /// The movement speed of the enemy in units per second.
        /// Configured in the editor and transferred to EnemySpeedComponent during baking.
        /// </summary>
        public float mMoveSpeed = 5f;
        
        /// <summary>
        /// Damage value dealt to the player when the enemy makes contact.
        /// Configured in the editor and transferred to EnemyDamageComponent during baking.
        /// </summary>
        public int mDamage = 5;

        /// <summary>
        /// Health of the enemy at the start of a run. Scaled up over the run by the WaveDirector when spawned.
        /// </summary>
        public float mMaxHealth = 60f;

        /// <summary>
        /// Experience given by the orb dropped when a weapon kills this enemy.
        /// </summary>
        public float mExperience = 1f;
        
        /// <summary>
        /// Baker class that converts MonoBehaviour data to ECS components.
        /// Executes during scene loading/baking to transform GameObject properties into pure data.
        /// </summary>
        public class EnemyBaker : Baker<EnemyAuthoring>
        {
            /// <summary>
            /// Baking method that converts the EnemyAuthoring MonoBehaviour into ECS components.
            /// Called automatically by the baking system to prepare entity data for high-performance processing.
            /// This separation allows gameplay logic to operate on pure data without MonoBehaviour overhead.
            /// </summary>
            /// <param name="pAuthoring">The EnemyAuthoring MonoBehaviour instance being baked.</param>
            public override void Bake(EnemyAuthoring pAuthoring)
            {
                // Retrieve or create the entity that represents this GameObject in the ECS world
                // Uses Dynamic transform usage flags to allow runtime position/rotation updates
                var lEntity = GetEntity(TransformUsageFlags.Dynamic);

                // Add the speed component containing movement properties
                // mSmoothedSeparation is initialized to zero and updated by the movement system
                AddComponent(lEntity, new EnemySpeedComponent
                {
                    mValue = pAuthoring.mMoveSpeed,              // Transfer editor-configured speed value
                    mSmoothedSeparation = float3.zero            // Initialize separation vector
                });

                // Add the damage component for collision handling
                // The movement system reads this value when an enemy contacts the player
                AddComponent(lEntity, new EnemyDamageComponent
                {
                    mValue = pAuthoring.mDamage                  // Transfer editor-configured damage value
                });

                AddComponent(lEntity, new EnemyHealthComponent
                {
                    mValue = pAuthoring.mMaxHealth
                });

                AddComponent(lEntity, new EnemyRewardComponent
                {
                    mExperience = pAuthoring.mExperience
                });

                // Hit feedback: red flash and knockback, both at rest
                AddComponent(lEntity, new EnemyHitFlash { mValue = 0f });
                AddComponent(lEntity, new EnemyKnockback { mVelocity = float3.zero });
                AddComponent(lEntity, new EnemyWaveHit { mLastWaveId = 0 });

                // Add the death and contact markers, disabled until the enemy dies or touches the player
                AddComponent<EnemyDeadTag>(lEntity);
                SetComponentEnabled<EnemyDeadTag>(lEntity, false);
                AddComponent<EnemyContactTag>(lEntity);
                SetComponentEnabled<EnemyContactTag>(lEntity, false);
            }
        }
    }
    
    /// <summary>
    /// Pure data component that stores enemy movement properties.
    /// Used by the EnemyMovementSystem and EnemyHashSystem for efficient spatial calculations.
    /// Contains only numeric data for optimal cache performance in DOTS processing.
    /// </summary>
    public struct EnemySpeedComponent : IComponentData
    {
        /// <summary>
        /// Base movement speed of the enemy in units per second.
        /// Determines how fast the enemy moves toward the player.
        /// </summary>
        public float mValue;
        
        /// <summary>
        /// Smoothed separation force accumulated from nearby enemies.
        /// Updated each frame by the movement system to avoid neighbors.
        /// Uses exponential smoothing to create natural movement curves.
        /// </summary>
        public float3 mSmoothedSeparation;
    }

    /// <summary>
    /// Pure data component that stores enemy damage properties.
    /// Read by the movement system when detecting collision with the player.
    /// Contains only the numeric value needed for damage calculation.
    /// </summary>
    public struct EnemyDamageComponent : IComponentData
    {
        /// <summary>
        /// Damage value dealt to the player upon contact.
        /// Applied when the enemy collides with the player entity.
        /// </summary>
        public int mValue;
    }

    /// <summary>
    /// Remaining health of an enemy. Written on the main thread by the EnemyBridge when a weapon hits the enemy.
    /// </summary>
    public struct EnemyHealthComponent : IComponentData
    {
        public float mValue;
    }

    /// <summary>
    /// Experience given by the orb dropped when a weapon kills the enemy.
    /// </summary>
    public struct EnemyRewardComponent : IComponentData
    {
        public float mExperience;
    }

    /// <summary>
    /// Intensity of the red flash shown when the enemy takes damage, from 0 (none) to 1 (fully red).
    /// Bound to the per-instance _HitFlash property of the enemy shader graphs: Entities Graphics uploads it to the GPU.
    /// Set to 1 by the EnemyBridge on hit, then faded out by the movement job.
    /// </summary>
    [MaterialProperty("_HitFlash")]
    public struct EnemyHitFlash : IComponentData
    {
        public float mValue;
    }

    /// <summary>
    /// Velocity pushing the enemy away from the last hit, added to its movement and damped over time.
    /// </summary>
    public struct EnemyKnockback : IComponentData
    {
        /// <summary>
        /// Exponential damping rate of the knockback velocity, per second. An initial speed v moves the enemy
        /// by v / DampingRate in total.
        /// </summary>
        public const float DampingRate = 10f;

        public float3 mVelocity;

        /// <summary>
        /// Returns the knockback to keep when an enemy is hit again: the strongest push wins, so that a weak push
        /// (e.g. the Aura) never cancels a strong one (e.g. the Nova) hitting during the same frame.
        /// </summary>
        /// <param name="pCurrent">Current knockback velocity.</param>
        /// <param name="pNew">Knockback velocity of the new hit.</param>
        /// <returns>The velocity with the highest speed.</returns>
        public static float3 Strongest(float3 pCurrent, float3 pNew)
        {
            return math.lengthsq(pNew) > math.lengthsq(pCurrent) ? pNew : pCurrent;
        }
    }

    /// <summary>
    /// Identifier of the last expanding wave (Nova) that hit the enemy, so that a wave hits each enemy only once,
    /// even when its push carries the enemy ahead of the wave front.
    /// </summary>
    public struct EnemyWaveHit : IComponentData
    {
        public int mLastWaveId;
    }

    /// <summary>
    /// Enableable marker set when an enemy dies (hit by a weapon or touching the player).
    /// Enabling a bit is not a structural change, so it can be written from parallel jobs and from the
    /// main thread without a command buffer. Dead enemies are destroyed in a single batch by the
    /// EnemyDeathSystem at the start of the next simulation, and are skipped by every other code path until then.
    /// </summary>
    public struct EnemyDeadTag : IComponentData, IEnableableComponent
    {
    }

    /// <summary>
    /// Enableable marker set alongside EnemyDeadTag when the enemy died by touching the player,
    /// so the EnemyDeathSystem knows which deaths must damage the player.
    /// </summary>
    public struct EnemyContactTag : IComponentData, IEnableableComponent
    {
    }
}
