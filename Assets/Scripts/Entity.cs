using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public abstract class Entity : EntityBase
{
    private float mHealth;
    private float mDamage;
    private float mSpeed;

    public float Health
    {
        get { return mHealth; }
        set { mHealth = value; }
    }

    public float Damage
    {
        get { return mDamage; }
        set { mDamage = value; }
    }

    public float Speed
    {
        get { return mSpeed; }
        set { mSpeed = value; }
    }
}
