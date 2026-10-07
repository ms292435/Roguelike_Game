using TMPro;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// UI component calculating and displaying exponential moving average frames-per-second (FPS).
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        public TextMeshProUGUI mFpsText;

        private float mDeltaTime = 0.0f;

        private void Update()
        {
            mDeltaTime += (Time.unscaledDeltaTime - mDeltaTime) * 0.1f;

            if (mFpsText != null)
            {
                float lFps = 1.0f / mDeltaTime;
                mFpsText.text = string.Format("{0:0.} FPS", lFps);
            }
        }
    }
}
