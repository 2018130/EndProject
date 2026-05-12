using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class SharkTube : NetworkBehaviour
{
    [SerializeField] private float knockbackPower = 3f;
    [SerializeField] private float bumperPower = 10f;

    private PlayerNetwork driver;
    [SerializeField] GameObject driverPos;
    private Rigidbody rb;
    private Vector2 moveInput;

    private float moveSpeed;
    private float duration;

    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private float spawnInterval = 0.1f;
    [SerializeField] private Transform effectPos;
    private float effectTimer;

    public NetworkVariable<bool> isMoving = new NetworkVariable<bool>();

    private NetworkVariable<NetworkObjectReference> driverRef =
        new NetworkVariable<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        SkillEffectPool.Instance.Get(spawnEffectPrefab, transform.position, Quaternion.identity);
        AudioManager.Instance.PlaySFX("start_shark");
        rb = GetComponent<Rigidbody>();

        driverRef.OnValueChanged += OnDriverChanged;

        if (driverRef.Value.TryGet(out NetworkObject driverNetObj))
        {
            SetupDriverPhysics(driverNetObj.GetComponent<PlayerNetwork>(), true);
        }

        isMoving.OnValueChanged += OnIsMovingChanged;
    }

    public override void OnNetworkDespawn()
    {
        isMoving.OnValueChanged -= OnIsMovingChanged;
        driverRef.OnValueChanged -= OnDriverChanged;

        if (driver != null)
        {
            Collider col = driver.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;

            driver = null;
        }
    }

    private void OnIsMovingChanged(bool previous, bool current)
    {
        if (current) AudioManager.Instance.PlaySFX("tube", loop: true);
        else AudioManager.Instance.StopSFX();
    }

    private void OnDriverChanged(NetworkObjectReference previous, NetworkObjectReference current)
    {
        if (current.TryGet(out NetworkObject driverNetObj))
        {
            SetupDriverPhysics(driverNetObj.GetComponent<PlayerNetwork>(), true);
        }
    }

    private void SetupDriverPhysics(PlayerNetwork targetDriver, bool isInside)
    {
        if (targetDriver == null) return;

        this.driver = targetDriver;
        targetDriver.GetComponent<Collider>().isTrigger = isInside;

        Rigidbody pRb = targetDriver.GetComponent<Rigidbody>();
        if (pRb != null)
        {
            //pRb.isKinematic = isInside;
            pRb.linearVelocity = Vector3.zero;
        }
    }
    public void Initialize(float duration, float moveSpeed, PlayerNetwork driver)
    {
        this.duration = duration;
        this.moveSpeed = moveSpeed;
        this.driver = driver;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;

        if (IsServer)
        {
            driverRef.Value = new NetworkObjectReference(driver.NetworkObject);
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;

            isMoving.Value = true;

            SyncStats_ClientRpc(duration, moveSpeed);
        }

        StartCoroutine(StopSkill());
    }

    [ClientRpc]
    private void SyncStats_ClientRpc(float duration, float speed)
    {
        this.duration = duration;
        this.moveSpeed = speed;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || driver == null) return; // 오너 체크 필수 추가

        moveInput = driver.netMoveInput.Value;

        // PlayerNetwork와 동일한 카메라 기준 이동 연산
        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0; camForward.Normalize();
        camRight.y = 0; camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 0.15f);
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

    private void LateUpdate()
    {
        if (driver != null)
        {
            driver.transform.position = driverPos.transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerNetwork targetPlayer = other.GetComponent<PlayerNetwork>();

            if(targetPlayer != null && driver != targetPlayer)
            {
                Vector3 knockbackDir = (targetPlayer.transform.position - transform.position).normalized;
                knockbackDir.y = 0;
                targetPlayer.ApplyKnockback_ClientRpc(knockbackDir * knockbackPower);
            }
            //playernetwork 의 ApplyKnockback_ClientRpc 사용
        }
        if (other.GetComponent<SharkTube>() != null || other.GetComponent<ShipDuckNotSsipDuck>() != null)
        {
            ApplyBumperRecoil(other.transform.position);
        }
    }

    private void ApplyBumperRecoil(Vector3 hitPoint)
    {
        Vector3 recoilDir = (transform.position - hitPoint).normalized;
        recoilDir.y = 0;

        rb.AddForce(recoilDir * bumperPower, ForceMode.Impulse);
    }

    private IEnumerator StopSkill()
    {
        yield return new WaitForSeconds(duration);

        if (!IsServer) yield break;

        isMoving.Value = false;
        PlayDespawnSFX_ClientRpc();

        if (driver != null)
        {
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.Alive;

            // 살짝 위로 띄워서 착지 처리
            Vector3 kickDir = driver.transform.up;
            driver.ApplyKnockback_ClientRpc(kickDir * knockbackPower);
        }

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        yield return new WaitForSeconds(0.1f);


        GetComponent<NetworkObject>().Despawn();
    }

    [ClientRpc]
    private void PlayDespawnSFX_ClientRpc()
    {
        AudioManager.Instance.PlaySFX("end_shark");
    }


}
