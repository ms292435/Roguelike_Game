using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Base class providing foundational position state for game entities.
    /// </summary>
    public abstract class EntityBase : MonoBehaviour
    {
        private float mPosX;
        private float mPosY;

        /// <summary>
        /// Retrieves the entity's recorded horizontal position.
        /// </summary>
        /// <returns>X-axis coordinate.</returns>
        public float GetPosX() => mPosX;

        /// <summary>
        /// Sets the entity's horizontal position.
        /// </summary>
        /// <param name="pValue">X-axis coordinate value.</param>
        public void SetPosX(float pValue) => mPosX = pValue;

        /// <summary>
        /// Retrieves the entity's recorded vertical position.
        /// </summary>
        /// <returns>Y-axis coordinate.</returns>
        public float GetPosY() => mPosY;

        /// <summary>
        /// Sets the entity's vertical position.
        /// </summary>
        /// <param name="pValue">Y-axis coordinate value.</param>
        public void SetPosY(float pValue) => mPosY = pValue;
    }
}