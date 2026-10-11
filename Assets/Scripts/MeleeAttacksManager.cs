using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Singleton object pool manager for melee visual attack instances (e.g. SwordSlash VFX).
    /// Prevents repeated instantiation and garbage collection overhead during frequent combat strikes.
    /// </summary>
    public class MeleeAttacksManager : MonoBehaviour
    {
        public static MeleeAttacksManager Instance { get; private set; }
        private readonly Dictionary<GameObject, Queue<SwordSlash>> mPools = new();

        /// <summary>
        /// Retrieves or instantiates a pooled SwordSlash VFX instance at a specified position and rotation.
        /// </summary>
        /// <param name="pPrefab">Source VFX prefab.</param>
        /// <param name="pPosition">World position to spawn at.</param>
        /// <param name="pRotation">Rotation orientation of the slash.</param>
        /// <returns>An active SwordSlash instance.</returns>
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

        /// <summary>
        /// Deactivates and returns a SwordSlash instance to its corresponding prefab pool.
        /// </summary>
        /// <param name="pSlash">The SwordSlash instance to recycle.</param>
        public void ReturnToPool(SwordSlash pSlash)
        {
            pSlash.gameObject.SetActive(false);
            mPools[pSlash.OriginPrefab].Enqueue(pSlash);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            { 
                Destroy(gameObject); 
                return; 
            }
            Instance = this;
            mPools.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}