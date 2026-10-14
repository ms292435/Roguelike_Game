using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Marker component that tags an experience orb as collected by the player.
    /// Used to signal the collection system to process the orb and award experience.
    /// </summary>
    public struct ExperienceCollectedEvent : IComponentData { }

    /// <summary>
    /// System that handles experience orb movement toward the player.
    /// Implements attraction zone detection and smooth movement toward the player.
    /// Executes during the simulation phase each frame.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ExperienceMovementSystem : ISystem
    {
        /// <summary>
        /// Depth of the experience orbs. Orbs and enemies are opaque and sorted by the depth buffer (the camera is
        /// at z = 0): orbs sit behind every enemy (z between 0.05 and 0.51), in front of the Aura (0.6) and the ground (1).
        /// </summary>
        public const float OrbDepth = 0.55f;
        /// <summary>
        /// Speed of the orbs pulled by a magnet pickup, faster than the regular attraction.
        /// </summary>
        private const float MagnetAttractionSpeed = 25f;

        private static bool sAttractAllRequested;

        /// <summary>
        /// Makes every experience orb fly to the player during the next update (magnet pickup).
        /// </summary>
        public static void RequestAttractAll()
        {
            sAttractAllRequested = true;
        }

        /// <summary>
        /// Initializes the system and sets up requirements.
        /// </summary>
        public void OnCreate(ref SystemState pState)
        {
            // Require at least one experience orb to be present
            pState.RequireForUpdate<ExperienceData>();
        }

        /// <summary>
        /// Main update loop that retrieves player data and schedules movement jobs.
        /// Cannot use [BurstCompile] due to managed Player.Instance access.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Exit early if player hasn't been instantiated
            if (Player.Instance == null) return;

            // Get player's current world position
            float3 lPlayerPos = Player.Instance.transform.position;
            
            // Get attraction radius (zone where orbs start moving toward player)
            float lAttractRadius = Player.Instance.mAttractRadius;
            
            // Get collection radius (zone where orbs are automatically collected)
            float lCollectRadius = Player.Instance.mCollectRadius;
            
            // Get frame delta time for frame-rate independent movement
            float lDeltaTime = SystemAPI.Time.DeltaTime;

            // Consume the magnet request: every orb is attracted this update
            bool lAttractAll = sAttractAllRequested;
            sAttractAllRequested = false;

            // Schedule the movement job for all experience orbs
            ScheduleJobs(ref pState, lPlayerPos, lAttractRadius, lCollectRadius, lDeltaTime, lAttractAll);
        }

        /// <summary>
        /// Schedules the parallel experience movement job.
        /// Collected orbs are marked through the BeginSimulation command buffer: it is played back at the start of the
        /// next frame, so no structural change happens at the end of the simulation, where it would force every
        /// running job (including the enemy movement job) to complete.
        /// </summary>
        /// <param name="pPlayerPos">Current player world position.</param>
        /// <param name="pAttractRadius">Radius at which orbs start attracting toward player.</param>
        /// <param name="pCollectRadius">Radius at which orbs are collected by player.</param>
        /// <param name="pDeltaTime">Frame delta time in seconds.</param>
        /// <param name="pAttractAll">True when a magnet pickup attracts every orb.</param>
        [BurstCompile]
        private void ScheduleJobs(ref SystemState pState,
            float3 pPlayerPos, float pAttractRadius, float pCollectRadius, float pDeltaTime, bool pAttractAll)
        {
            // Create an entity command buffer for marking collected orbs, played back at the start of the next frame
            var lEcbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var lEcb = lEcbSingleton.CreateCommandBuffer(pState.WorldUnmanaged).AsParallelWriter();

            // Schedule the movement job with all necessary parameters
            pState.Dependency = new ExperienceMoveJob
            {
                mPlayerPosition = pPlayerPos,
                // Pre-square radii to avoid expensive square root in job
                mSqrAttractRadius = pAttractRadius * pAttractRadius,
                mSqrCollectRadius = pCollectRadius * pCollectRadius,
                mDeltaTime = pDeltaTime,
                mECB = lEcb,
                mAttractAll = pAttractAll,
                mMagnetSpeed = MagnetAttractionSpeed,
            }.ScheduleParallel(pState.Dependency);
        }

        /// <summary>
        /// Burst-compiled job that processes movement for each experience orb.
        /// Handles attraction activation, movement, and collection detection.
        /// </summary>
        [BurstCompile]
        public partial struct ExperienceMoveJob : IJobEntity
        {
            public float3 mPlayerPosition;
            public float mSqrAttractRadius;
            public float mSqrCollectRadius;
            public float mDeltaTime;
            public EntityCommandBuffer.ParallelWriter mECB;

            /// <summary>
            /// True when a magnet pickup attracts every orb, at mMagnetSpeed at least.
            /// </summary>
            public bool mAttractAll;
            public float mMagnetSpeed;

            /// <summary>
            /// Executes movement logic for a single experience orb entity.
            /// Handles attraction activation, movement toward player, and collection marking.
            /// </summary>
            /// <param name="pSortKey">Entity index for command buffer ordering.</param>
            /// <param name="pEntity">The experience orb entity being updated.</param>
            /// <param name="pTransform">Orb transform component containing position (modified).</param>
            /// <param name="pXp">Experience data component with attraction state (modified).</param>
            private void Execute(
                [EntityIndexInQuery] int pSortKey,
                Entity pEntity,
                ref LocalTransform pTransform,
                ref ExperienceData pXp)
            {
                // Get orb's current position and normalize to 2D space (z = 0)
                float3 lPos = pTransform.Position;
                lPos.z = 0f;
                
                // Get player position and normalize to 2D space
                float3 lPlayerPos = mPlayerPosition;
                lPlayerPos.z = 0f;

                // Calculate squared distance to reduce expensive square root operations
                float lSqrDist = math.lengthsq(lPlayerPos - lPos);

                // --- Attraction Activation Check ---
                // Activate attraction when orb enters attraction zone
                if (!pXp.mIsAttracted && lSqrDist < mSqrAttractRadius)
                    pXp.mIsAttracted = true;

                // Magnet pickup: every orb flies to the player, wherever it is
                if (mAttractAll)
                {
                    pXp.mIsAttracted = true;
                    pXp.mAttractionSpeed = math.max(pXp.mAttractionSpeed, mMagnetSpeed);
                }

                // --- Movement Logic ---
                // Apply movement if attraction is active
                if (pXp.mIsAttracted)
                {
                    // Calculate direction toward player
                    float3 lDir = math.normalizesafe(lPlayerPos - lPos);
                    
                    // Apply frame-rate independent movement toward player
                    lPos += lDir * pXp.mAttractionSpeed * mDeltaTime;
                    
                    // Ensure position remains in 2D space
                    lPos.z = 0f;
                    
                    // Update transform with new position, at the orb depth (keeps the orb scale: elite orbs are bigger)
                    pTransform.Position = new float3(lPos.x, lPos.y, OrbDepth);
                    
                    // Recalculate squared distance after movement
                    lSqrDist = math.lengthsq(lPlayerPos - lPos);
                }

                // --- Collection Detection Check ---
                // Mark orb as collected when it enters collection zone
                if (lSqrDist < mSqrCollectRadius)
                    mECB.AddComponent<ExperienceCollectedEvent>(pSortKey, pEntity);
            }
        }
    }

    /// <summary>
    /// System that processes collected experience orbs and awards experience to player.
    /// Runs at the start of the simulation, with the other structural changes and before any job is scheduled,
    /// so destroying the collected orbs never has to wait for a running job.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EnemySpawnerSystem))]
    public partial struct ExperienceCollectionSystem : ISystem
    {
        private EntityQuery mCollectedQuery;

        /// <summary>
        /// Initializes the system and sets up requirements.
        /// </summary>
        public void OnCreate(ref SystemState pState)
        {
            // Require at least one collected orb to be present
            pState.RequireForUpdate<ExperienceCollectedEvent>();

            mCollectedQuery = SystemAPI.QueryBuilder().WithAll<ExperienceData, ExperienceCollectedEvent>().Build();
        }

        /// <summary>
        /// Main update loop that collects all marked experience orbs.
        /// Accumulates experience values and destroys collected orbs.
        /// Awards total experience to player in a single batch.
        /// </summary>
        public void OnUpdate(ref SystemState pState)
        {
            // Exit early if player hasn't been instantiated
            if (Player.Instance == null) return;

            // Accumulator for total experience from all collected orbs
            float lTotalXP = 0f;

            // --- Collection Processing ---
            // Query all orbs marked with ExperienceCollectedEvent
            foreach (var lXp in
                SystemAPI.Query<RefRO<ExperienceData>>()
                    .WithAll<ExperienceCollectedEvent>())
            {
                // Accumulate experience value from collected orb
                lTotalXP += lXp.ValueRO.mValue;
            }

            // Destroy all collected orbs in one structural change
            pState.EntityManager.DestroyEntity(mCollectedQuery);

            // --- Experience Award ---
            // Award accumulated experience to player if any was collected
            if (lTotalXP > 0f)
                Player.Instance.AddExperience(lTotalXP);
        }
    }
}