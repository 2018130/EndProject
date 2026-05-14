using System.Collections;
using Unity.Netcode;
using UnityEngine;

public struct ProjectileData
{
    public ulong OwnerClientId;
    public Faction OwnerFaction;

    public int MaxHitCountPerShot;

    public float BulletSpeed;
    public float GravityStartDistance;
    public float LiftForce; //조준선 보정용, 위로 힘을 줌
    public float AirResistance; // 탄환 감속량 (0이면 저격총처럼 무한 직선)

    public float Damage;
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : NetworkBehaviour
{
    private ProjectileData projectileData;

    private Rigidbody rb;

    private Vector3 startPosition;

    private bool gravityEnabled = false;
    private bool isHit = false;

    // ─────────────────────────────
    // Unity
    // ─────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        StartCoroutine(AutoDespawn());
    }

    private void Update()
    {
        if (gravityEnabled)
            return;

        if (Vector3.Distance(startPosition, transform.position)
            >= projectileData.GravityStartDistance)
        {
            rb.useGravity = true;
            gravityEnabled = true;
        }
    }

    // ─────────────────────────────
    // Trigger
    // ─────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (isHit)
            return;

        Debug.Log($"[Projectile] Trigger Hit : {other.name}");

        // ─────────────────────────────
        // Sand Destructible
        // ─────────────────────────────

        SandDestructible sand =
            other.GetComponentInParent<SandDestructible>();

        if (sand != null)
        {
            Debug.Log("[Projectile] SandDestructible Hit");

            sand.RegisterHit(
                other.ClosestPoint(transform.position),
                projectileData.Damage
            );

            isHit = true;

            GetComponent<NetworkObject>().Despawn();

            return;
        }

        // ─────────────────────────────
        // Player
        // ─────────────────────────────

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        if (playerHealth.OwnerClientId
            == projectileData.OwnerClientId)
            return;

        isHit = true;

        playerHealth.TakeDamage(
            projectileData.Damage,
            projectileData.OwnerFaction,
            projectileData.OwnerClientId,
            other.ClosestPoint(transform.position),
            transform.position
        );

        GetComponent<NetworkObject>().Despawn();
    }

    // ─────────────────────────────
    // Init
    // ─────────────────────────────

    public void Initialize(ProjectileData projectileData)
    {
        this.projectileData = projectileData;
        startPosition = transform.position;

        // 저격총 데이터에서 이 값을 0으로 보내면 속도가 전혀 줄지 않습니다.
        rb.linearDamping = projectileData.AirResistance;

        IgnoreOwnerCollision();
    }

    private void IgnoreOwnerCollision()
    {
        if (!NetworkManager.Singleton.ConnectedClients
            .TryGetValue(projectileData.OwnerClientId,
                out var client))
            return;

        Collider myCollider = GetComponent<Collider>();

        if (myCollider == null)
            return;

        Collider[] ownerColliders =
            client.PlayerObject
                .GetComponentsInChildren<Collider>();

        foreach (Collider col in ownerColliders)
        {
            Physics.IgnoreCollision(myCollider, col);
        }
    }

    // ─────────────────────────────
    // Movement
    // ─────────────────────────────

    public void AddForce(Vector3 dir)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 조준 방향(dir)에 위쪽 방향(Vector3.up) 보정치를 더합니다.
        // liftForce가 0.1이라면 위쪽으로 약 5.7도 정도 더 들어올려 발사합니다.
        Vector3 correction = Vector3.up * projectileData.LiftForce;
        Vector3 finalDir = (dir.normalized + correction).normalized;

        rb.linearVelocity = finalDir * projectileData.BulletSpeed;
    }

    // ─────────────────────────────
    // Lifetime
    // ─────────────────────────────

    private IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(20f);

        if (!IsSpawned)
            yield break;

        GetComponent<NetworkObject>().Despawn();
    }
}