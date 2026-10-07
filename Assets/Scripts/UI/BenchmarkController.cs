using UnityEngine;
using UnityEngine.InputSystem;
using Roguelike.DOTS;

namespace Roguelike
{
    /// <summary>
    /// In-game benchmark controller allowing dynamic runtime adjustments of the enemy horde size up to 1,000,000.
    /// Provides both an on-screen interactive IMGUI overlay and keyboard hotkeys (1-7, +/-, C, F1).
    /// Eliminates the need to recompile code between benchmark measurement stages.
    /// </summary>
    public class BenchmarkController : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the BenchmarkController.
        /// </summary>
        public static BenchmarkController Instance { get; private set; }

        /// <summary>
        /// Current target enemy horde count actively maintained by the spawner.
        /// Defaults to 2,000 for standard testing.
        /// Maximum ceiling is 1,000,000 entities.
        /// </summary>
        public static int TargetEnemyCount { get; private set; } = 2000;

        private static bool mClearRequested = false;

        [Header("UI Display Settings")]
        [SerializeField] private bool mShowOverlay = true;

        private GUIStyle mBoxStyle;
        private GUIStyle mHeaderStyle;
        private GUIStyle mLabelStyle;
        private GUIStyle mButtonStyle;
        private bool mStylesInitialized = false;

        /// <summary>
        /// Automatically instantiates a persistent BenchmarkController at runtime if none exists in the scene.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var lHostObject = new GameObject("[BenchmarkController]");
                lHostObject.AddComponent<BenchmarkController>();
                DontDestroyOnLoad(lHostObject);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Sets a new target enemy horde count clamped between 0 and 1,000,000.
        /// </summary>
        /// <param name="pCount">New target count.</param>
        public static void SetTarget(int pCount)
        {
            TargetEnemyCount = Mathf.Clamp(pCount, 0, 1000000);
        }

        /// <summary>
        /// Adjusts the target enemy count by a relative delta value.
        /// </summary>
        /// <param name="pDelta">Amount to add or subtract.</param>
        public static void AdjustTarget(int pDelta)
        {
            SetTarget(TargetEnemyCount + pDelta);
        }

        /// <summary>
        /// Requests an immediate destruction of all active enemies and experience orbs.
        /// Also resets static counters immediately.
        /// </summary>
        public static void RequestClear()
        {
            mClearRequested = true;
            TargetEnemyCount = 0;
            EnemyCounterSystem.TotalEnemies = 0;
            EnemyCounterSystem.VisibleEnemies = 0;
        }

        /// <summary>
        /// Checks and consumes the pending clear request flag.
        /// Called by the EnemySpawnerSystem on the simulation thread.
        /// </summary>
        /// <returns>True if a clear was requested, false otherwise.</returns>
        public static bool ConsumeClearRequest()
        {
            if (mClearRequested)
            {
                mClearRequested = false;
                return true;
            }
            return false;
        }

        private void Update()
        {
            ProcessKeyboardInput();
        }

        /// <summary>
        /// Polls keyboard input using the New Input System (Keyboard.current).
        /// </summary>
        private void ProcessKeyboardInput()
        {
            var lKeyboard = Keyboard.current;
            if (lKeyboard == null) return;

            // F1: Toggle overlay visibility
            if (lKeyboard.f1Key.wasPressedThisFrame)
            {
                mShowOverlay = !mShowOverlay;
            }

            // Quick tier shortcuts: 1-7 (Main row and Numpad) up to 1 Million
            if (lKeyboard.digit1Key.wasPressedThisFrame || lKeyboard.numpad1Key.wasPressedThisFrame) SetTarget(1000);
            if (lKeyboard.digit2Key.wasPressedThisFrame || lKeyboard.numpad2Key.wasPressedThisFrame) SetTarget(5000);
            if (lKeyboard.digit3Key.wasPressedThisFrame || lKeyboard.numpad3Key.wasPressedThisFrame) SetTarget(10000);
            if (lKeyboard.digit4Key.wasPressedThisFrame || lKeyboard.numpad4Key.wasPressedThisFrame) SetTarget(50000);
            if (lKeyboard.digit5Key.wasPressedThisFrame || lKeyboard.numpad5Key.wasPressedThisFrame) SetTarget(100000);
            if (lKeyboard.digit6Key.wasPressedThisFrame || lKeyboard.numpad6Key.wasPressedThisFrame) SetTarget(500000);
            if (lKeyboard.digit7Key.wasPressedThisFrame || lKeyboard.numpad7Key.wasPressedThisFrame) SetTarget(1000000);

            // Increment / Decrement: + and - keys
            bool lShiftPressed = lKeyboard.leftShiftKey.isPressed || lKeyboard.rightShiftKey.isPressed;
            int lStep = lShiftPressed ? 50000 : 10000;

            if (lKeyboard.numpadPlusKey.wasPressedThisFrame || lKeyboard.equalsKey.wasPressedThisFrame)
            {
                AdjustTarget(lStep);
            }

            if (lKeyboard.numpadMinusKey.wasPressedThisFrame || lKeyboard.minusKey.wasPressedThisFrame)
            {
                AdjustTarget(-lStep);
            }

            // Clear / Reset: 'C' key or 'Delete'
            if (lKeyboard.cKey.wasPressedThisFrame || lKeyboard.deleteKey.wasPressedThisFrame)
            {
                RequestClear();
            }
        }

