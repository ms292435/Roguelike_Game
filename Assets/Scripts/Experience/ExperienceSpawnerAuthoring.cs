using Unity.Entities;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Authoring component that allows designers to configure the experience spawner in the Unity Editor.
    /// Gets baked into ECS components at runtime for high-performance experience orb instantiation.
    /// Stores a reference to the experience prefab to be spawned when enemies die.
    /// </summary>
    public class ExperienceSpawnerAuthoring : MonoBehaviour
    {
        public GameObject mExperiencePrefab;

        /// <summary>
        /// Baker class that converts ExperienceSpawnerAuthoring into ECS components.
        /// Transforms the GameObject reference into an entity reference for use in systems.
        /// </summary>
        public class Baker : Baker<ExperienceSpawnerAuthoring>
        {
            /// <summary>
            /// Baking method that converts the experience spawner configuration into ECS components.
            /// Prepares the prefab entity for use by the experience spawning system.
            /// </summary>
            /// <param name="pAuthoring">The ExperienceSpawnerAuthoring MonoBehaviour being baked.</param>
            public override void Bake(ExperienceSpawnerAuthoring pAuthoring)
            {
                // Retrieve or create the entity representing this spawner in the ECS world
                // Uses None for transform usage since the spawner itself doesn't move or render
                var lEntity = GetEntity(TransformUsageFlags.None);
                
                // Add the experience spawner data component containing the prefab reference
                AddComponent(lEntity, new ExperienceSpawnerData
                {
                    // Convert the experience prefab GameObject to an entity reference
                    // Uses Dynamic flags to allow instantiated experience orbs to move and be rendered
                    mPrefab = GetEntity(pAuthoring.mExperiencePrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }

    /// <summary>
    /// Pure data component that stores the experience spawner configuration.
    /// Contains only the entity reference needed for prefab instantiation.
    /// Used by systems that spawn experience orbs when enemies die.
    /// </summary>
    public struct ExperienceSpawnerData : IComponentData
    {
        public Entity mPrefab;
    }
}