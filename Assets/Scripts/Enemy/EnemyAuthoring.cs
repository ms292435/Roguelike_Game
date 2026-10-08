using Unity.Entities;
using Unity.Mathematics;
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

                // Add the death marker, disabled until the enemy is queued for destruction
                AddComponent<EnemyDeadTag>(lEntity);
                SetComponentEnabled<EnemyDeadTag>(lEntity, false);
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
    /// Enableable marker set when an enemy has been queued for destruction in an entity command buffer.
    /// Prevents the same enemy from being destroyed twice (and dropping two experience orbs)
    /// before the command buffer is played back.
    /// </summary>
    public struct EnemyDeadTag : IComponentData, IEnableableComponent
    {
    }
}
