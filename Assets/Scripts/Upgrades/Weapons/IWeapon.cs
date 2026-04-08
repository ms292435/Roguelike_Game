using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public interface IWeapon
    {
        WeaponUpgradeData Data { get; }
        public void Attack();
        public void UpdateWeapon(float pDeltaTime);

        public void Initialize(WeaponUpgradeData pData);
    }
}