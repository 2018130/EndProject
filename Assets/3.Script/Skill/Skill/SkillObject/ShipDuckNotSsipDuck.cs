using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class ShipDuckNotSsipDuck : NetworkBehaviour
{
    [SerializeField] GameObject[] seats;
    private PlayerNetwork[] passengers;
    [SerializeField] private float knockbackPower = 3f;
    [SerializeField] private float bumperPower = 10f;
    [Header("Exit Knockback Tuning")]
    [SerializeField] private float driverExitForceMultiplier = 0.15f;
    [SerializeField] private float passengerExitUpFactor = 2.2f;
    [SerializeField] private float passengerExitForceMultiplier = 2.0f;

    private PlayerNetwork driver;
    [SerializeField] GameObject driverPos;
    private Rigidbody rb;
    private Vector2 moveInput;

    private int seatNumb = 0;

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
    private NetworkList<NetworkObjectReference> passengerRefs;

    private bool isEnded = false;
    private bool isShuttingDown = false;

    private void Awake()
    {
        passengerRefs = new NetworkList<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
        SkillEffectPool.Instance.Get(spawnEffectPrefab, transform.position, Quaternion.identity);
        AudioManager.Instance.PlaySFX("start_duck");

        passengers = new PlayerNetwork[seats.Length];
        rb = GetComponent<Rigidbody>();

        driverRef.OnValueChanged += OnDriverChanged;
        if (driverRef.Value.TryGet(out NetworkObject driverObj))
        {
            //SetupDriverPhysics(driverObj.GetComponent<PlayerNetwork>(), true);
            OnDriverChanged(default, driverRef.Value);
        }

        passengerRefs.OnListChanged += OnpassengerChanged;
        foreach (var passengerRef in passengerRefs)
        {
            AddPassengerLocal(passengerRef);
        }

        isMoving.OnValueChanged += OnIsMovingChanged;
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer && !isEnded)
        {
            KickAllPassengers();
        }

        isMoving.OnValueChanged -= OnIsMovingChanged;
        driverRef.OnValueChanged -= OnDriverChanged;
        if (passengerRefs != null) passengerRefs.OnListChanged -= OnpassengerChanged;

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

    private void OnpassengerChanged(NetworkListEvent<NetworkObjectReference> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<NetworkObjectReference>.EventType.Add)
        {
            //if (changeEvent.Value.TryGet(out NetworkObject obj))
            //{
            //    PlayerNetwork p = obj.GetComponent<PlayerNetwork>();
            //    passengers[seatNumb] = p;

            //    p.GetComponent<Collider>().isTrigger = true;
            //}

            AddPassengerLocal(changeEvent.Value);
        }
    }

    private void AddPassengerLocal(NetworkObjectReference passengerRef)
    {
        if (passengerRef.TryGet(out NetworkObject obj))
        {
            PlayerNetwork p = obj.GetComponent<PlayerNetwork>();
            if (p == null || seatNumb >= seats.Length) return;

            passengers[seatNumb] = p;
            p.GetComponent<Collider>().isTrigger = true;

            seatNumb++;
        }
    }

    private void OnDriverChanged(NetworkObjectReference previous, NetworkObjectReference current)
    {
        if (current.TryGet(out NetworkObject driverNetObj))
        {
            //SetupDriverPhysics(driverNetObj.GetComponent<PlayerNetwork>(), true);

            driver = driverNetObj.GetComponent<PlayerNetwork>();
            driver.GetComponent<Collider>().isTrigger = true;
        }
        else
        {
            driver = null;
        }
    }

    //private void SetupDriverPhysics(PlayerNetwork targetDriver, bool isInside)
    //{
    //    if (targetDriver == null) return;

    //    this.driver = targetDriver;
    //    targetDriver.GetComponent<Collider>().isTrigger = isInside;

    //    Rigidbody pRb = targetDriver.GetComponent<Rigidbody>();
    //    if (pRb != null)
    //    {
    //        //pRb.isKinematic = isInside;
    //        pRb.linearVelocity = Vector3.zero;
    //    }
    //}

    public void Initialize(float duration, float moveSpeed, PlayerNetwork driver)
    {
        this.duration = duration;
        this.moveSpeed = moveSpeed;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;


        if (IsServer)
        {
            this.driver = driver;
            driverRef.Value = new NetworkObjectReference(driver.NetworkObject);
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;
            driver.SetPassengerMode_ClientRpc(true);
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

        if (!IsServer)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || driver == null) return;

        moveInput = driver.netMoveInput.Value;

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0; camForward.Normalize();
        camRight.y = 0; camRight.Normalize();

        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        if (move.magnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, 0.2f));
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

        if (!IsServer || isEnded || driver == null) return;

        PlayerHealth driverHealth = driver.GetComponent<PlayerHealth>();
        if (driverHealth == null) return;

        //if(driverHealth.State.Value == PlayerState.Dead || driverHealth.State.Value == PlayerState.Down)
        //{
        //    StopAllCoroutines();
        //    StartCoroutine(ForceEndVehicle_Co());
        //}

        if(!isShuttingDown && (driverHealth.State.Value == PlayerState.Dead || driverHealth.State.Value == PlayerState.Down))
        {
            isShuttingDown = true;
            StopAllCoroutines();
            StartCoroutine(ForceEndVehicle_Co());
        }
    }

    private IEnumerator ForceEndVehicle_Co()
    {
        // 전용 서버에서도 탑승 트리거가 다시 작동하지 않도록 로컬에서도 비활성화
        DisableBoatLocal();
        DisableBoat_ClientRpc();
        yield return new WaitForSeconds(0.05f);

        KickAllPassengers();

        yield return new WaitForSeconds(0.1f);

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        PlayDespawnSFX_ClientRpc();
        GetComponent<NetworkObject>().Despawn();
    }

    private void LateUpdate()
    {
        Quaternion vehicleRot = transform.rotation;

        if (driver != null)
        {
            driver.transform.position = driverPos.transform.position;
            driver.transform.rotation = vehicleRot;
        }

        for (int i = 0; i < seatNumb; i++)
        {
            if (passengers[i] != null)
            {
                passengers[i].transform.position = seats[i].transform.position;
                passengers[i].transform.rotation = vehicleRot;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerNetwork passenger = other.GetComponent<PlayerNetwork>();

            if (passenger != null && passenger != driver && seatNumb < seats.Length)
            {
                bool alreadyIn = false;
                for (int i = 0; i < passengerRefs.Count; i++)
                {
                    if (passengerRefs[i].NetworkObjectId == passenger.NetworkObjectId)
                    {
                        alreadyIn = true;
                        break;
                    }
                }

                if (!alreadyIn)
                {
                    passenger.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;
                    passenger.SetPassengerMode_ClientRpc(true);
                    passengerRefs.Add(new NetworkObjectReference(passenger.NetworkObject));
                }
            }
        }
        if (other.GetComponent<SharkTube>() != null || other.GetComponent<ShipDuckNotSsipDuck>() != null)
        {
            ApplyBumperRecoil(other.transform.position);
        }
    }

    private void KickAllPassengers()
    {
        if (isEnded) return;
        isEnded = true;

        int currentCount = seatNumb;
        seatNumb = 0;

        if (driver != null)
        {
            PlayerHealth driverHealth = driver.GetComponent<PlayerHealth>();

            if (driverHealth != null)
            {
                // 이동은 PlayerNetwork가 State==OnVehicle이면 막고,
                // 스킬 재사용도 UseSkill_ServerRpc가 OnVehicle이면 막는다.
                // 특정 종료 타이밍에 OnVehicle이 남아버리는 케이스를 차단하기 위해
                // Dead만 제외하고 강제로 Alive로 복구한다.
                if (driverHealth.State.Value != PlayerState.Dead)
                    driverHealth.State.Value = PlayerState.Alive;
            }

            driver.SetPassengerMode_ClientRpc(false);
            driver.ForceExitVehicleState_ClientRpc();
            driver.EnableInputOnLandClientRpc();

            Collider driverCol = driver.GetComponent<Collider>();
            if (driverCol != null) driverCol.isTrigger = false;
            ResetCollider_ClientRpc(driver.NetworkObject);

            // driver는 "제자리에서 천천히 떨어지는" 느낌:
            // 수평 밀림 없이 아주 약한 위쪽 임펄스만 주고, 이후 중력으로 자연스럽게 낙하.
            driver.ApplyKnockback_ClientRpc(Vector3.up * (knockbackPower * driverExitForceMultiplier));
            driver = null;
        }

        for (int i = 0; i < currentCount; i++)
        {
            if (passengers[i] == null) continue;

            passengers[i].GetComponent<Collider>().isTrigger = false;
            ResetCollider_ClientRpc(passengers[i].NetworkObject);

            // passenger는 대각선 위 방향으로 높게 날아가게(상승량↑)
            Vector3 flyingDir = (passengers[i].transform.position - transform.position);
            flyingDir.y = 0f;
            flyingDir = flyingDir.sqrMagnitude < 0.0001f ? transform.forward : flyingDir.normalized;
            flyingDir = (flyingDir + Vector3.up * passengerExitUpFactor).normalized;

            PlayerHealth passengerHealth = passengers[i].GetComponent<PlayerHealth>();

            if (passengerHealth != null && passengerHealth.State.Value != PlayerState.Dead)
            {
                passengerHealth.State.Value = PlayerState.Alive;
            }

            passengers[i].SetPassengerMode_ClientRpc(false);
            passengers[i].ForceExitVehicleState_ClientRpc();
            passengers[i].EnableInputOnLandClientRpc();
            passengers[i].ApplyKnockback_ClientRpc(flyingDir * (knockbackPower * passengerExitForceMultiplier));

            passengers[i] = null;
        }

        if (IsServer)
        {
            driverRef.Value = default;
            passengerRefs.Clear();
        }
    }

    [ClientRpc]
    private void ResetCollider_ClientRpc(NetworkObjectReference playerRef)
    {
        if (playerRef.TryGet(out NetworkObject obj))
            obj.GetComponent<Collider>().isTrigger = false;
    }

    private void ApplyBumperRecoil(Vector3 hitPoint)
    {
        Vector3 recoilDir = (transform.position - hitPoint).normalized;
        recoilDir.y = 0;

        rb.AddForce(recoilDir * bumperPower, ForceMode.Impulse);
    }

    //private void TakePassengers(PlayerNetwork passenger)
    //{
    //    for (int i = 0; i < seatNumb; i++)
    //    {
    //        if (passengers[i] == passenger) return;
    //    }

    //    passengers[seatNumb] = passenger;
    //    passenger.GetComponent<Collider>().isTrigger = true;
    //    passenger.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;

    //    seatNumb++;
    //}

    [ClientRpc]
    private void DisableBoat_ClientRpc()
    {
        DisableBoatLocal();
    }

    private void DisableBoatLocal()
    {
        Collider[] boatCols = GetComponentsInChildren<Collider>();
        foreach (Collider col in boatCols)
        {
            col.enabled = false;
        }
    }

    private IEnumerator StopSkill()
    {
        yield return new WaitForSeconds(duration);
        if (IsServer)
        {
            isShuttingDown = true;

            // 전용 서버에서도 탑승 트리거가 다시 작동하지 않도록 로컬에서도 비활성화
            DisableBoatLocal();
            DisableBoat_ClientRpc();

            yield return new WaitForSeconds(0.05f);

            KickAllPassengers();

            yield return new WaitForSeconds(0.1f);

            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            PlayDespawnSFX_ClientRpc();
            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void PlayDespawnSFX_ClientRpc()
    {
        AudioManager.Instance.PlaySFX("end_duck");
    }
}
