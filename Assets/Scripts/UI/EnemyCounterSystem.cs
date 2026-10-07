using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Roguelike.DOTS
{
    /// <summary>
    /// System that calculates the total number of alive enemies and visible enemies within the camera viewport.
    /// Uses archetype chunk metadata for instant, zero-latency total counts without race conditions,
    /// and a Burst-compiled background job for viewport culling visibility checks.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyMovementSystem))]
    public partial struct EnemyCounterSystem : ISystem
    {
        private NativeReference<int> mVisibleCount;
        private EntityQuery mEnemyQuery;
        private JobHandle mJobHandle;
        private bool mJobScheduled;

        /// <summary>
        /// Total number of active enemy entities currently alive in the world.
        /// Updated synchronously on the main thread from archetype chunk metadata.
        /// </summary>
        public static int TotalEnemies;

        /// <summary>
        /// Number of enemy entities located inside the camera's visible orthographic viewport.
        /// Updated from the completion of the viewport culling job.
        /// </summary>
        public static int VisibleEnemies;

        public void OnCreate(ref SystemState pState)
        {
            mEnemyQuery = SystemAPI.QueryBuilder().WithAll<EnemySpeedComponent>().Build();
            mVisibleCount = new NativeReference<int>(Allocator.Persistent);
            TotalEnemies = 0;
            VisibleEnemies = 0;
            mJobScheduled = false;
        }

        public void OnDestroy(ref SystemState pState)
        {
            if (mJobScheduled)
            {
                mJobHandle.Complete();
            }

            if (mVisibleCount.IsCreated)
            {
                mVisibleCount.Dispose();
            }
        }

        public void OnUpdate(ref SystemState pState)
        {
            // Complete previous frame's count job safely before reading
            if (mJobScheduled)
            {
                mJobHandle.Complete();
                VisibleEnemies = mVisibleCount.Value;
                mJobScheduled = false;
            }

            // Read the exact total enemy count instantly from entity metadata (O(chunks), zero latency)
            TotalEnemies = mEnemyQuery.CalculateEntityCount();

            // Exit early if there are no enemies alive or player/camera are absent
            if (TotalEnemies == 0 || Player.Instance == null || Camera.main == null)
            {
                VisibleEnemies = 0;
                return;
            }

            // Reset accumulator for this frame's visibility job
            mVisibleCount.Value = 0;

            float3 lPlayerPos = Player.Instance.transform.position;
            Camera lCam = Camera.main;
            float lHalfHeight = lCam.orthographicSize;
            float lHalfWidth = lHalfHeight * lCam.aspect;

            mJobHandle = new CountVisibleEnemiesJob
            {
                mPlayerPosition = lPlayerPos,
                mHalfHeight = lHalfHeight,
                mHalfWidth = lHalfWidth,
                mVisibleCount = mVisibleCount,
            }.Schedule(pState.Dependency);

            pState.Dependency = mJobHandle;
            mJobScheduled = true;
        }

        /// <summary>
        /// Burst-compiled job that evaluates enemy positions against the camera viewport.
        /// </summary>
        [BurstCompile]
        public partial struct CountVisibleEnemiesJob : IJobEntity
        {
            public float3 mPlayerPosition;
            public float mHalfWidth;
            public float mHalfHeight;

            public NativeReference<int> mVisibleCount;

            private void Execute(in LocalTransform pTransform, in EnemySpeedComponent _)
            {
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
