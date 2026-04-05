using UnityEngine;

namespace Roguelike
{
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
    }
}