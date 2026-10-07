using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Singleton manager handling global sound effect playback for weapons and enemy deaths.
    /// Manages independent audio sources with pitch randomization and rate-limiting to prevent clipping.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance accessible globally across gameplay systems.
        /// </summary>
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource mWeaponSource;
        [SerializeField] private AudioSource mEnemiesSource;

        private float mLastDeathSoundTime;
        private const float mDeathSoundCooldown = 0.05f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Plays an enemy death sound effect with pitch modulation and rate-limiting cooldown.
        /// </summary>
        /// <param name="pClip">The AudioClip to play.</param>
        public void PlayEnemyDeathSound(AudioClip pClip)
        {
            if (pClip == null || mEnemiesSource == null) return;

            // Verify cooldown to prevent audio clutter when multiple enemies die simultaneously
            if (Time.time - mLastDeathSoundTime > mDeathSoundCooldown)
            {
                // Randomize pitch slightly to add variety to the sound effects
                mEnemiesSource.pitch = Random.Range(0.8f, 1.2f);
                mEnemiesSource.PlayOneShot(pClip);
                mLastDeathSoundTime = Time.time;
            }
        }

        /// <summary>
        /// Plays a weapon attack sound effect with subtle pitch modulation.
        /// </summary>
        /// <param name="pClip">The AudioClip to play.</param>
        public void PlayWeaponSound(AudioClip pClip)
        {
            if (pClip != null && mWeaponSource != null)
            {
                mWeaponSource.pitch = Random.Range(0.9f, 1.1f);
                mWeaponSource.PlayOneShot(pClip);
            }
        }
    }
}