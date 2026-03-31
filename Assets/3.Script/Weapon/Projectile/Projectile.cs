using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ProjectileData
{
    public ulong OwnerClientId;
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

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out Combat otherCombat))
        {
            otherCombat.TakeDamage(combat, combat.CombatData.Damage);
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
