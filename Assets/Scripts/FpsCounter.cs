using UnityEngine;

namespace Roguelike
{
    public class FpsCounter : MonoBehaviour
    {
        private float mDeltaTime = 0.0f;

        void Update()
        {
            mDeltaTime += (Time.unscaledDeltaTime - mDeltaTime) * 0.1f;
        }

        void OnGUI()
        {
            int lWidth = Screen.width, lHeight = Screen.height;

            GUIStyle lStyle = new();

            Rect lRect = new(10, 10, lWidth, lHeight * 0.02f);
            lStyle.alignment = TextAnchor.UpperLeft;
            lStyle.fontSize = lHeight * 2 / 100;
            lStyle.normal.textColor = Color.white;

            float lFps = 1.0f / mDeltaTime;
            string lText = string.Format("{0:0.} FPS", lFps);

            GUI.Label(lRect, lText, lStyle);
        }
    }
}