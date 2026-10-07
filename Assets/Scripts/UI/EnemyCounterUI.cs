using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Roguelike.DOTS;

namespace Roguelike
{
    /// <summary>
    /// UI component displaying real-time enemy statistics (Visible, Total count, and Target Horde) on the screen HUD.
    /// Queries the static counts updated by EnemyCounterSystem and BenchmarkController.
    /// </summary>
    public class EnemyCounterUI : MonoBehaviour
    {
        [FormerlySerializedAs("m_Label")]
        [SerializeField] private TextMeshProUGUI mLabel;

        private void Update()
        {
            if (mLabel != null)
            {
                mLabel.text = $"Visible Enemies : {EnemyCounterSystem.VisibleEnemies:N0}\nTotal Enemies : {EnemyCounterSystem.TotalEnemies:N0} / Target : {BenchmarkController.TargetEnemyCount:N0}";
            }
        }
    }
}
