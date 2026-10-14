using Cinemachine;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Controls the orthographic size of the scene's Cinemachine virtual camera, i.e. how far the player can see.
    /// The camera is looked up lazily and looked up again after a scene reload.
    /// </summary>
    public static class CameraZoom
    {
        private static CinemachineVirtualCamera sCamera;

        private static CinemachineVirtualCamera VirtualCamera
        {
            get
            {
                // Unity's null check also catches a camera destroyed by a scene reload
                if (sCamera == null) sCamera = Object.FindAnyObjectByType<CinemachineVirtualCamera>();
                return sCamera;
            }
        }

        /// <summary>
        /// Sets the camera size immediately.
        /// </summary>
        /// <param name="pSize">Orthographic size: half of the visible height, in world units.</param>
        public static void SetSize(float pSize)
        {
            var lCamera = VirtualCamera;
            if (lCamera != null) lCamera.m_Lens.OrthographicSize = pSize;
        }

        /// <summary>
        /// Moves the camera size smoothly towards a target size.
        /// </summary>
        /// <param name="pTargetSize">Orthographic size to reach.</param>
        /// <param name="pSharpness">How fast the size converges: higher is faster.</param>
        /// <param name="pDeltaTime">Time elapsed since the last call, in seconds.</param>
        public static void MoveTowards(float pTargetSize, float pSharpness, float pDeltaTime)
        {
            var lCamera = VirtualCamera;
            if (lCamera == null) return;

            // Frame-rate independent exponential smoothing
            float lBlend = 1f - Mathf.Exp(-pSharpness * pDeltaTime);
            lCamera.m_Lens.OrthographicSize = Mathf.Lerp(lCamera.m_Lens.OrthographicSize, pTargetSize, lBlend);
        }
    }
}
