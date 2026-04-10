using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike
{
    public class ExperienceBar : MonoBehaviour, IObserver
    {
        public Image mXpFill;
        public TextMeshProUGUI mXpText;
        public static ExperienceBar Instance { get; private set; }

        public void OnNotify(ISubject pSubject, string pEventName)
        {
            if (pSubject is Player lPlayer && pEventName == "Experience")
            {
                UpdateExperienceBar(lPlayer);
            }
        }
            
        private void UpdateExperienceBar(Player pPlayer)
        {
            mXpFill.fillAmount = pPlayer.CurrentXP / pPlayer.MaxXP;
            if (mXpText != null)
                mXpText.text = $"XP: {pPlayer.CurrentXP} / {pPlayer.MaxXP}";
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            Player.Instance.AddObserver(this);
            UpdateExperienceBar(Player.Instance);
        }
    }
}