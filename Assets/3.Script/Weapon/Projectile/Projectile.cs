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

        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            if (playerHealth.OwnerClientId == projectileData.OwnerClientId) return;

            playerHealth.TakeDamage(
                projectileData.Damage,
                projectileData.OwnerFaction,
                projectileData.OwnerClientId
            );
            // 맞으면 총알 제거
            GetComponent<NetworkObject>().Despawn();
        }
    }

    public void Initialize(ProjectileData projectileData)
    {
        this.projectileData = projectileData;
    }

    public void AddForce(Vector3 dir)
    {
        rb.AddForce(dir.normalized * projectileData.BulletSpeed);
    }
}
