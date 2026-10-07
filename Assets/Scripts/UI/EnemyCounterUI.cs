using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Roguelike.DOTS;

namespace Roguelike
{
    /// <summary>
    /// UI component displaying real-time enemy statistics (Visible and Total count) on the screen HUD.
    /// Queries the static counts updated by EnemyCounterSystem.
    /// </summary>
    public class EnemyCounterUI : MonoBehaviour
    {
        [FormerlySerializedAs("m_Label")]
        [SerializeField] private TextMeshProUGUI mLabel;

        private void Update()
        {
            if (mLabel != null)
            {
                mLabel.text = $"Visible Enemies : {EnemyCounterSystem.VisibleEnemies}\nTotal Enemies : {EnemyCounterSystem.TotalEnemies}";
            }
        }
    }
}
