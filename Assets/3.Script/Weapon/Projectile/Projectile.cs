using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ProjectileData
{
    public ulong OwnerClientId;
    public Faction OwnerFaction;
    public int MaxHitCountPerShot;
    public float BulletSpeed;
    public float GravityStartDistance;
    public float Damage;

}

public class Projectile : NetworkBehaviour
{
    private ProjectileData projectileData;

    private Rigidbody rb;

    private Vector3 startPosition;
    private bool gravityEnabled = false;

    private bool isHit = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(AutoDespawn());
    }

    private void Update()
    {
        if (gravityEnabled) return;

        if (Vector3.Distance(startPosition, transform.position) >= projectileData.GravityStartDistance)
        {
            rb.useGravity = true;
            gravityEnabled = true;
        }
    }

    private IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(20f);
        GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (isHit) return;

        // ★ damage 파라미터 추가
        if (other.TryGetComponent(out SandDestructible sand))
        {
            sand.RegisterHit(
                other.ClosestPoint(transform.position),
                projectileData.Damage          // weaponData.Damage에서 넘어온 값
            );
            isHit = true;
            GetComponent<NetworkObject>().Despawn();
            return;
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth == null) return;
        if (playerHealth.OwnerClientId == projectileData.OwnerClientId) return;

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
    public void Initialize(ProjectileData projectileData)
    {
        this.projectileData = projectileData;

        startPosition = transform.position;

        // 발사자 플레이어 콜라이더 무시
        if (NetworkManager.Singleton.ConnectedClients
            .TryGetValue(projectileData.OwnerClientId, out var client))
        {
            Collider myCollider = GetComponent<Collider>();
            if (myCollider == null) return;

            // 발사자의 모든 콜라이더 무시
            Collider[] ownerColliders = client.PlayerObject.GetComponentsInChildren<Collider>();
            foreach (Collider col in ownerColliders)
            {
                Physics.IgnoreCollision(myCollider, col);
            }
        }
    }

    public void AddForce(Vector3 dir)
    {
        rb.AddForce(dir.normalized * projectileData.BulletSpeed);
    }
}
