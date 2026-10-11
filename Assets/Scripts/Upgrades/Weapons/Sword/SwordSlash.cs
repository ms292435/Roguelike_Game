using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Visual slash effect instance spawned during sword melee attacks.
    /// Handles lifetime duration and automatically returns to the MeleeAttacksManager pool.
    /// </summary>
    public class SwordSlash : MonoBehaviour
    {
        /// <summary>
        /// Reference to the source prefab used to identify the correct object pool.
        /// </summary>
        public GameObject OriginPrefab { get; set; }

        [SerializeField] private float mDuration = 0.5f;

        private float mTimer;

        private void OnEnable()
        {
            mTimer = 0f;
        }

        private void Update()
        {
            mTimer += Time.deltaTime;
            if (mTimer >= mDuration)
            {
                MeleeAttacksManager.Instance.ReturnToPool(this);
            }
        }
    }
}