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

        Debug.Log($"충돌: {other.gameObject.name}, 레이어: {LayerMask.LayerToName(other.gameObject.layer)}");

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return; // 플레이어 아니면 무시

        if (playerHealth.OwnerClientId == projectileData.OwnerClientId) return; // 자기 자신 무시
        Debug.Log($"적 때린 거 맞음");
        isHit = true;
        playerHealth.TakeDamage(projectileData.Damage, projectileData.OwnerFaction, projectileData.OwnerClientId);
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
