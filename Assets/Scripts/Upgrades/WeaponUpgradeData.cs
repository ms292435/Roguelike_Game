using UnityEditor;
using UnityEngine;

namespace Roguelike
{
    [CreateAssetMenu]
    public class WeaponUpgradeData : UpgradeData
    {
        public GameObject mVisualPrefab;
        public string Type;
    }
}