using Unity.Entities;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Authoring component that allows designers to configure the enemy spawner in the Unity Editor.
    /// Gets baked into ECS components at runtime for high-performance enemy spawning logic.
    /// Spawn rates and horde sizes are driven by the WaveDirector (game) or the BenchmarkController (benchmark).
    /// </summary>
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject mEnemyPrefab;

        /// <summary>
        /// Optional tougher enemy spawned regularly during a regular game, at the rate set by the WaveDirector.
        /// </summary>
        public GameObject mElitePrefab;

        /// <summary>
        /// Optional fast and fragile enemy spawned in packs during a regular game, at the rate set by the WaveDirector.
        /// </summary>
        public GameObject mRunnerPrefab;

        /// <summary>
        /// Optional final boss, spawned once at the end of the run. Without it, surviving the run wins the game.
        /// </summary>
        public GameObject mBossPrefab;

        /// <summary>
        /// Baker class that converts EnemySpawnerAuthoring into ECS components.
        /// </summary>
        public class Baker : Baker<EnemySpawnerAuthoring>
        {
            /// <summary>
            /// Baking method that converts the spawner configuration into an EnemySpawnerData component.
            /// </summary>
            /// <param name="pAuthoring">The EnemySpawnerAuthoring MonoBehaviour being baked.</param>
            public override void Bake(EnemySpawnerAuthoring pAuthoring)
            {
                // Retrieve the entity representing this spawner in the ECS world
                // Uses None for transform usage since spawner position doesn't change
                var lEntity = GetEntity(TransformUsageFlags.None);

                AddComponent(lEntity, new EnemySpawnerData
                {
                    // Convert the enemy prefab GameObject to an entity reference
                    // Uses Dynamic flags to allow instantiated enemies to move
                    mPrefab = GetEntity(pAuthoring.mEnemyPrefab, TransformUsageFlags.Dynamic),

                    // Optional elite prefab
                    mElitePrefab = pAuthoring.mElitePrefab != null
                        ? GetEntity(pAuthoring.mElitePrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,

                    // Optional runner prefab
                    mRunnerPrefab = pAuthoring.mRunnerPrefab != null
                        ? GetEntity(pAuthoring.mRunnerPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,

                    // Optional boss prefab
                    mBossPrefab = pAuthoring.mBossPrefab != null
                        ? GetEntity(pAuthoring.mBossPrefab, TransformUsageFlags.Dynamic)
                        : Entity.Null,

                    // Initialize deterministic random generator with fixed seed
                    // Ensures reproducible enemy spawn positions across runs
                    mRandom = new Unity.Mathematics.Random(1234)
                });
            }
        }
    }

    /// <summary>
    /// Pure data component containing the spawner's prefabs and random generator.
    /// Updated each frame by the spawning system.
    /// </summary>
    public struct EnemySpawnerData : IComponentData
    {
        public Entity mPrefab;
        public Entity mElitePrefab;
        public Entity mRunnerPrefab;
        public Entity mBossPrefab;
        public Unity.Mathematics.Random mRandom;
    }
}