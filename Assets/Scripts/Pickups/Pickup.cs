using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Rare bonuses dropped by killed enemies.
    /// </summary>
    public enum PickupType
    {
        Heal,
        Magnet,
        Bomb,
    }

    /// <summary>
    /// Bonus lying on the ground, collected when the player walks over it.
    /// Pickups are rare and few at a time, so they are plain GameObjects rather than entities.
    /// Their sprites are loaded from Resources/Pickups, so they need no setup in the scene.
    /// </summary>
    public class Pickup : MonoBehaviour
    {
        // --- Effects ---
        private const float HealFraction = 0.3f;        // Share of the maximum health restored
        private const float BombDamage = 2000f;         // Kills regular enemies and most elites, not the boss
        private const float BombScreenMargin = 1.1f;    // Bomb radius, relative to the half diagonal of the screen

        // --- Behaviour on the ground ---
        private const float CollectRadius = 1.3f;
        private const float Lifetime = 30f;
        private const float BlinkDuration = 5f;         // Blinks during its last seconds before disappearing
        private const float BobAmplitude = 0.12f;
        private const float BobSpeed = 3f;
        private const float Scale = 1.4f;

        // Drop weights of each type: heals and magnets are more common than bombs
        private const float HealWeight = 40f;
        private const float MagnetWeight = 35f;
        private const float BombWeight = 25f;

        private PickupType mType;
        private SpriteRenderer mRenderer;
        private Vector3 mBasePosition;
        private float mAge;

        /// <summary>
        /// Spawns a pickup of a random type, drawn with the drop weights.
        /// </summary>
        /// <param name="pPosition">World position where the pickup drops.</param>
        public static void SpawnRandom(Vector3 pPosition)
        {
            float lRoll = Random.value * (HealWeight + MagnetWeight + BombWeight);
            PickupType lType = lRoll < HealWeight ? PickupType.Heal
                : lRoll < HealWeight + MagnetWeight ? PickupType.Magnet
                : PickupType.Bomb;
            Spawn(lType, pPosition);
        }

        /// <summary>
        /// Spawns a pickup of a given type.
        /// </summary>
        /// <param name="pType">Type of bonus.</param>
        /// <param name="pPosition">World position where the pickup drops.</param>
        public static void Spawn(PickupType pType, Vector3 pPosition)
        {
            var lObject = new GameObject($"Pickup_{pType}");
            lObject.transform.position = new Vector3(pPosition.x, pPosition.y, 0f);
            lObject.transform.localScale = Vector3.one * Scale;

            var lPickup = lObject.AddComponent<Pickup>();
            lPickup.mType = pType;
            lPickup.mBasePosition = lObject.transform.position;

            lPickup.mRenderer = lObject.AddComponent<SpriteRenderer>();
            lPickup.mRenderer.sprite = Resources.Load<Sprite>($"Pickups/{pType.ToString().ToLowerInvariant()}");

            // Above the ground and the enemies, below the player (sorting order 1 on this layer)
            lPickup.mRenderer.sortingLayerName = "Entities";
            lPickup.mRenderer.sortingOrder = 0;
        }

        private void Update()
        {
            // Scaled delta time: pickups age and bob only while the game runs
            mAge += Time.deltaTime;
            if (mAge >= Lifetime || Player.Instance == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = mBasePosition + Vector3.up * (Mathf.Sin(mAge * BobSpeed) * BobAmplitude);

            // Blinks faster and faster before disappearing
            float lTimeLeft = Lifetime - mAge;
            mRenderer.enabled = lTimeLeft > BlinkDuration || Mathf.Repeat(mAge * (12f - lTimeLeft), 1f) < 0.6f;

            Vector3 lToPlayer = Player.Instance.transform.position - mBasePosition;
            lToPlayer.z = 0f;
            if (lToPlayer.sqrMagnitude <= CollectRadius * CollectRadius)
            {
                Apply();
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Applies the effect of the bonus.
        /// </summary>
        private void Apply()
        {
            switch (mType)
            {
                case PickupType.Heal:
                    Player.Instance.Heal(Player.Instance.MaxHealth * HealFraction);
                    break;

                case PickupType.Magnet:
                    DOTS.ExperienceMovementSystem.RequestAttractAll();
                    break;

                case PickupType.Bomb:
                    // Damages every enemy on screen: a disc covering the whole view around the camera
                    Camera lCamera = Camera.main;
                    if (lCamera == null) break;

                    float lHalfHeight = lCamera.orthographicSize;
                    float lHalfWidth = lHalfHeight * lCamera.aspect;
                    float lRadius = Mathf.Sqrt(lHalfWidth * lHalfWidth + lHalfHeight * lHalfHeight) * BombScreenMargin;
                    DOTS.EnemyBridge.QueueAreaDamage(lCamera.transform.position, 0f, lRadius, BombDamage, 0f);
                    GameHUD.FlashScreen();
                    break;
            }
        }
    }
}
