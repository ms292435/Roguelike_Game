using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that counts total and visible enemies within the camera viewport.
    /// Uses native references and asynchronously reads the previous frame's job results
    /// to avoid main thread pipeline stalls.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyCounterSystem : ISystem
    {
        private NativeReference<int> mTotalCount;
        private NativeReference<int> mVisibleCount;
        private bool mJobScheduled;

        /// <summary>
        /// Total number of active enemy entities currently alive in the world.
        /// Exposed statically for UI reading.
        /// </summary>
        public static int TotalEnemies;

        /// <summary>
        /// Number of enemy entities located inside the camera's visible orthographic viewport.
        /// Exposed statically for UI reading.
        /// </summary>
        public static int VisibleEnemies;

        public void OnCreate(ref SystemState pState)
        {
            pState.RequireForUpdate<EnemySpeedComponent>();
            mTotalCount = new NativeReference<int>(Allocator.Persistent);
            mVisibleCount = new NativeReference<int>(Allocator.Persistent);
            TotalEnemies = 0;
            VisibleEnemies = 0;
        }

        public void OnDestroy(ref SystemState pState)
        {
            if (mTotalCount.IsCreated) mTotalCount.Dispose();
            if (mVisibleCount.IsCreated) mVisibleCount.Dispose();
        }

        public void OnUpdate(ref SystemState pState)
        {
            if (Player.Instance == null || Camera.main == null) return;

            // Read the results of the job scheduled on the previous frame (already completed without sync stalls)
            if (mJobScheduled)
            {
                TotalEnemies = mTotalCount.Value;
                VisibleEnemies = mVisibleCount.Value;
            }

            // Reset accumulators for this frame's job
            mTotalCount.Value = 0;
            mVisibleCount.Value = 0;

            float3 lPlayerPos = Player.Instance.transform.position;
            Camera lCam = Camera.main;
            float lHalfHeight = lCam.orthographicSize;
            float lHalfWidth = lHalfHeight * lCam.aspect;

            pState.Dependency = new CountEnemiesJob
            {
                mPlayerPosition = lPlayerPos,
                mHalfHeight = lHalfHeight,
                mHalfWidth = lHalfWidth,
                mTotalCount = mTotalCount,
                mVisibleCount = mVisibleCount,
            }.Schedule(pState.Dependency);

            mJobScheduled = true;
        }

        /// <summary>
        /// Burst-compiled job that evaluates enemy visibility and counts totals.
        /// </summary>
        [BurstCompile]
        public partial struct CountEnemiesJob : IJobEntity
        {
            public float3 mPlayerPosition;
            public float mHalfWidth;
            public float mHalfHeight;

            public NativeReference<int> mTotalCount;
            public NativeReference<int> mVisibleCount;

            private void Execute(in LocalTransform pTransform, in EnemySpeedComponent pSpeed)
            {
                mTotalCount.Value++;

                float3 lPos = pTransform.Position;
                bool lIsVisible =
                    lPos.x > mPlayerPosition.x - mHalfWidth &&
                    lPos.x < mPlayerPosition.x + mHalfWidth &&
                    lPos.y > mPlayerPosition.y - mHalfHeight &&
                    lPos.y < mPlayerPosition.y + mHalfHeight;

                if (lIsVisible)
                {
                    mVisibleCount.Value++;
                }
            }
        }
    }
}
