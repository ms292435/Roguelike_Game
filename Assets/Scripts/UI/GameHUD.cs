using Roguelike.DOTS;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    /// <summary>
    /// In-game HUD of a regular run: time left before the end of the run, and player health.
    /// Built at runtime on the scene's Canvas, so it needs no setup in the scene. Hidden in Benchmark mode.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        private TextMeshProUGUI mTimerText;
        private TextMeshProUGUI mHealthText;
        private GameObject mBossBar;
        private RectTransform mBossBarFill;
        private Image mFlash;

        private const float FlashStartAlpha = 0.75f;
        private const float FlashFadeSpeed = 2.5f;   // Alpha lost per second

        private static GameHUD sInstance;

        /// <summary>
        /// Flashes the whole screen white, then fades it out (bomb pickup).
        /// </summary>
        public static void FlashScreen()
        {
            if (sInstance == null || sInstance.mFlash == null) return;

            Color lColor = sInstance.mFlash.color;
            lColor.a = FlashStartAlpha;
            sInstance.mFlash.color = lColor;
        }

        /// <summary>
        /// Creates the HUD on the scene's root Canvas if the scene does not have one yet.
        /// Prefers a screen-space overlay Canvas, and falls back on any root Canvas.
        /// </summary>
        public static void EnsureExists()
        {
            if (FindAnyObjectByType<GameHUD>() != null) return;

            Canvas lCanvas = null;
            foreach (var lCandidate in FindObjectsByType<Canvas>())
            {
                if (!lCandidate.isRootCanvas) continue;

                if (lCanvas == null || lCandidate.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    lCanvas = lCandidate;
                }
            }

            if (lCanvas == null)
            {
                Debug.LogWarning("GameHUD: no Canvas found in the scene.");
                return;
            }

            var lHudObject = new GameObject("[GameHUD]", typeof(RectTransform));
            var lRect = (RectTransform)lHudObject.transform;
            lRect.SetParent(lCanvas.transform, false);
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            // First sibling: the menus and the level-up panel are drawn on top of the HUD
            lRect.SetAsFirstSibling();

            lHudObject.AddComponent<GameHUD>();
        }

        private void Awake()
        {
            mTimerText = CreateText("Timer", new Vector2(0.5f, 1f), new Vector2(0f, -70f), 56, TextAlignmentOptions.Top);
            mHealthText = CreateText("Health", new Vector2(0f, 1f), new Vector2(30f, -70f), 40, TextAlignmentOptions.TopLeft);
            CreateBossBar();
            CreateFlash();
            sInstance = this;
        }

        /// <summary>
        /// Creates the full-screen white image used by FlashScreen, transparent at rest.
        /// </summary>
        private void CreateFlash()
        {
            var lFlashObject = new GameObject("Flash", typeof(RectTransform), typeof(Image));
            var lRect = (RectTransform)lFlashObject.transform;
            lRect.SetParent(transform, false);
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero;
            lRect.offsetMax = Vector2.zero;

            mFlash = lFlashObject.GetComponent<Image>();
            mFlash.color = new Color(1f, 1f, 1f, 0f);
            mFlash.raycastTarget = false;
        }

        private void OnDestroy()
        {
            if (sInstance == this) sInstance = null;
        }

        /// <summary>
        /// Creates the boss health bar below the timer: a dark background and a red fill whose width follows the
        /// boss health.
        /// </summary>
        private void CreateBossBar()
        {
            mBossBar = new GameObject("BossBar", typeof(RectTransform), typeof(Image));
            var lBarRect = (RectTransform)mBossBar.transform;
            lBarRect.SetParent(transform, false);
            lBarRect.anchorMin = lBarRect.anchorMax = lBarRect.pivot = new Vector2(0.5f, 1f);
            lBarRect.anchoredPosition = new Vector2(0f, -150f);
            lBarRect.sizeDelta = new Vector2(800f, 28f);
            var lBackground = mBossBar.GetComponent<Image>();
            lBackground.color = new Color(0f, 0f, 0f, 0.6f);
            lBackground.raycastTarget = false;

            var lFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            mBossBarFill = (RectTransform)lFill.transform;
            mBossBarFill.SetParent(lBarRect, false);
            mBossBarFill.anchorMin = Vector2.zero;
            mBossBarFill.anchorMax = Vector2.one;
            mBossBarFill.offsetMin = new Vector2(3f, 3f);
            mBossBarFill.offsetMax = new Vector2(-3f, -3f);
            var lFillImage = lFill.GetComponent<Image>();
            lFillImage.color = new Color(0.85f, 0.18f, 0.15f, 1f);
            lFillImage.raycastTarget = false;

            mBossBar.SetActive(false);
        }

        private void Update()
        {
            // Unscaled time: the flash fades out even if the game gets paused right after the bomb
            if (mFlash.color.a > 0f)
            {
                Color lFlashColor = mFlash.color;
                lFlashColor.a = Mathf.Max(0f, lFlashColor.a - FlashFadeSpeed * Time.unscaledDeltaTime);
                mFlash.color = lFlashColor;
            }

            bool lVisible = WaveDirector.IsRunning && !GameManager.IsBenchmarkMode;
            mTimerText.enabled = lVisible;
            mHealthText.enabled = lVisible;
            if (!lVisible)
            {
                mBossBar.SetActive(false);
                return;
            }

            if (WaveDirector.IsBossPhase)
            {
                mTimerText.text = "BOSS";
            }
            else
            {
                int lSeconds = Mathf.CeilToInt(WaveDirector.RemainingTime);
                mTimerText.text = $"{lSeconds / 60:00}:{lSeconds % 60:00}";
            }

            bool lShowBossBar = BossStatus.IsAlive && BossStatus.MaxHealth > 0f;
            mBossBar.SetActive(lShowBossBar);
            if (lShowBossBar)
            {
                // The fill keeps its left edge and shrinks from the right
                mBossBarFill.anchorMax = new Vector2(Mathf.Clamp01(BossStatus.Health / BossStatus.MaxHealth), 1f);
            }

            if (Player.Instance != null)
            {
                mHealthText.text = $"HP {Mathf.CeilToInt(Mathf.Max(0f, Player.Instance.Health))} / {Mathf.CeilToInt(Player.Instance.MaxHealth)}";
            }
        }

        /// <summary>
        /// Creates a text anchored to a point of the screen.
        /// </summary>
        /// <param name="pName">Name of the text GameObject.</param>
        /// <param name="pAnchor">Anchor and pivot of the text, in normalized screen coordinates.</param>
        /// <param name="pOffset">Offset from the anchor, in canvas units.</param>
        /// <param name="pFontSize">Font size.</param>
        /// <param name="pAlignment">Text alignment.</param>
        /// <returns>The created text component.</returns>
        private TextMeshProUGUI CreateText(string pName, Vector2 pAnchor, Vector2 pOffset, float pFontSize,
            TextAlignmentOptions pAlignment)
        {
            var lTextObject = new GameObject(pName, typeof(RectTransform));
            var lRect = (RectTransform)lTextObject.transform;
            lRect.SetParent(transform, false);
            lRect.anchorMin = pAnchor;
            lRect.anchorMax = pAnchor;
            lRect.pivot = pAnchor;
            lRect.anchoredPosition = pOffset;
            lRect.sizeDelta = new Vector2(600f, 80f);

            var lText = lTextObject.AddComponent<TextMeshProUGUI>();
            lText.fontSize = pFontSize;
            lText.alignment = pAlignment;
            lText.color = Color.white;
            lText.raycastTarget = false;
            return lText;
        }
    }
}
