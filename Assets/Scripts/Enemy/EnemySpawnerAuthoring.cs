using Unity.Entities;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Authoring component that allows designers to configure enemy spawner properties in the Unity Editor.
    /// Gets baked into ECS components at runtime for high-performance enemy spawning logic.
    /// Stores difficulty progression settings and spawn parameters.
    /// </summary>
    public class EnemySpawnerAuthoring : MonoBehaviour
    {
        public GameObject mEnemyPrefab;
        public AnimationCurve mDifficultyCurve;
        
        /// <summary>
        /// Base spawn interval in seconds between enemy waves.
        /// Gets multiplied by the difficulty curve value to scale with game progression.
        /// Default is 2 seconds at difficulty 1.0.
        /// </summary>
        public float mBaseSpawnInterval = 2f;
        public int mBaseEnemiesPerWave = 1;

        /// <summary>
        /// Baker class that converts EnemySpawnerAuthoring into ECS components.
        /// Separates managed data (AnimationCurve) from pure data for optimal performance.
        /// </summary>
        public class Baker : Baker<EnemySpawnerAuthoring>
        {
            /// <summary>
            /// Baking method that converts the spawner configuration into ECS components.
            /// Creates both pure data components and managed components as needed.
            /// </summary>
            /// <param name="pAuthoring">The EnemySpawnerAuthoring MonoBehaviour being baked.</param>
            public override void Bake(EnemySpawnerAuthoring pAuthoring)
            {
                // Retrieve the entity representing this spawner in the ECS world
                // Uses None for transform usage since spawner position doesn't change
                var lEntity = GetEntity(TransformUsageFlags.None);

                // Add the pure data component containing spawn logic parameters
                AddComponent(lEntity, new EnemySpawnerData
                {
                    // Convert the enemy prefab GameObject to an entity reference
                    // Uses Dynamic flags to allow instantiated enemies to move
                    mPrefab = GetEntity(pAuthoring.mEnemyPrefab, TransformUsageFlags.Dynamic),
                    
                    // Transfer spawn interval settings from editor configuration
                    mBaseSpawnInterval = pAuthoring.mBaseSpawnInterval,
                    
                    // Transfer enemy count per wave setting
                    mBaseEnemiesPerWave = pAuthoring.mBaseEnemiesPerWave,
                    
                    // Initialize timer to zero (will be incremented by spawning system)
                    mTimer = 0f,
                    
                    // Initialize game timer to zero (tracks total elapsed time)
                    mGameTimer = 0f,
                    
                    // Initialize deterministic random generator with fixed seed
                    // Ensures reproducible enemy spawn positions across runs
                    mRandom = new Unity.Mathematics.Random(1234)
                });

                // Add managed component containing AnimationCurve (cannot be stored in pure data struct)
                // Managed components incur a small overhead but are necessary for reference types
                AddComponentObject(lEntity, new EnemySpawnerManaged
                {
                    mDifficultyCurve = pAuthoring.mDifficultyCurve
                });
            }
        }
    }

    /// <summary>
    /// Pure data component containing all spawner logic parameters.
    /// Stores only numeric and deterministic data for optimal DOTS performance.
    /// Updated each frame by the spawning system.
    /// </summary>
    public struct EnemySpawnerData : IComponentData
    {
        public Unity.Entities.Entity mPrefab;
        public float mBaseSpawnInterval;
        public int mBaseEnemiesPerWave;
        public float mTimer;
        public float mGameTimer;
        public Unity.Mathematics.Random mRandom;
    }

    /// <summary>
    /// Managed component containing reference-type data that cannot be stored in pure struct components.
    /// Holds the AnimationCurve used for difficulty scaling over time.
    /// Managed components should be used sparingly due to small performance overhead.
    /// </summary>
    public class EnemySpawnerManaged : IComponentData
    {
        /// <summary>
        /// Difficulty progression curve mapping game time to difficulty multiplier.
        /// Evaluated by the spawning system each frame to scale spawn rates and enemy counts.
        /// Typical range: 1.0 (start) to 2.0+ (late game).
        /// </summary>
        public AnimationCurve mDifficultyCurve;
    }
}