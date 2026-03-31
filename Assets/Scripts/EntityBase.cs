using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class EntityBase : MonoBehaviour
{
    private float mPosX;
    private float mPosY;

    public float GetPosX()
    {
        return mPosX;
    }

    public void SetPosX(float pValue)
    {
        mPosX = pValue;
    }
    public float GetPosY() 
    {
        return mPosY;
    }

    public void SetPosY(float pValue)
    {
        mPosY = pValue;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}