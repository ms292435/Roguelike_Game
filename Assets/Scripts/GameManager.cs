using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelike
{
    /// <summary>
    /// Singleton manager orchestrating game states, UI panels (Main Menu, Game Over / Victory), and scene reloads.
    /// Starts a session either in Game mode (5-minute survival run driven by the WaveDirector) or in Benchmark mode
    /// (horde size controlled by the BenchmarkController, uncapped frame rate for measurements).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject mMainMenuPanel;
        [SerializeField] private GameObject mGameOverPanel;

        /// <summary>
        /// Title of the end panel, switched between "Game Over" and the victory message.
        /// Found by name in the end panel when not assigned.
        /// </summary>
        [SerializeField] private TextMeshProUGUI mResultTitleText;

        public static GameManager Instance { get; private set; }

        /// <summary>
        /// True when the current session is a benchmark, false for a regular game.
        /// Kept across scene reloads so that "Restart" replays the same mode.
        /// </summary>
        public static bool IsBenchmarkMode { get; private set; }

        /// <summary>
        /// Number of enemies killed by the player's weapons during the current run.
        /// </summary>
        public int Score { get; set; } = 0;

        public TextMeshProUGUI mScoreText;

        private static bool mSkipMainMenu = false;
        private const float BenchmarkCameraSize = 30f;

        /// <summary>
        /// Displays the end panel with the "Game Over" title, and pauses the game.
        /// </summary>
        public void DisplayGameOver()
        {
            DisplayResult("GAME OVER");
        }

        /// <summary>
        /// Displays the end panel with the victory title, and pauses the game.
        /// </summary>
        public void DisplayVictory()
        {
            DisplayResult("YOU SURVIVED!");
        }

        /// <summary>
        /// Starts a regular game: a 5-minute survival run with normal player stats.
        /// </summary>
        public void StartGame()
        {
            StartSession(false);
        }

        /// <summary>
        /// Starts a benchmark session: horde size controlled from the benchmark HUD, uncapped frame rate.
        /// </summary>
        public void StartBenchmark()
        {
            StartSession(true);
        }

        /// <summary>
        /// Restarts the active scene and bypasses the main menu directly into the same mode.
        /// </summary>
        public static void ReplayGame()
        {
            mSkipMainMenu = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// Hides the menus, applies the player stats and frame rate settings of the mode, and starts the simulation.
        /// </summary>
        /// <param name="pBenchmark">True for a benchmark session, false for a regular game.</param>
        private void StartSession(bool pBenchmark)
        {
            IsBenchmarkMode = pBenchmark;
            Score = 0;

            if (mMainMenuPanel != null) mMainMenuPanel.SetActive(false);
            if (mGameOverPanel != null) mGameOverPanel.SetActive(false);

            // Benchmarks need an uncapped frame rate; a regular game is synced to the monitor
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = pBenchmark ? 0 : 1;

            if (Player.Instance != null) Player.Instance.ApplyModeStats(pBenchmark);

            if (pBenchmark)
            {
                // Benchmarks use the widest view, to see the whole horde
                WaveDirector.StopRun();
                CameraZoom.SetSize(BenchmarkCameraSize);
            }
            else
            {
                WaveDirector.BeginRun();
            }

            Time.timeScale = 1f;
        }

        /// <summary>
        /// Shows the end panel with a title and the final score, and pauses the game.
        /// </summary>
        /// <param name="pTitle">Title displayed at the top of the end panel.</param>
        private void DisplayResult(string pTitle)
        {
            WaveDirector.StopRun();

            if (mGameOverPanel != null) mGameOverPanel.SetActive(true);
            if (mResultTitleText != null) mResultTitleText.text = pTitle;

            if (mScoreText != null)
                mScoreText.text = $"Score: {Score}";

            // Stop time to freeze the game when the end panel is displayed
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Shows the main menu UI and pauses game simulation.
        /// </summary>
        private void ShowMainMenu()
        {
            if (mMainMenuPanel != null) mMainMenuPanel.SetActive(true);
            if (mGameOverPanel != null) mGameOverPanel.SetActive(false);

            Time.timeScale = 0f;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (mResultTitleText == null && mGameOverPanel != null)
            {
                foreach (var lText in mGameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (lText.gameObject.name == "GameOver")
                    {
                        mResultTitleText = lText;
                        break;
                    }
                }
            }

            if (!mSkipMainMenu)
            {
                ShowMainMenu();
            }
        }

        private void Start()
        {
            // Replays start in Start rather than Awake, so that the Player has initialized before its stats are applied
            if (mSkipMainMenu)
            {
                mSkipMainMenu = false;
                StartSession(IsBenchmarkMode);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
