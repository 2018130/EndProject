using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// 각 BeachObject 프리팹에 부착
// 서버: Rigidbody로 물리 주도
// 클라이언트: Kinematic + 보간으로 시각적 표현
public class BeachObject : MonoBehaviour
{
    [Header("물리 설정")]
    [SerializeField] private float impactForceMultiplier = 3f;
    [SerializeField] private float sleepSpeedThreshold = 0.1f;

    [Header("동기화 설정")]
    [SerializeField] private float interpSpeed = 15f;

    // 서버 전용
    private Rigidbody _rb;
    private float _syncTimer;
    private bool _isSleeping;
    private float _aliveTime;
    private const float SleepCheckDelay = 1.5f;

    // 클라이언트 전용
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private bool _hasTarget;

    // 공용
    private BeachObjectPool _pool;
    private bool _isServer;

    // Pool에서 ID로 오브젝트를 찾기 위한 고정 식별자
    // BeachObjectPool.OnNetworkSpawn에서 생성 순서에 따라 부여
    public int ObjectId { get; set; }
    public bool IsSleeping => _isSleeping;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    // BeachObjectPool.SpawnOne / SpawnObject_ClientRpc에서 호출
    public void Initialize(BeachObjectPool pool, bool isServer)
    {
        _pool = pool;
        _isServer = isServer;
        _rb = GetComponent<Rigidbody>();
        _aliveTime = 0f;

        if (_isServer)
        {
            _rb.isKinematic = false;
            _isSleeping = false;
            _syncTimer = 0f;

            // ★ 한 프레임 뒤에 WakeUp (SetActive 직후 물리 초기화 완료 보장)
            StartCoroutine(WakeUpNextFrame());
        }
        else
        {
            _rb.isKinematic = true;
            _hasTarget = false;
        }
    }

    // ── 클라이언트: 서버로부터 위치/회전 수신 ────────────────────────────────
    // BeachObjectPool.SyncObjectState_ClientRpc / BatchSyncState_ClientRpc에서 호출
    public void ApplyNetworkState(Vector3 pos, Quaternion rot)
    {
        if (_isServer) return;

        _targetPosition = pos;
        _targetRotation = rot;
        _hasTarget = true;
    }

    // ── Update ───────────────────────────────────────────────────────────────
    private void Update()
    {
        // Pool이 아직 네트워크 스폰 안 됐으면 대기
        if (_pool == null || !_pool.IsSpawned) return;

        if (_isServer)
            ServerUpdate();
        else
            ClientUpdate();
    }

    private void ServerUpdate()
    {
        _aliveTime += Time.deltaTime;

        // 스폰 직후 물리가 불안정한 구간에서 Sleep 판정 방지
        if (_aliveTime < SleepCheckDelay) return;

        bool shouldSleep = _rb.linearVelocity.magnitude < sleepSpeedThreshold
                        && _rb.angularVelocity.magnitude < sleepSpeedThreshold;

        if (shouldSleep && !_isSleeping)
        {
            _rb.Sleep();
            _isSleeping = true;

            // Sleep 진입 시 최종 위치를 클라이언트에 강제 전송
            // BatchSyncAll에서 IsSleeping 오브젝트는 제외되므로 여기서 직접 전송
            _pool.BroadcastObjectState(this, transform.position, transform.rotation);
        }
    }

    private void ClientUpdate()
    {
        if (!_hasTarget) return;

        // 서버에서 받은 위치/회전으로 부드럽게 보간
        transform.position = Vector3.Lerp(
            transform.position, _targetPosition, interpSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, _targetRotation, interpSpeed * Time.deltaTime);
    }

    // ── 충돌 감지 (서버 전용) ────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (!_isServer) return;

        // Projectile 충돌 → 진행 방향으로 힘 적용
        if (other.TryGetComponent(out Projectile _))
        {
            Vector3 force = other.transform.forward * impactForceMultiplier;
            force.y = Mathf.Abs(force.y) + 2f; // 약간 위로 튕기게
            WakeAndAddForce(force);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isServer) return;

        // 다른 BeachObject와 충돌 시 Sleep 해제
        if (collision.gameObject.TryGetComponent(out BeachObject _))
            WakeIfSleeping();
    }

    private void WakeAndAddForce(Vector3 force)
    {
        if (_rb.IsSleeping()) _rb.WakeUp();
        _isSleeping = false;
        _rb.AddForce(force, ForceMode.Impulse);
    }

    private void WakeIfSleeping()
    {
        if (!_isSleeping) return;
        _rb.WakeUp();
        _isSleeping = false;
    }

    // ── 풀 반환 ──────────────────────────────────────────────────────────────
    // BeachObjectPool.ReturnToPool / DeactivateObject_ClientRpc에서 호출
    public void ReturnToPool()
    {
        if (_isServer && _rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.Sleep();
        }

        _aliveTime = 0f;
        _hasTarget = false;
        _isSleeping = false;
        gameObject.SetActive(false);
    }
    private IEnumerator WakeUpNextFrame()
    {
        yield return new WaitForFixedUpdate();
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.WakeUp();
            // 아주 작은 힘을 줘서 물리 시뮬레이션 강제 시작
            _rb.AddForce(Vector3.down * 0.01f, ForceMode.Impulse);
        }
    }
}