using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MalangBong : NetworkBehaviour
{
    [SerializeField] private Collider attackCollider;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    // PlayerNetwork에서 스폰 직후 호출하여 CardData 값 주입
    public void Initialize(float cardDamage, float cardSpeed, ulong ownerId, Animator playerAni)
    {
        damage = cardDamage;
        attackCooldown = cardSpeed;
        this.ownerClientId = ownerId;
        this.playerAnimator = playerAni;

        if (attackCollider != null)
            attackCollider.enabled = false;
        //if (animator == null)
        //    animator = GetComponent<Animator>();
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

        Debug.Log("4444");
        PerformAttack_ServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PerformAttack_ServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != ownerClientId) return;

        if (!_canAttack.Value) return;

        Debug.Log("5555");
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
        Debug.Log("6666");
        _canAttack.Value = false;
        hitThisSwing.Clear();

        yield return new WaitForSeconds(attackCooldown);

        _canAttack.Value = true;
    }

    [ClientRpc]
    private void PlayAttackAnimator_ClientRpc(ulong playerNetworkObjectId)
    {
        // SpawnManager.SpawnedObjects는 서버·클라이언트 모두에서 사용 가능
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
            Debug.Log("7777");
            playerAnimator.SetTrigger("MeleeAttack");
        }
        else
        {
            Debug.Log($"{ownerClientId} player Animator is null");
        }
    }

    public void EnableAttackCollider()//휘두르는 프레임 시작 시 이벤트 호출
    {
        if (IsServer && attackCollider != null)
            attackCollider.enabled = true;
    }

    public void DisableAttackCollider()//휘두르는 프레임 종료 시 이벤트 호출
    {
        if (IsServer && attackCollider != null)
            attackCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out Combat targetCombat))
        {
            // 자신을 때리는 것 방지
            if (other.TryGetComponent(out NetworkObject targetNetObj))
            {
                if (targetNetObj.OwnerClientId == OwnerClientId) return;
                if (hitThisSwing.Contains(targetNetObj.OwnerClientId)) return;
                hitThisSwing.Add(targetNetObj.OwnerClientId);
            }

            // 소유자(공격자)의 Combat 컴포넌트 찾기
            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(OwnerClientId, out var client))
            {
                if (client.PlayerObject.TryGetComponent(out Combat myCombat))
                {
                    targetCombat.TakeDamage(myCombat, damage);
                }
            }
        }
    }
}