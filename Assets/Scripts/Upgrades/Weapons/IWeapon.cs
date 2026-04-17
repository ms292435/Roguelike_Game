using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public interface IWeapon
    {
        WeaponUpgradeData Data { get; }

        int WeaponLevel { get; set; }

        public void Attack();
        public void UpdateWeapon(float pDeltaTime);
        public void Initialize(WeaponUpgradeData pData);
        WeaponUpgradeProposal GetNextUpgradeProposal();
    }
}