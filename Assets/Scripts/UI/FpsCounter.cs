using TMPro;
using UnityEngine;

namespace Roguelike
{
    public class FpsCounter : MonoBehaviour
    {
        public TextMeshProUGUI mFpsText;

        private float mDeltaTime = 0.0f;

        void Update()
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