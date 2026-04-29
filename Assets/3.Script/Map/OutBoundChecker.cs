using System.Collections;
using System.Collections.Generic; // Dictionary를 사용하기 위해 필요합니다.
using UnityEngine;

public class OutBoundChecker : MonoBehaviour
{
    [SerializeField]
    private float damage = 10f;
    [SerializeField]
    private float damageDelay = 3f;

    [SerializeField]
    private GameObject sharkPrefab;

    // 1. 다수의 플레이어 각각의 코루틴을 추적하기 위한 딕셔너리
    private Dictionary<PlayerHealth, Coroutine> attackCoroutines = new Dictionary<PlayerHealth, Coroutine>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            // 2. 이미 이 플레이어에 대한 코루틴이 돌고 있다면 중복 실행하지 않음
            if (attackCoroutines.ContainsKey(player))
            {
                return;
            }//

            // 3. 코루틴을 시작하고, 해당 플레이어를 Key로 삼아 딕셔너리에 저장
            Coroutine newRoutine = StartCoroutine(AttackWait(player));
            attackCoroutines.Add(player, newRoutine);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerHealth player))
        {
            // 4. 영역을 나간 플레이어가 딕셔너리에 있는지 확인
            if (attackCoroutines.ContainsKey(player))
            {
                // 해당 플레이어의 코루틴만 정확히 찾아서 중지
                StopCoroutine(attackCoroutines[player]);

                // 중지했으므로 딕셔너리에서 목록 삭제
                attackCoroutines.Remove(player);
                Debug.Log($"{player.name}가 영역을 벗어나 상어 공격이 취소되었습니다.");
            }
        }
    }

    private IEnumerator AttackWait(PlayerHealth playerHealth)
    {
        float timer = 0f;

        while (timer < damageDelay)
        {
            yield return null;
            timer += Time.deltaTime;
        }

        // 5. 딜레이가 끝났으므로 딕셔너리에서 제거 (목록 정리 차원)
        if (attackCoroutines.ContainsKey(playerHealth))
        {
            attackCoroutines.Remove(playerHealth);
        }

        // 대기 시간이 끝났으니 실제 공격 진행
        StartCoroutine(Attack(playerHealth));
    }

    private IEnumerator Attack(PlayerHealth playerHealth)
    {
        // 6. 멀티플레이 환경에서는 대기 시간 동안 플레이어가 이미 죽거나 연결이 끊겼을 수 있으므로 null 체크 추가
        if (playerHealth == null) yield break;

        Vector3 spawnPos = playerHealth.transform.position;
        spawnPos.y -= 2f;
        GameObject shark = Instantiate(sharkPrefab, spawnPos, Quaternion.identity);

        shark.GetComponent<Rigidbody>().AddForce(Vector3.up * 100f);

        yield return new WaitForSeconds(1f);

        // 데미지를 주기 직전에도 플레이어가 존재하는지 한 번 더 확인하면 아주 안전합니다.
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }
        Destroy(shark);
    }
}