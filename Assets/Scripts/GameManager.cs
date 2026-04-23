using UnityEngine;
using UnityEngine.SceneManagement;

namespace Roguelike
{
    public class GameManager : MonoBehaviour
    {
        [Header("UI Reference")]
        [SerializeField] private GameObject mMainMenuPanel;
        [SerializeField] private GameObject mGameOverPanel;

        public static GameManager Instance { get; private set; }

        private static bool mSkipMainMenu = false;

        public void DisplayGameOver()
        {
            if (mGameOverPanel != null) mGameOverPanel.SetActive(true);
            // Stop time to freeze the game when game over is displayed
            Time.timeScale = 0f;
        }
        public void StartGame()
        {
            mMainMenuPanel.SetActive(false);
            if (mGameOverPanel != null) mGameOverPanel.SetActive(false);

            Time.timeScale = 1f;
        }

        public static void ReplayGame()
        {
            mSkipMainMenu = true;
            // Set back time scale to normal
            Time.timeScale = 1f;
            // Get the current active scene and reload it
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ShowMainMenu()
        {
            mMainMenuPanel.SetActive(true);
            if (mGameOverPanel != null) mGameOverPanel.SetActive(false);

            Time.timeScale = 0f;
        }

        void Awake()
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
        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

    }
}