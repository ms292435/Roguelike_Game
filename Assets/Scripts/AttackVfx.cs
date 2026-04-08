using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class AttackVfx : MonoBehaviour
    {
        public void DestroyVfx()
        {
            Destroy(gameObject);
        }
    }
}