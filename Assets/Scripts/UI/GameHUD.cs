using TMPro;
using UnityEngine;

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
        }

        private void Update()
        {
            bool lVisible = WaveDirector.IsRunning && !GameManager.IsBenchmarkMode;
            mTimerText.enabled = lVisible;
            mHealthText.enabled = lVisible;
            if (!lVisible) return;

            int lSeconds = Mathf.CeilToInt(WaveDirector.RemainingTime);
            mTimerText.text = $"{lSeconds / 60:00}:{lSeconds % 60:00}";

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
