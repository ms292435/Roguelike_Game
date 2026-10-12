using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Drives the horde during a regular game: a 5-minute survival run whose difficulty grows over time, ending with
    /// a final boss whose death wins the game.
    /// Two curves, indexed by the elapsed time in seconds, define the maximum horde size and the spawn rate.
    /// The EnemySpawnerSystem reads the target and consumes the spawn budget accumulated here.
    /// The camera starts close to the player and zooms out progressively as the horde grows.
    /// Place it on a GameObject of the scene to tune its curves in the Inspector and save them with the scene.
    /// If the scene has none, a default one is created at the start of the run.
    /// </summary>
    public class WaveDirector : MonoBehaviour
    {
        [Tooltip("Debug: spawns an elite, a runner pack and the boss as soon as the run starts. Disable before release.")]
        public bool mDebugSpawnEverythingAtStart = false;

        [Tooltip("Duration of a run in seconds. Surviving until the end wins the game.")]
        public float mRunDuration = 300f;

        [Tooltip("Maximum number of enemies alive, by elapsed time in seconds.")]
        public AnimationCurve mTargetEnemies = new AnimationCurve(
            new Keyframe(0f, 8f),
            new Keyframe(30f, 60f),
            new Keyframe(60f, 150f),
            new Keyframe(90f, 500f),
            new Keyframe(150f, 2000f),
            new Keyframe(210f, 5000f),
            new Keyframe(270f, 12000f),
            new Keyframe(300f, 20000f));

        [Tooltip("Number of enemies spawned per second, by elapsed time in seconds.")]
        public AnimationCurve mSpawnRate = new AnimationCurve(
            new Keyframe(0f, 1.5f),
            new Keyframe(20f, 2.5f),
            new Keyframe(60f, 10f),
            new Keyframe(150f, 80f),
            new Keyframe(240f, 240f),
            new Keyframe(300f, 600f));

        [Tooltip("Multiplier applied to the health of newly spawned enemies, by elapsed time in seconds.")]
        public AnimationCurve mEnemyHealthMultiplier = new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(60f, 1.2f),
            new Keyframe(150f, 2.5f),
            new Keyframe(240f, 4f),
            new Keyframe(300f, 5f));

        [Tooltip("Seconds between two elite spawns, by elapsed time in seconds.")]
        public AnimationCurve mEliteInterval = new AnimationCurve(
            new Keyframe(0f, 25f),
            new Keyframe(150f, 12f),
            new Keyframe(300f, 5f));

        [Tooltip("Time in seconds before the first runner pack.")]
        public float mRunnerStartTime = 30f;

        [Tooltip("Seconds between two runner packs, by elapsed time in seconds.")]
        public AnimationCurve mRunnerPackInterval = new AnimationCurve(
            new Keyframe(30f, 18f),
            new Keyframe(150f, 10f),
            new Keyframe(300f, 5f));

        [Tooltip("Number of runners in a pack, by elapsed time in seconds.")]
        public AnimationCurve mRunnerPackSize = new AnimationCurve(
            new Keyframe(30f, 4f),
            new Keyframe(150f, 8f),
            new Keyframe(300f, 14f));

        [Tooltip("Camera orthographic size (how far the player sees), by horde size: the bigger the horde, the wider the view.")]
        public AnimationCurve mCameraSizeByHorde = new AnimationCurve(
            new Keyframe(8f, 6f),
            new Keyframe(150f, 8f),
            new Keyframe(600f, 11f),
            new Keyframe(2000f, 15f),
            new Keyframe(5000f, 20f),
            new Keyframe(12000f, 26f),
            new Keyframe(20000f, 30f));

        [Tooltip("How fast the camera follows the horde size: higher is faster.")]
        public float mCameraZoomSharpness = 0.5f;

        private static WaveDirector sInstance;

        private bool mRunning;
        private int mTargetEnemyCount;
        private float mSpawnBudget;
        private float mEliteBudget;
        private float mRunnerBudget;
        private bool mBossPhase;
        private bool mBossSpawnPending;

        /// <summary>
        /// True when the spawner has a boss prefab. Set by the EnemySpawnerSystem.
        /// </summary>
        public static bool HasBossPrefab { get; set; }

        /// <summary>
        /// True once the run time is over and the final boss has been released.
        /// </summary>
        public static bool IsBossPhase => IsRunning && sInstance.mBossPhase;

        /// <summary>
        /// Time elapsed since the start of the run, in seconds. Paused with the game.
        /// </summary>
        public static float ElapsedTime { get; private set; }

        /// <summary>
        /// Time left before the end of the run, in seconds.
        /// </summary>
        public static float RemainingTime => sInstance != null ? Mathf.Max(0f, sInstance.mRunDuration - ElapsedTime) : 0f;

        /// <summary>
        /// True while a regular game is in progress.
        /// </summary>
        public static bool IsRunning => sInstance != null && sInstance.mRunning;

        /// <summary>
        /// Maximum number of enemies alive at this point of the run. Zero when no run is in progress.
        /// </summary>
        public static int TargetEnemyCount => IsRunning ? sInstance.mTargetEnemyCount : 0;

        /// <summary>
        /// Multiplier applied to the health of the enemies spawned at this point of the run.
        /// </summary>
        public static float EnemyHealthMultiplier =>
            IsRunning ? sInstance.mEnemyHealthMultiplier.Evaluate(ElapsedTime) : 1f;

        /// <summary>
        /// Starts a new run from the beginning.
        /// </summary>
        public static void BeginRun()
        {
            EnsureInstance();
            ElapsedTime = 0f;
            sInstance.mSpawnBudget = 0f;
            sInstance.mEliteBudget = 0f;
            sInstance.mRunnerBudget = 0f;
            sInstance.mBossPhase = false;
            sInstance.mBossSpawnPending = false;

            if (sInstance.mDebugSpawnEverythingAtStart)
            {
                Debug.LogWarning("WaveDirector: debug spawn enabled, the elite, runners and boss appear immediately.");
                sInstance.mEliteBudget = 1f;
                sInstance.mRunnerBudget = 1f;
                // The spawner only reports its boss prefab once the run has started: request the boss regardless
                sInstance.mBossPhase = true;
                sInstance.mBossSpawnPending = true;
            }
            sInstance.mTargetEnemyCount = Mathf.RoundToInt(sInstance.mTargetEnemies.Evaluate(0f));
            sInstance.mRunning = true;

            CameraZoom.SetSize(sInstance.mCameraSizeByHorde.Evaluate(sInstance.mTargetEnemyCount));
            GameHUD.EnsureExists();
        }

        /// <summary>
        /// Stops the current run. The horde target drops to zero, so the spawner clears the remaining enemies.
        /// </summary>
        public static void StopRun()
        {
            if (sInstance != null) sInstance.mRunning = false;
        }

        /// <summary>
        /// Takes up to pMaxCount enemies from the spawn budget accumulated since the last spawn.
        /// </summary>
        /// <param name="pMaxCount">Maximum number of enemies the spawner can add this frame.</param>
        /// <returns>The number of enemies to spawn this frame.</returns>
        public static int ConsumeSpawnBudget(int pMaxCount)
        {
            if (!IsRunning || pMaxCount <= 0) return 0;

            int lCount = Mathf.Min((int)sInstance.mSpawnBudget, pMaxCount);
            sInstance.mSpawnBudget -= lCount;
            return lCount;
        }

        /// <summary>
        /// Takes up to pMaxCount elites from the elite budget accumulated since the last elite spawn.
        /// </summary>
        /// <param name="pMaxCount">Maximum number of enemies the spawner can add this frame.</param>
        /// <returns>The number of elites to spawn this frame.</returns>
        public static int ConsumeEliteBudget(int pMaxCount)
        {
            if (!IsRunning || pMaxCount <= 0) return 0;

            int lCount = Mathf.Min((int)sInstance.mEliteBudget, pMaxCount);
            sInstance.mEliteBudget -= lCount;
            return lCount;
        }

        /// <summary>
        /// Takes one runner pack from the budget accumulated since the last pack, if one is due.
        /// </summary>
        /// <param name="pMaxCount">Maximum number of enemies the spawner can add this frame.</param>
        /// <returns>The number of runners in the pack to spawn this frame, or zero.</returns>
        public static int ConsumeRunnerPack(int pMaxCount)
        {
            if (!IsRunning || pMaxCount <= 0 || sInstance.mRunnerBudget < 1f) return 0;

            sInstance.mRunnerBudget -= 1f;
            int lPackSize = Mathf.RoundToInt(sInstance.mRunnerPackSize.Evaluate(ElapsedTime));
            return Mathf.Min(lPackSize, pMaxCount);
        }

        /// <summary>
        /// Returns true once, when the final boss must be spawned.
        /// </summary>
        /// <returns>True if the spawner must spawn the boss this frame.</returns>
        public static bool ConsumeBossSpawn()
        {
            if (!IsRunning || !sInstance.mBossSpawnPending) return false;

            sInstance.mBossSpawnPending = false;
            return true;
        }

        private static void EnsureInstance()
        {
            if (sInstance != null) return;

            // Prefer the WaveDirector placed in the scene, which holds the tuned curves
            sInstance = FindAnyObjectByType<WaveDirector>();
            if (sInstance != null) return;

            Debug.LogWarning("WaveDirector: none found in the scene, using the default values.");
            var lHostObject = new GameObject("[WaveDirector]");
            sInstance = lHostObject.AddComponent<WaveDirector>();
        }

        private void Awake()
        {
            if (sInstance != null && sInstance != this)
            {
                Debug.LogWarning("WaveDirector: several instances in the scene, only the first one is used.");
                return;
            }
            sInstance = this;
        }

        private void Update()
        {
            if (!mRunning) return;

            // Scaled delta time: the run is paused with the game (main menu, level up)
            float lDeltaTime = Time.deltaTime;
            ElapsedTime += lDeltaTime;

            mTargetEnemyCount = Mathf.RoundToInt(mTargetEnemies.Evaluate(ElapsedTime));
            CameraZoom.MoveTowards(mCameraSizeByHorde.Evaluate(mTargetEnemyCount), mCameraZoomSharpness, lDeltaTime);

            // Accumulate the spawn budget, capped to one second of spawning so that a long pause cannot burst
            float lRate = mSpawnRate.Evaluate(ElapsedTime);
            mSpawnBudget = Mathf.Min(mSpawnBudget + lRate * lDeltaTime, Mathf.Max(1f, lRate));
            mEliteBudget = Mathf.Min(mEliteBudget + lDeltaTime / Mathf.Max(0.1f, mEliteInterval.Evaluate(ElapsedTime)), 2f);

            if (ElapsedTime >= mRunnerStartTime)
            {
                float lPackInterval = Mathf.Max(0.1f, mRunnerPackInterval.Evaluate(ElapsedTime));
                mRunnerBudget = Mathf.Min(mRunnerBudget + lDeltaTime / lPackInterval, 2f);
            }

            // End of the run: release the final boss, whose death wins the game.
            // Without a boss, surviving the run is enough
            if (ElapsedTime >= mRunDuration && !mBossPhase)
            {
                if (HasBossPrefab)
                {
                    mBossPhase = true;
                    mBossSpawnPending = true;
                }
                else if (GameManager.Instance != null)
                {
                    GameManager.Instance.DisplayVictory();
                }
            }
        }

        private void OnDestroy()
        {
            if (sInstance == this)
            {
                sInstance = null;
            }
        }
    }
}
