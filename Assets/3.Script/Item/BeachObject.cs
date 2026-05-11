using System.Collections;
using UnityEngine;

// 각 BeachObject 프리팹에 부착
// 서버/클라이언트 판단을 _pool.IsServer로 직접 참조
// → Initialize의 파라미터 전달 실수로 인한 오동작 원천 차단
public class BeachObject : MonoBehaviour
{
    [Header("물리 설정")]
    [SerializeField] private float impactForceMultiplier = 3f;
    [SerializeField] private float sleepSpeedThreshold = 0.1f;

    [Header("동기화 설정")]
    [SerializeField] private float interpSpeed = 15f;
    [SerializeField] private float teleportThreshold = 3f;

    // 서버 전용
    private Rigidbody _rb;
    private bool _isSleeping;
    private float _aliveTime;
    private const float SleepCheckDelay = 1.5f;

    // 클라이언트 전용
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private bool _hasTarget;

    // 공용
    private BeachObjectPool _pool;

    // BeachObjectPool이 생성 순서에 따라 부여하는 고정 식별자
    public int ObjectId { get; set; }
    public bool IsSleeping => _isSleeping;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    // isServer 파라미터 없이 _pool.IsServer로 직접 판단
    public void Initialize(BeachObjectPool pool)
    {
        _pool = pool;
        _rb = GetComponent<Rigidbody>();
        _aliveTime = 0f;

        if (_pool.IsServer)
        {
            _rb.isKinematic = false;
            _isSleeping = false;
            StartCoroutine(WakeUpNextFrame());
        }
        else
        {
            _rb.isKinematic = true;
            _hasTarget = false;
        }

        Debug.Log($"[BeachObject] {gameObject.name} Id:{ObjectId} IsServer:{_pool.IsServer}");
    }

    // ── 클라이언트: 위치/회전 수신 ───────────────────────────────────────────
    public void ApplyNetworkState(Vector3 pos, Quaternion rot)
    {
        if (_pool != null && _pool.IsServer) return;

        _targetPosition = pos;
        _targetRotation = rot;
        _hasTarget = true;
    }

    // ── Update ───────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_pool == null || !_pool.IsSpawned) return;

        if (_pool.IsServer)
            ServerUpdate();
        else
            ClientUpdate();
    }

    private void ServerUpdate()
    {
        _aliveTime += Time.deltaTime;

        // 스폰 직후 물리 불안정 구간에서 Sleep 판정 방지
        if (_aliveTime < SleepCheckDelay) return;

        bool shouldSleep = _rb.linearVelocity.magnitude < sleepSpeedThreshold
                        && _rb.angularVelocity.magnitude < sleepSpeedThreshold;

        if (shouldSleep && !_isSleeping)
        {
            _rb.Sleep();
            _isSleeping = true;

            // BatchSyncAll에서 제외되므로 마지막 위치 강제 전송
            _pool.BroadcastObjectState(this, transform.position, transform.rotation);
        }
    }

    private void ClientUpdate()
    {
        if (!_hasTarget) return;

        float dist = Vector3.Distance(transform.position, _targetPosition);

        if (dist > teleportThreshold)
        {
            // 거리가 너무 멀면 즉시 이동 (보간 지연 방지)
            transform.position = _targetPosition;
            transform.rotation = _targetRotation;
        }
        else
        {
            transform.position = Vector3.Lerp(
                transform.position, _targetPosition, interpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, _targetRotation, interpSpeed * Time.deltaTime);
        }
    }

    // ── 충돌 감지 ────────────────────────────────────────────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (_pool == null || !_pool.IsServer) return;

        if (other.TryGetComponent(out Projectile _))
        {
            Vector3 force = other.transform.forward * impactForceMultiplier;
            force.y = Mathf.Abs(force.y) + 2f;
            WakeAndAddForce(force);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_pool == null || !_pool.IsServer) return;

        if (collision.gameObject.TryGetComponent(out BeachObject _))
            WakeIfSleeping();
    }

    private void WakeAndAddForce(Vector3 force)
    {
        if (_rb.IsSleeping()) _rb.WakeUp();

        bool wasSleeping = _isSleeping;
        _isSleeping = false;
        _rb.AddForce(force, ForceMode.Impulse);

        // Sleep 상태에서 깨어난 경우 즉시 동기화
        if (wasSleeping)
            _pool.BroadcastObjectState(this, transform.position, transform.rotation);
    }

    private void WakeIfSleeping()
    {
        if (!_isSleeping) return;

        _rb.WakeUp();
        _isSleeping = false;
        _pool.BroadcastObjectState(this, transform.position, transform.rotation);
    }

    // ── 풀 반환 ──────────────────────────────────────────────────────────────
    public void ReturnToPool()
    {
        if (_pool != null && _pool.IsServer && _rb != null)
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

    // SetActive 직후 물리 초기화 완료 후 WakeUp
    private IEnumerator WakeUpNextFrame()
    {
        yield return new WaitForFixedUpdate();
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.WakeUp();
            _rb.AddForce(Vector3.down * 0.01f, ForceMode.Impulse);
        }
    }
}