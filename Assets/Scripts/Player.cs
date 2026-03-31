using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Player : Entity
{
    public List<Experience> mExperienceList = new List<Experience>();
    public ExperienceBar mExperienceBar;

    public float mFireRate = 10f;


    public float mAttractRadius = 2f;
    public float mCollectRadius = 1f;

    private Rigidbody2D mRigibody;

    private float mFireTimer = 0f;


    public static Player Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private Player()
    {
        Health = 100f;
        Damage = 50f;
        Speed = 10f;
    }

    private void Start()
    {
        mRigibody = GetComponent<Rigidbody2D>();
        mRigibody.freezeRotation = true;
    }

    private void Update()
    {
        float lMoveHorizontal = Input.GetAxis("Horizontal");
        float lMoveVertical = Input.GetAxis("Vertical");

        mRigibody.freezeRotation = true;
        mRigibody.velocity = new Vector3(lMoveHorizontal, lMoveVertical) * Speed;

        HandleExperience();
        HandleShooting();
    }

    private void HandleExperience()
    {
        Vector3 playerPos = transform.position;
        float sqrAttract = mAttractRadius * mAttractRadius;
        float sqrCollect = mCollectRadius * mCollectRadius;

        // On boucle à l'envers car on va potentiellement retirer des éléments (Collect)
        for (int i = mExperienceList.Count - 1; i >= 0; i--)
        {
            Experience exp = mExperienceList[i];

            // Calcul de distance au carré (plus performant)
            Vector3 offset = playerPos - exp.transform.position;
            float sqrDist = offset.sqrMagnitude;

            // 1. Détection (Trigger)
            if (!exp.IsTrigger && sqrDist < sqrAttract)
            {
                exp.Trigger();
            }

            // 2. Mouvement (si trigger)
            if (exp.IsTrigger)
            {
                exp.transform.position = Vector3.MoveTowards(
                    exp.transform.position,
                    playerPos,
                    exp.mSpeed * Time.deltaTime
                );
            }

            // 3. Collecte
            if (sqrDist < sqrCollect)
            {
                exp.Collect(mExperienceBar);
                // On ne fait rien d'autre pour cet index, il est retourné au pool
            }
        }
    }

    void HandleShooting()
    {
        mFireTimer += Time.deltaTime;

        if (mFireTimer >= 1f / mFireRate)
        {
            Shoot();
            mFireTimer = 0f;
        }
    }

    void Shoot()
    {
        Enemy target = SpatialGrid.Instance.GetClosestEnemyInGrid(transform.position);

        if (target == null) return;

        Vector3 direction = (target.transform.position - transform.position).normalized;

        Projectile proj = ProjectilePool.Instance.GetProjectile();
        proj.transform.position = transform.position;
        proj.Init(direction, Damage);
    }
}