        private void InitializeStyles()
        {
            if (mStylesInitialized) return;

            mBoxStyle = new GUIStyle(GUI.skin.box);
            mBoxStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.12f, 0.88f));

            mHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            mHeaderStyle.normal.textColor = new Color(0.35f, 0.85f, 1f);

            mLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
            mLabelStyle.normal.textColor = Color.white;

            mButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };

            mStylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!mShowOverlay) return;

            InitializeStyles();

            int lPanelWidth = 380;
            int lPanelHeight = 150;
            int lMargin = 10;
            Rect lPanelRect = new Rect(Screen.width - lPanelWidth - lMargin, lMargin, lPanelWidth, lPanelHeight);

            GUILayout.BeginArea(lPanelRect, mBoxStyle);

            GUILayout.Label("── ROGUELIKE BENCHMARK SUITE (MAX: 1M) ──", mHeaderStyle);
            GUILayout.Space(4);

            int lTotal = EnemyCounterSystem.TotalEnemies;
            int lVisible = EnemyCounterSystem.VisibleEnemies;

            GUILayout.Label($"<b>Target Horde:</b> {TargetEnemyCount:N0}   |   <b>Active:</b> {lTotal:N0}   |   <b>Visible:</b> {lVisible:N0}", mLabelStyle);

            GUILayout.Space(6);

            // Row 1: Quick target presets up to 1,000,000
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1k", mButtonStyle)) SetTarget(1000);
            if (GUILayout.Button("5k", mButtonStyle)) SetTarget(5000);
            if (GUILayout.Button("10k", mButtonStyle)) SetTarget(10000);
            if (GUILayout.Button("50k", mButtonStyle)) SetTarget(50000);
            if (GUILayout.Button("100k", mButtonStyle)) SetTarget(100000);
            if (GUILayout.Button("500k", mButtonStyle)) SetTarget(500000);
            if (GUILayout.Button("1M", mButtonStyle)) SetTarget(1000000);
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Row 2: Adjustments & Clear
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-50k", mButtonStyle)) AdjustTarget(-50000);
            if (GUILayout.Button("-10k", mButtonStyle)) AdjustTarget(-10000);
            if (GUILayout.Button("+10k", mButtonStyle)) AdjustTarget(10000);
            if (GUILayout.Button("+50k", mButtonStyle)) AdjustTarget(50000);

            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            if (GUILayout.Button("Clear All", mButtonStyle)) RequestClear();
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label("<color=#888888>Keys 1-7: 1k to 1M | +/-: Step | C: Clear | F1: Hide HUD</color>", mLabelStyle);

            GUILayout.EndArea();
        }

        private Texture2D MakeTex(int pWidth, int pHeight, Color pCol)
        {
            Color[] lPix = new Color[pWidth * pHeight];
            for (int i = 0; i < lPix.Length; ++i)
            {
                lPix[i] = pCol;
            }
            Texture2D lResult = new Texture2D(pWidth, pHeight);
            lResult.SetPixels(lPix);
            lResult.Apply();
            return lResult;
        }
    }
}
