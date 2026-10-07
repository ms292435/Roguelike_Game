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

            // Schedule the movement job for all experience orbs
            ScheduleJobs(ref pState, lPlayerPos, lAttractRadius, lCollectRadius, lDeltaTime);
        }

        /// <summary>
        /// Schedules the parallel experience movement job.
        /// Creates command buffer for marking collected orbs.
        /// </summary>
        /// <param name="pPlayerPos">Current player world position.</param>
        /// <param name="pAttractRadius">Radius at which orbs start attracting toward player.</param>
        /// <param name="pCollectRadius">Radius at which orbs are collected by player.</param>
        /// <param name="pDeltaTime">Frame delta time in seconds.</param>
        [BurstCompile]
        private void ScheduleJobs(ref SystemState pState,
            float3 pPlayerPos, float pAttractRadius, float pCollectRadius, float pDeltaTime)
        {
            // Create an entity command buffer for marking collected orbs
            var lEcbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
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
                    
                    // Update transform with new position
                    pTransform = LocalTransform.FromPosition(lPos);
                    
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
    /// Executes after the movement system to process all collected orbs in a single pass.
    /// Runs during the simulation phase.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExperienceMovementSystem))]
    public partial struct ExperienceCollectionSystem : ISystem
    {
        /// <summary>
        /// Initializes the system and sets up requirements.
        /// </summary>
        public void OnCreate(ref SystemState pState)
        {
            // Require at least one collected orb to be present
            pState.RequireForUpdate<ExperienceCollectedEvent>();
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

            // Create an entity command buffer for deferred entity destruction
            var lEcb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(pState.WorldUnmanaged);

            // Accumulator for total experience from all collected orbs
            float lTotalXP = 0f;

            // --- Collection Processing ---
            // Query all orbs marked with ExperienceCollectedEvent
            foreach (var (lXp, lEntity) in
                SystemAPI.Query<RefRO<ExperienceData>>()
                    .WithAll<ExperienceCollectedEvent>()
                    .WithEntityAccess())
            {
                // Accumulate experience value from collected orb
                lTotalXP += lXp.ValueRO.mValue;
                
                // Queue orb for destruction
                lEcb.DestroyEntity(lEntity); 
            }

            // --- Experience Award ---
            // Award accumulated experience to player if any was collected
            if (lTotalXP > 0f)
                Player.Instance.AddExperience(lTotalXP);
        }
    }
}