using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    /// <summary>
    /// UI component displaying the player's current experience progression fill bar and text.
    /// Implements IObserver to automatically refresh when the player raises an "Experience" event.
    /// </summary>
    public class ExperienceBar : MonoBehaviour, IObserver
    {
        public Image mXpFill;
        public TextMeshProUGUI mXpText;
        public static ExperienceBar Instance { get; private set; }

        /// <summary>
        /// Observer callback triggered by player events.
        /// </summary>
        /// <param name="pSubject">The event subject (Player).</param>
        /// <param name="pEventName">The notification name.</param>
        public void OnNotify(ISubject pSubject, string pEventName)
        {
            if (pSubject is Player lPlayer && pEventName == "Experience")
            {
                UpdateExperienceBar(lPlayer);
            }
        }
            
        /// <summary>
        /// Updates the fill amount and text display based on player XP values.
        /// </summary>
        /// <param name="pPlayer">The player instance to read values from.</param>
        private void UpdateExperienceBar(Player pPlayer)
        {
            if (mXpFill != null && pPlayer.MaxXP > 0f)
            {
                mXpFill.fillAmount = pPlayer.CurrentXP / pPlayer.MaxXP;
            }

            if (mXpText != null)
            {
                mXpText.text = $"XP: {pPlayer.CurrentXP} / {pPlayer.MaxXP}";
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
        }

        private void Start()
        {
            if (Player.Instance != null)
            {
                Player.Instance.AddObserver(this);
                UpdateExperienceBar(Player.Instance);
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
