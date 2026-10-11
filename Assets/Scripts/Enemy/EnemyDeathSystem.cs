using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that removes every enemy flagged with EnemyDeadTag, in one batch, at the start of the simulation.
    /// Enemies are flagged by the movement job (contact with the player) and by the EnemyBridge (weapon hits).
    /// For each dead enemy, it drops an experience orb at its position, and applies the damage of the enemies that
    /// touched the player.
    /// Gathering all structural changes here, before any enemy job is scheduled, means that no job has to be
    /// waited for during the frame: the movement job of the previous frame is completed here, after it had
    /// the whole rendering phase to finish on the worker threads.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ExperienceCollectionSystem))]
    [UpdateBefore(typeof(EnemySpawnerSystem))]
    public partial struct EnemyDeathSystem : ISystem
    {
        private EntityQuery mDeadEnemyQuery;

        public void OnCreate(ref SystemState pState)
        {
            // Only matches enemies whose EnemyDeadTag is enabled
            mDeadEnemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyDeadTag, LocalTransform, EnemyDamageComponent>()
                .WithPresent<EnemyContactTag>()
                .Build();
        }

        /// <summary>
        /// Collects the dead enemies, destroys them in a single batch, spawns their experience orbs and damages the player.
        /// Cannot use [BurstCompile] due to managed Player.Instance access.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Reading the enabled bits completes the previous frame's movement job, the only job writing them
            if (mDeadEnemyQuery.IsEmpty) return;

            var lDeathPositions = new NativeList<float3>(Allocator.Temp);
            int lContactDamage = 0;

            foreach (var (lTransform, lDamage, lContact) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyDamageComponent>, EnabledRefRO<EnemyContactTag>>()
                    .WithAll<EnemyDeadTag>()
                    .WithPresent<EnemyContactTag>())
            {
                lDeathPositions.Add(lTransform.ValueRO.Position);

                if (lContact.ValueRO)
                {
                    lContactDamage += lDamage.ValueRO.mValue;
                }
            }

            // Destroy all dead enemies in one structural change
            pState.EntityManager.DestroyEntity(mDeadEnemyQuery);

            SpawnExperienceOrbs(ref pState, lDeathPositions.AsArray());
            lDeathPositions.Dispose();

            if (lContactDamage > 0 && Player.Instance != null)
            {
                Player.Instance.Health -= lContactDamage;
            }
        }

        /// <summary>
        /// Instantiates one experience orb per death position, in a single batch.
        /// </summary>
        /// <param name="pPositions">World positions where the enemies died.</param>
        private void SpawnExperienceOrbs(ref SystemState pState, NativeArray<float3> pPositions)
        {
            if (!SystemAPI.TryGetSingleton(out ExperienceSpawnerData lSpawnerData)) return;
            if (lSpawnerData.mPrefab == Entity.Null) return;

            var lEntityManager = pState.EntityManager;
            var lOrbs = lEntityManager.Instantiate(lSpawnerData.mPrefab, pPositions.Length, Allocator.Temp);

            for (int i = 0; i < lOrbs.Length; i++)
            {
                lEntityManager.SetComponentData(lOrbs[i], LocalTransform.FromPosition(pPositions[i]));
                lEntityManager.SetComponentData(lOrbs[i], new ExperienceData
                {
                    mValue = 1f,              // Standard experience value per orb
                    mAttractionSpeed = 8f,    // Movement speed when attracted to player
                    mIsAttracted = false,     // Orb is not initially attracted to player
                });
                lEntityManager.SetComponentEnabled<ExperienceData>(lOrbs[i], true);
            }

            lOrbs.Dispose();
        }
    }
}
