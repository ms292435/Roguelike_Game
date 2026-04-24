using UnityEngine;

namespace Roguelike
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource mWeaponSource;
        [SerializeField] private AudioSource mEnemiesSource;


        // Limiter to avoid too many death sounds playing at once
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

        public void PlayEnemyDeathSound(AudioClip pClip)
        {
            if (pClip == null || mEnemiesSource == null) return;

            // Verify cooldown to prevent audio clutter when multiple enemies die simultaneously
            if (Time.time - mLastDeathSoundTime > mDeathSoundCooldown)
            {
                // Randomize pitch slightly to add variety to the sound effects
                mEnemiesSource.pitch = Random.Range(0.8f, 1.2f);

                // PlayOneShot is used to allow multiple sounds to overlap without cutting each other off
                mEnemiesSource.PlayOneShot(pClip);

                mLastDeathSoundTime = Time.time;
            }
        }

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