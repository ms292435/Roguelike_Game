using Unity.Entities;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Authoring component that allows designers to configure experience orb properties in the Unity Editor.
    /// Gets baked into ECS components at runtime for high-performance experience collection logic.
    /// Stores base properties for attraction speed and experience value.
    /// </summary>
        public class ExperienceAuthoring : MonoBehaviour
    {
        public float mValue = 1f;
        public float mAttractionSpeed = 8f;

        /// <summary>
        /// Baker class that converts ExperienceAuthoring into ECS components.
        /// Transfers editor-configured properties to pure data components.
        /// </summary>
        public class Baker : Baker<ExperienceAuthoring>
        {
            /// <summary>
            /// Baking method that converts the experience orb configuration into ECS components.
            /// Creates the experience data component and disables it initially.
            /// </summary>
            /// <param name="pAuthoring">The ExperienceAuthoring MonoBehaviour being baked.</param>
            public override void Bake(ExperienceAuthoring pAuthoring)
            {
                // Retrieve or create the entity representing this experience orb
                // Uses Renderable flag since experience orbs need to be visually displayed
                var lEntity = GetEntity(TransformUsageFlags.Renderable);

                // Add the experience data component with editor-configured properties
                AddComponent(lEntity, new ExperienceData
                {
                    // Transfer experience value from editor configuration
                    mValue = pAuthoring.mValue,
                    
                    // Transfer attraction speed from editor configuration
                    mAttractionSpeed = pAuthoring.mAttractionSpeed,
                    
                    // Initialize attraction state (orbs don't attract initially)
                    mIsAttracted = false,
                });

                // Disable the component initially
                // The spawner will enable it when the orb is actually spawned into the world
                // This optimization reduces unnecessary processing for prefab instances
                SetComponentEnabled<ExperienceData>(lEntity, false);
            }
        }
    }

    /// <summary>
    /// Pure data component that stores experience orb properties and state.
    /// Used by movement and collection systems for experience handling.
    /// Implements IEnableableComponent to allow runtime enable/disable optimization.
    /// </summary>
    public struct ExperienceData : IComponentData, IEnableableComponent
    {
        public float mValue;
        public float mAttractionSpeed;
        public bool mIsAttracted;
    }
}