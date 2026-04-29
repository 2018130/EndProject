using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class PenguinChargeObject : NetworkBehaviour
{
    // 3√ 
    private Rigidbody rb;
    private float speed;  
    private float damage;
    private ulong ownerClientId;
    private Faction ownerFaction;

    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private Transform effectPos;
    private float effectTimer;

    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>();

    private void Awake()
    {
        TryGetComponent(out rb);
    }

    public override void OnNetworkSpawn()
    {
        AudioManager.Instance.PlaySFX("PenguinCharge",true);
    }

    public override void OnNetworkDespawn()
    {
        AudioManager.Instance.StopSFX();
    }

    public void Initialize(float speed, float damage, Vector3 dir, ulong ownerClientId, Faction ownerFaction)
    {
        this.speed = speed;
        this.damage = damage;
        this.ownerClientId = ownerClientId;
        this.ownerFaction = ownerFaction;
        rb.AddForce(dir * speed, ForceMode.Impulse);

        isMoving.Value = true;

        StartCoroutine(LifeRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerNetwork player))
        {
            if (player.OwnerClientId == ownerClientId) return;

            // HP 10
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            health.TakeDamage(damage, ownerFaction, ownerClientId);

            // ≥ÀπÈ
            Vector3 knockbackDir = (player.transform.position - transform.position).normalized;
            player.ApplyKnockback_ClientRpc(knockbackDir * 10f);
        }
    }

    private void Update()
    {
        if (isMoving.Value)
        {
            effectTimer -= Time.deltaTime;
            if (effectTimer <= 0f)
            {
                effectTimer = spawnInterval;

                SkillEffectPool.Instance.Get(effectPrefab, effectPos.position, Quaternion.identity);
            }
        }
    }
    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(3f);
        GetComponent<NetworkObject>().Despawn();
    }


}
