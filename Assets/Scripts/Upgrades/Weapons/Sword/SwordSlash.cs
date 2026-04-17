using UnityEngine;
using UnityEngine.VFX;

namespace Roguelike
{

    public class SwordSlash : MonoBehaviour
    {
        public GameObject OriginPrefab { get; set; }

        [SerializeField] private float mDuration = 0.5f;

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