using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MalangBong : NetworkBehaviour
{
    [SerializeField] private Collider attackCollider;
    [SerializeField] private float attackDuration = 0.3f; // 공격 모션(콜라이더 켜져있는) 시간

    private float damage;
    private float attackCooldown;
    private bool canAttack = true;

    // PlayerNetwork에서 스폰 직후 호출하여 CardData 값 주입
    public void Initialize(float cardDamage, float cardCooldown)
    {
        damage = cardDamage;
        attackCooldown = cardCooldown;

        if (attackCollider != null)
            attackCollider.enabled = false;
    }

    public void RequestAttack()
    {
        if (!IsOwner || !canAttack) return;
        PerformAttack_ServerRpc();
    }

    [ServerRpc]
    private void PerformAttack_ServerRpc()
    {
        if (!canAttack) return;
        StartCoroutine(AttackCoroutine());
    }

    private IEnumerator AttackCoroutine()
    {
        canAttack = false;
        SetColliderEnabled_ClientRpc(true);

        yield return new WaitForSeconds(attackDuration);

        SetColliderEnabled_ClientRpc(false);

        // 전체 쿨타임에서 모션 진행 시간을 뺀 만큼 추가 대기
        float remainingCooldown = attackCooldown - attackDuration;
        if (remainingCooldown > 0)
            yield return new WaitForSeconds(remainingCooldown);

        canAttack = true;
    }

    [ClientRpc]
    private void SetColliderEnabled_ClientRpc(bool isEnabled)
    {
        if (attackCollider != null)
            attackCollider.enabled = isEnabled;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // 데미지 판정은 서버에서만

        if (other.TryGetComponent(out Combat targetCombat))
        {
            // 자신을 때리는 것 방지
            if (other.TryGetComponent(out NetworkObject targetNetObj) && targetNetObj.OwnerClientId == OwnerClientId)
                return;

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