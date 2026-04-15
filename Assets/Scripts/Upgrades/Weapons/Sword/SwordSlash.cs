using UnityEngine;
using UnityEngine.VFX;

namespace Roguelike
{

    public class SwordSlash : MonoBehaviour
    {
        [SerializeField] private float mDuration = 0.5f;
        public GameObject OriginPrefab { get; set; }
        private float mTimer;

        void OnEnable()
        {
            mTimer = 0f;
        }

        void Update()
        {
            mTimer += Time.deltaTime;
            if (mTimer >= mDuration)
            {
                MeleeAttacksManager.Instance.ReturnToPool(this);
            }
        }
    }
}