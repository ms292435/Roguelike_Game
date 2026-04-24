using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public interface IWeapon
    {
        WeaponUpgradeData Data { get; }

        AudioClip AttackSound { get; }

        int WeaponLevel { get; set; }

        public void Attack();
        public bool UpdateWeapon(float pDeltaTime);
        public void Initialize(WeaponUpgradeData pData);
        WeaponUpgradeProposal GetNextUpgradeProposal();
    }
}