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
}

public class Projectile : NetworkBehaviour
{
    private ProjectileData projectileData;

    private Rigidbody rb;

    private Combat combat;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        combat = GetComponent<Combat>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        StartCoroutine(AutoDespawn());
    }

    private IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(20f);
        if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
        //GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(
                combat.CombatData.Damage,
                projectileData.OwnerFaction,
                projectileData.OwnerClientId
            );
            // 맞으면 총알 제거
            //GetComponent<NetworkObject>().Despawn();
            if (NetworkObject != null && NetworkObject.IsSpawned) NetworkObject.Despawn();
        }
    }

    public void Initialize(ProjectileData projectileData)
    {
        this.projectileData = projectileData;
    }

    public void AddForce(Vector3 dir)
    {
        if(dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }

        rb.AddForce(dir.normalized * projectileData.BulletSpeed);
    }
}
