using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike
{
    public class ExperienceManager : MonoBehaviour
    {
        private readonly List<Experience> mFlyingExperience = new();
        private readonly List<Experience> mNearbyExpCache = new();

        private void HandleExperience()
        {
            var lPlayerPosition = Player.Instance.mCurrentPosition;
            float lSqrAttract = Player.Instance.mAttractRadius * Player.Instance.mAttractRadius;
            float lSqrCollect = Player.Instance.mCollectRadius * Player.Instance.mCollectRadius;

            SpatialGrid.Instance.GetNearbyExperience(lPlayerPosition, Player.Instance.mAttractRadius, mNearbyExpCache);

            for (int i = mNearbyExpCache.Count - 1; i >= 0; i--)
            {
                Experience lExperience = mNearbyExpCache[i];

                Vector3 lOffset = lPlayerPosition - lExperience.mCurrentPosition;
                float lSqrDist = lOffset.sqrMagnitude;

                if (!lExperience.IsTrigger && lSqrDist < lSqrAttract)
                {
                    lExperience.Trigger();
                    mFlyingExperience.Add(lExperience);
                }
            }

            for (int i = mFlyingExperience.Count - 1; i >= 0; i--)
            {
                Experience lExperience = mFlyingExperience[i];
                float lSqrDist = (lPlayerPosition - lExperience.mCurrentPosition).sqrMagnitude;

                if (lSqrDist < lSqrCollect)
                {
                    lExperience.Collect();
                    mFlyingExperience.RemoveAt(i);
                }
            }
        }
        void Update()
        {
            HandleExperience();
        }
    }
}