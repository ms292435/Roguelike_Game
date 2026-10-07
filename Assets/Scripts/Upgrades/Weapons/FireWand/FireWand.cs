using Unity.Entities;
using UnityEngine;

namespace Roguelike
{
    /// <summary>
    /// Ranged magic weapon that periodically targets and fires projectiles at the closest enemy.
    /// Interacts with the unmanaged ECS spatial hash grid to locate enemy entities.
    /// </summary>
    public class FireWand : MonoBehaviour, IWeapon
    {
        public WeaponUpgradeData Data => mData;
        public int WeaponLevel { get; set; }
        public AudioClip AttackSound => mAttackSound;

        private WeaponUpgradeData mData;
        private float mDamage = 50f;
        private float mFireRate = 0.5f;
        private float mFireTimer = 0f;
        private const float mTargetRange = 18f;

        [SerializeField] private AudioClip mAttackSound;

        /// <summary>
        /// Initializes the weapon with configuration data.
        /// </summary>
        /// <param name="pData">The weapon upgrade configuration data.</param>
        public void Initialize(WeaponUpgradeData pData) => mData = pData;

        /// <summary>
        /// Advances weapon cooldown timer and executes an attack if ready.
        /// </summary>
        /// <param name="pDeltaTime">Elapsed frame time.</param>
        /// <returns>True if an attack was triggered this frame, false otherwise.</returns>
        public bool UpdateWeapon(float pDeltaTime)
        {
            mFireTimer += pDeltaTime;
            if (mFireTimer >= mFireRate)
            {
                Attack();
                mFireTimer = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the closest enemy using the ECS spatial hash grid and launches a projectile towards it.
        /// </summary>
        public void Attack()
        {
            var lPlayerPosition = Player.Instance.mCurrentPosition;

            Vector3 lTargetPos = GetClosestEnemyPositionECS(lPlayerPosition);
            if (lTargetPos == Vector3.zero) return;

            Vector3 lDirection = (lTargetPos - lPlayerPosition).normalized;
            Projectile lProjectile = ProjectileManager.Instance.GetProjectile(mData.mProjectilePrefab, lPlayerPosition);
            lProjectile.Init(lDirection, mDamage * Player.Instance.Damage);
        }

        /// <summary>
        /// Queries the ECS unmanaged spatial hash grid to find the nearest enemy position within target range.
        /// </summary>
        /// <param name="pFrom">Origin position to search from (player position).</param>
        /// <returns>The world position of the closest enemy, or Vector3.zero if none found.</returns>
        private Vector3 GetClosestEnemyPositionECS(Vector3 pFrom)
        {
            var lWorld = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (lWorld == null) return Vector3.zero;

            var lHashHandle = lWorld.Unmanaged.GetExistingUnmanagedSystem<DOTS.EnemyHashSystem>();
            if (lHashHandle == SystemHandle.Null) return Vector3.zero;

            ref var lHashSystem = ref lWorld.Unmanaged.GetUnsafeSystemRef<DOTS.EnemyHashSystem>(lHashHandle);
            if (!lHashSystem.mSpatialGrid.IsCreated) return Vector3.zero;

            float lCellSize = DOTS.EnemyHashSystem.CellSize;
            Unity.Mathematics.float3 lFrom = pFrom;
            lFrom.z = 0f;

            Unity.Mathematics.int2 lCenterCell = new(
                (int)Mathf.Floor(pFrom.x / lCellSize),
                (int)Mathf.Floor(pFrom.y / lCellSize)
            );

            float lBestSqr = mTargetRange * mTargetRange;
            Vector3 lBestPos = Vector3.zero;
            int lCellRange = Mathf.CeilToInt(mTargetRange / lCellSize);

            for (int x = -lCellRange; x <= lCellRange; x++)
            {
                for (int y = -lCellRange; y <= lCellRange; y++)
                {
                    var lCell = lCenterCell + new Unity.Mathematics.int2(x, y);
                    if (lHashSystem.mSpatialGrid.TryGetFirstValue(lCell, out DOTS.EnemyGridData lData, out var lIt))
                    {
                        do
                        {
                            Unity.Mathematics.float3 lEp = lData.mPosition;
                            lEp.z = 0f;
                            float lSqr = Unity.Mathematics.math.lengthsq(lEp - lFrom);
                            if (lSqr < lBestSqr)
                            {
                                lBestSqr = lSqr;
                                lBestPos = new Vector3(lData.mPosition.x, lData.mPosition.y, 0f);
                            }
                        } while (lHashSystem.mSpatialGrid.TryGetNextValue(out lData, ref lIt));
                    }
                }
            }

            return lBestPos;
        }

        /// <summary>
        /// Generates an upgrade proposal for the level up selection screen.
        /// </summary>
        /// <returns>A proposed upgrade option with an execution callback.</returns>
        public WeaponUpgradeProposal GetNextUpgradeProposal()
        {
            int lRandomChoice = Random.Range(0, 2);
            var lProposal = new WeaponUpgradeProposal();

            switch (lRandomChoice)
            {
                case 0:
                    lProposal.Description = "Damage +20%";
                    lProposal.ApplyAction = () => { mDamage *= 1.2f; WeaponLevel++; };
                    break;
                case 1:
                    lProposal.Description = "Fire rate +15%";
                    lProposal.ApplyAction = () => { mFireRate *= 0.85f; WeaponLevel++; };
                    break;
            }
            return lProposal;
        }
    }
}