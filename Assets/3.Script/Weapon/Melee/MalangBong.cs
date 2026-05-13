using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MalangBong : NetworkBehaviour
{
    //[SerializeField] private Collider attackCollider;
    private readonly List<Collider> _pipeColliders = new List<Collider>();
    private Animator playerAnimator;

    private float damage;
    private float attackCooldown;

    //private bool canAttack = true;
    private NetworkVariable<bool> _canAttack = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
        );

    private ulong ownerClientId;
    private Transform followTarget;

    private HashSet<ulong> hitThisSwing = new HashSet<ulong>();

    [SerializeField] private float hitStartDelay = 0.2f;  // 스윙 시작 후 콜라이더 ON까지 대기
    [SerializeField] private float hitActiveDuration = 0.3f;  // 콜라이더 켜져 있는 시간

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        DisableAllColliders();
    }

    // PlayerNetwork에서 스폰 직후 호출하여 CardData 값 주입
    public void Initialize(float cardDamage, float cardSpeed, ulong ownerId, Animator playerAni)
    {
        damage = cardDamage;
        attackCooldown = cardSpeed;
        ownerClientId = ownerId;
        playerAnimator = playerAni;

        // 서버에서만 히트박스 세팅 (OnTriggerEnter 처리는 서버에서만 하므로 충분)
        if (IsServer)
        {
            SetupHitboxes();
        }
    }

    private void SetupHitboxes()
    {
        _pipeColliders.Clear();

        Collider[] allChildColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        foreach (Collider col in allChildColliders)
        {
            if (col.gameObject == gameObject) continue;

            col.isTrigger = true;
            col.enabled = false;

            MalangBongHitbox hitbox = col.gameObject.GetComponent<MalangBongHitbox>();
            if (hitbox == null)
                hitbox = col.gameObject.AddComponent<MalangBongHitbox>();

            hitbox.Setup(this);
            _pipeColliders.Add(col);
        }

        Debug.Log($"[MalangBong] 히트박스 설정 완료: {_pipeColliders.Count}개 파이프 콜라이더");
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    private void LateUpdate()
    {
        if (followTarget != null)
        {
            transform.position = followTarget.position;
            transform.rotation = followTarget.rotation;
        }
    }

    public void RequestAttack()
    {
        if (!IsOwner) return;

        if (!_canAttack.Value) return;

        PerformAttack_ServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PerformAttack_ServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != ownerClientId) return;

        if (!_canAttack.Value) return;

        StartCoroutine(AttackCoroutine());

        // 서버에서 플레이어 NetworkObjectId를 조회해 ClientRpc에 전달
        // ConnectedClients는 서버 전용이므로 여기서만 사용
        ulong playerNetObjId = 0;
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var ownerClient))
            playerNetObjId = ownerClient.PlayerObject.NetworkObjectId;

        PlayAttackAnimator_ClientRpc(playerNetObjId);
    }

    private IEnumerator AttackCoroutine()
    {
        _canAttack.Value = false;
        hitThisSwing.Clear();

        yield return new WaitForSeconds(hitStartDelay);

        foreach (Collider col in _pipeColliders)
            if (col != null) col.enabled = true;

        yield return new WaitForSeconds(hitActiveDuration);

        foreach (Collider col in _pipeColliders)
            if (col != null) col.enabled = false;

        float remaining = attackCooldown - hitStartDelay - hitActiveDuration;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        _canAttack.Value = true;
    }

    [ClientRpc]
    private void PlayAttackAnimator_ClientRpc(ulong playerNetworkObjectId)
    {
        if (playerAnimator == null)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(playerNetworkObjectId, out NetworkObject playerNetObj))
            {
                playerAnimator = playerNetObj.GetComponent<Animator>();
            }
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("MeleeAttack");
        }
        else
        {
            Debug.LogWarning($"[MalangBong] {ownerClientId} player Animator is null");
        }
    }

    private void DisableAllColliders()
    {
        foreach (Collider col in _pipeColliders)
        {
            if (col != null) col.enabled = false;
        }
    }

    public void OnHitboxTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out Combat targetCombat))
        {
            if (other.TryGetComponent(out NetworkObject targetNetObj))
            {
                if (targetNetObj.OwnerClientId == ownerClientId) return;
                if (hitThisSwing.Contains(targetNetObj.OwnerClientId)) return;
                hitThisSwing.Add(targetNetObj.OwnerClientId);
            }

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var client))
            {
                if (client.PlayerObject.TryGetComponent(out Combat myCombat))
                {
                    targetCombat.TakeDamage(myCombat, damage);
                }
            }
        }
    }
}