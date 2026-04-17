using UnityEditor;
using UnityEngine;

namespace Roguelike
{
    [CreateAssetMenu]
    public class WeaponUpgradeData : UpgradeData
    {
        public GameObject mWeaponLogicPrefab;
        public GameObject mProjectilePrefab;
        public string Type;
        public int MaxLevel = 10;
    }
}