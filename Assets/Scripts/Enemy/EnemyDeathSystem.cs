using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that removes every enemy flagged with EnemyDeadTag, in one batch, at the start of the simulation.
    /// Enemies are flagged by the movement job (contact with the player) and by the EnemyBridge (weapon hits).
    /// Enemies killed by a weapon drop an experience orb worth their reward and count in the score; enemies that died
    /// by touching the player damage the player instead, and drop nothing.
    /// Gathering all structural changes here, before any enemy job is scheduled, means that no job has to be
    /// waited for during the frame: the movement job of the previous frame is completed here, after it had
    /// the whole rendering phase to finish on the worker threads.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ExperienceCollectionSystem))]
    [UpdateBefore(typeof(EnemySpawnerSystem))]
    public partial struct EnemyDeathSystem : ISystem
    {
        private const float BigOrbScale = 1.8f;
        private EntityQuery mDeadEnemyQuery;
        private Random mRandom;
        private EntityQuery mDeadBossQuery;
        private EntityQuery mLivingContactQuery;

        public void OnCreate(ref SystemState pState)
        {
            mRandom = new Random(0x5EED1234u);

            // Only matches enemies whose EnemyDeadTag is enabled
            mDeadEnemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyDeadTag, LocalTransform, EnemyDamageComponent, EnemyRewardComponent>()
                .WithPresent<EnemyContactTag>()
                .Build();

            mDeadBossQuery = SystemAPI.QueryBuilder().WithAll<EnemyDeadTag, EnemyBossTag>().Build();

            // Enemies that touched the player without dying (boss)
            mLivingContactQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyContactTag, EnemyDamageComponent>()
                .WithDisabled<EnemyDeadTag>()
                .Build();
        }

        /// <summary>
        /// Collects the dead enemies, destroys them in a single batch, spawns their experience orbs and damages the player.
        /// Cannot use [BurstCompile] due to managed Player.Instance access.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            ApplyLivingContacts(ref pState);

            // Reading the enabled bits completes the previous frame's movement job, the only job writing them
            if (mDeadEnemyQuery.IsEmpty) return;

            // Checked before the dead enemies are destroyed
            bool lBossKilled = !mDeadBossQuery.IsEmpty;

            var lPickupPositions = new NativeList<float3>(Allocator.Temp);
            var lOrbPositions = new NativeList<float3>(Allocator.Temp);
            var lOrbValues = new NativeList<float>(Allocator.Temp);
            int lContactDamage = 0;
            int lKills = 0;

            foreach (var (lTransform, lDamage, lReward, lContact) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<EnemyDamageComponent>, RefRO<EnemyRewardComponent>,
                        EnabledRefRO<EnemyContactTag>>()
                    .WithAll<EnemyDeadTag>()
                    .WithPresent<EnemyContactTag>())
            {
                if (lContact.ValueRO)
                {
                    // Died by touching the player: damage, but no reward
                    lContactDamage += lDamage.ValueRO.mValue;
                }
                else
                {
                    // Killed by a weapon
                    lOrbPositions.Add(lTransform.ValueRO.Position);
                    lOrbValues.Add(lReward.ValueRO.mExperience);
                    lKills++;

                    // Rare pickup drop
                    if (mRandom.NextFloat() < lReward.ValueRO.mPickupDropChance)
                    {
                        lPickupPositions.Add(lTransform.ValueRO.Position);
                    }
                }
            }

            // Destroy all dead enemies in one structural change
            pState.EntityManager.DestroyEntity(mDeadEnemyQuery);

            SpawnExperienceOrbs(ref pState, lOrbPositions.AsArray(), lOrbValues.AsArray());
            lOrbPositions.Dispose();
            lOrbValues.Dispose();

            // Pickups are a game mode feature: none in benchmarks
            if (!GameManager.IsBenchmarkMode)
            {
                foreach (var lPosition in lPickupPositions)
                {
                    Pickup.SpawnRandom(lPosition);
                }
            }
            lPickupPositions.Dispose();

            if (lContactDamage > 0 && Player.Instance != null)
            {
                Player.Instance.TakeContactDamage(lContactDamage);
            }

            if (lKills > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.Score += lKills;
            }

            // The boss never dies by touching the player: its death is always a weapon kill
            if (lBossKilled && GameManager.Instance != null)
            {
                GameManager.Instance.DisplayVictory();
            }
        }

        /// <summary>
        /// Damages the player for the enemies that touched it without dying (boss), and resets their contact marker.
        /// </summary>
        private void ApplyLivingContacts(ref SystemState pState)
        {
            if (mLivingContactQuery.IsEmpty) return;

            var lEntityManager = pState.EntityManager;
            using var lEntities = mLivingContactQuery.ToEntityArray(Allocator.Temp);
            int lContactDamage = 0;

            foreach (var lEntity in lEntities)
            {
                lContactDamage += lEntityManager.GetComponentData<EnemyDamageComponent>(lEntity).mValue;
                lEntityManager.SetComponentEnabled<EnemyContactTag>(lEntity, false);
            }

            if (Player.Instance != null)
            {
                Player.Instance.TakeContactDamage(lContactDamage);
            }
        }

        /// <summary>
        /// Instantiates one experience orb per weapon kill, in a single batch.
        /// Orbs worth more than one point (dropped by elites) are drawn bigger.
        /// </summary>
        /// <param name="pPositions">World positions where the enemies died.</param>
        /// <param name="pValues">Experience value of each orb.</param>
        private void SpawnExperienceOrbs(ref SystemState pState, NativeArray<float3> pPositions, NativeArray<float> pValues)
        {
            if (pPositions.Length == 0) return;
            if (!SystemAPI.TryGetSingleton(out ExperienceSpawnerData lSpawnerData)) return;
            if (lSpawnerData.mPrefab == Entity.Null) return;

            var lEntityManager = pState.EntityManager;
            var lOrbs = lEntityManager.Instantiate(lSpawnerData.mPrefab, pPositions.Length, Allocator.Temp);

            for (int i = 0; i < lOrbs.Length; i++)
            {
                float lScale = pValues[i] > 1f ? BigOrbScale : 1f;
                // Orbs sit at their own depth, behind every enemy
                float3 lOrbPosition = new float3(pPositions[i].x, pPositions[i].y, ExperienceMovementSystem.OrbDepth);
                lEntityManager.SetComponentData(lOrbs[i],
                    LocalTransform.FromPositionRotationScale(lOrbPosition, quaternion.identity, lScale));
                lEntityManager.SetComponentData(lOrbs[i], new ExperienceData
                {
                    mValue = pValues[i],      // Experience reward of the killed enemy
                    mAttractionSpeed = 8f,    // Movement speed when attracted to player
                    mIsAttracted = false,     // Orb is not initially attracted to player
                });
                lEntityManager.SetComponentEnabled<ExperienceData>(lOrbs[i], true);
            }

            lOrbs.Dispose();
        }
    }
}
