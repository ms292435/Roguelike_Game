using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelike
{
    /// <summary>
    /// Singleton manager orchestrating game states, UI panels (Main Menu, Game Over), and scene reloads.
    /// Manages game timescale and frame-rate settings.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject mMainMenuPanel;
        [SerializeField] private GameObject mGameOverPanel;

        public static GameManager Instance { get; private set; }
        public int Score { get; set; } = 0;

        public TextMeshProUGUI mScoreText;

        private static bool mSkipMainMenu = false;

        /// <summary>
        /// Displays the game over panel, sets final score text, and pauses game simulation.
        /// </summary>
        public void DisplayGameOver()
        {
            if (mGameOverPanel != null) mGameOverPanel.SetActive(true);

            if (mScoreText != null)
                mScoreText.text = $"Score: {Score}";

            // Stop time to freeze the game when game over is displayed
            Time.timeScale = 0f;
        }

        /// <summary>
        /// Starts active gameplay, hides menus, disables VSync and frame cap for benchmark measurements.
        /// </summary>
        public void StartGame()
        {
            if (mMainMenuPanel != null) mMainMenuPanel.SetActive(false);
            if (mGameOverPanel != null) mGameOverPanel.SetActive(false);

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Restarts the active scene and bypasses the main menu directly into gameplay.
        /// </summary>
        public static void ReplayGame()
        {
            mSkipMainMenu = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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

            if (mSkipMainMenu)
            {
                StartGame();
                mSkipMainMenu = false;
            }
            else
            {
                ShowMainMenu();
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