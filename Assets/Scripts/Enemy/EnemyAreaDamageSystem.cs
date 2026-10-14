using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Roguelike.DOTS
{
    /// <summary>
    /// Damage dealt to every enemy within a ring around a point. A disc is a ring with an inner radius of zero.
    /// </summary>
    public struct AreaDamageRequest
    {
        public float2 mCenter;

        /// <summary>
        /// Enemies at this distance or closer are not hit. Lets an expanding wave hit each enemy only once.
        /// </summary>
        public float mInnerRadius;
        public float mOuterRadius;
        public float mDamage;

        /// <summary>
        /// Initial speed of the knockback pushing hit enemies away from the center.
        /// </summary>
        public float mKnockback;

        /// <summary>
        /// When above zero, hit enemies are pushed hard enough to end up around this distance from the center
        /// (a shockwave carrying the enemies to its edge). The knockback speed is the minimum push.
        /// </summary>
        public float mPushToRadius;

        /// <summary>
        /// Identifier of the wave this request belongs to, so that a wave hits each enemy only once.
        /// Zero for requests that may hit the same enemy repeatedly (e.g. the Aura ticks).
        /// </summary>
        public int mWaveId;
    }

    /// <summary>
    /// Applies the area damage queued by the weapons (Aura, Nova) through the EnemyBridge, in a parallel Burst job.
    /// Area weapons can hit thousands of enemies at once: processing them in a job over all enemies costs far less
    /// than reading and writing each hit enemy from the main thread.
    /// Hit enemies lose health, flash red and are pushed back; enemies whose health drops to zero are flagged dead.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemySpawnerSystem))]
    [UpdateBefore(typeof(EnemyMovementSystem))]
    public partial struct EnemyAreaDamageSystem : ISystem
    {
        /// <summary>
        /// Requests queued on the main thread since the last update. Only accessed on the main thread:
        /// the job works on a copy.
        /// </summary>
        public NativeList<AreaDamageRequest> mPendingRequests;

        [BurstCompile]
        public void OnCreate(ref SystemState pState)
        {
            mPendingRequests = new NativeList<AreaDamageRequest>(16, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState pState)
        {
            if (mPendingRequests.IsCreated) mPendingRequests.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState pState)
        {
            if (mPendingRequests.Length == 0) return;

            var lRequests = mPendingRequests.ToArray(Allocator.TempJob);
            mPendingRequests.Clear();

            pState.Dependency = new AreaDamageJob
            {
                mRequests = lRequests,
            }.ScheduleParallel(pState.Dependency);

            lRequests.Dispose(pState.Dependency);
        }

        /// <summary>
        /// Burst-compiled job testing every living enemy against the queued damage areas.
        /// </summary>
        [BurstCompile]
        [WithPresent(typeof(EnemyDeadTag))]
        public partial struct AreaDamageJob : IJobEntity
        {
            [ReadOnly] public NativeArray<AreaDamageRequest> mRequests;

            private void Execute(in LocalTransform pTransform, ref EnemyHealthComponent pHealth,
                ref EnemyHitFlash pHitFlash, ref EnemyKnockback pKnockback, ref EnemyWaveHit pWaveHit,
                EnabledRefRW<EnemyDeadTag> pDead)
            {
                if (pDead.ValueRO) return;

                float2 lPosition = pTransform.Position.xy;

                // Areas hit the edge of the enemy, not only its center (matters for the boss)
                float lEnemyRadius = 0.5f * pTransform.Scale;

                for (int i = 0; i < mRequests.Length; i++)
                {
                    AreaDamageRequest lRequest = mRequests[i];
                    float2 lFromCenter = lPosition - lRequest.mCenter;
                    float lSqrDistance = math.lengthsq(lFromCenter);
                    float lDistance = math.sqrt(lSqrDistance);

                    // The enemy overlaps the ring: its near edge is inside the outer radius, its far edge outside the
                    // inner radius
                    bool lInside = lDistance - lEnemyRadius <= lRequest.mOuterRadius
                                   && (lRequest.mInnerRadius <= 0f || lDistance + lEnemyRadius > lRequest.mInnerRadius);
                    if (!lInside) continue;

                    // A wave hits each enemy only once
                    if (lRequest.mWaveId != 0)
                    {
                        if (pWaveHit.mLastWaveId == lRequest.mWaveId) continue;
                        pWaveHit.mLastWaveId = lRequest.mWaveId;
                    }

                    pHealth.mValue -= lRequest.mDamage;
                    if (pHealth.mValue <= 0f)
                    {
                        pDead.ValueRW = true;
                        return;
                    }

                    // Same feedback as a direct hit: red flash and knockback, weaker on bigger enemies
                    pHitFlash.mValue = 1f;

                    float lPushSpeed = lRequest.mKnockback;
                    if (lRequest.mPushToRadius > 0f)
                    {
                        // Speed needed to travel the remaining distance to the target radius
                        float lRemainingDistance = lRequest.mPushToRadius - lDistance;
                        lPushSpeed = math.max(lPushSpeed, lRemainingDistance * EnemyKnockback.DampingRate);
                    }

                    float2 lPush = math.normalizesafe(lFromCenter) * (lPushSpeed / math.max(1f, pTransform.Scale));
                    pKnockback.mVelocity = EnemyKnockback.Strongest(pKnockback.mVelocity, new float3(lPush, 0f));
                }
            }
        }
    }
}
