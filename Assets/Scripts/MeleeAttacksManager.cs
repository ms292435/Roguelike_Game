using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
namespace Roguelike
{
    public class MeleeAttacksManager : MonoBehaviour
    {
        public static MeleeAttacksManager Instance { get; private set; }
        private readonly Dictionary<GameObject, Queue<SwordSlash>> mPools = new();

        public SwordSlash GetVfx(GameObject pPrefab, Vector3 pPosition, Quaternion pRotation)
        {
            if (!mPools.ContainsKey(pPrefab))
                mPools[pPrefab] = new Queue<SwordSlash>();

            SwordSlash lSlash;

            if (mPools[pPrefab].Count > 0)
            {
                lSlash = mPools[pPrefab].Dequeue();
            }
            else
            {
                lSlash = Instantiate(pPrefab, transform).GetComponent<SwordSlash>();
                lSlash.OriginPrefab = pPrefab;
            }

            lSlash.transform.SetPositionAndRotation(pPosition, pRotation);
            lSlash.gameObject.SetActive(true);
            return lSlash;
        }

        public void ReturnToPool(SwordSlash pSlash)
        {
            pSlash.gameObject.SetActive(false);
            mPools[pSlash.OriginPrefab].Enqueue(pSlash);
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
    }
}