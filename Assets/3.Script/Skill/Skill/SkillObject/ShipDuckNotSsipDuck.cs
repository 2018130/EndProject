using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class ShipDuckNotSsipDuck : NetworkBehaviour
{
    [SerializeField] GameObject[] seats;
    private PlayerNetwork[] passengers;
    [SerializeField] private float knockbackPower = 3f;
    [SerializeField] private float bumperPower = 10f;

    private PlayerNetwork driver;
    [SerializeField] GameObject driverPos;
    private Rigidbody rb;
    private Vector2 moveInput;

    private int seatNumb = 0;

    private float moveSpeed;
    private float duration;

    private NetworkVariable<NetworkObjectReference> driverRef =
        new NetworkVariable<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkList<NetworkObjectReference> passengerRefs;

    private void Awake()
    {
        passengerRefs = new NetworkList<NetworkObjectReference>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    public override void OnNetworkSpawn()
    {
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
    }

    public override void OnNetworkDespawn()
    {
        driverRef.OnValueChanged -= OnDriverChanged;
        if (passengerRefs != null) passengerRefs.OnListChanged -= OnpassengerChanged;

        //if (driver != null)
        //{
        //    SetupDriverPhysics(driver, false);
        //}

        //터져서 모든 승객들 날라감
        //if (IsServer) KickAllPassengers();
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
        //this.driver = driver;

        if (IsServer)
        {
            //driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;
            ////driver.GetComponent<Collider>().isTrigger = true;
            //SetPlayerTrigger_ClientRpc(true);

            this.driver = driver;

            driverRef.Value = new NetworkObjectReference(driver.NetworkObject);
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;

            StartCoroutine(StopSkill());
        }

    }

    private void FixedUpdate()
    {
        if (!IsOwner || driver == null) return;

        moveInput = driver.netMoveInput.Value;
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        if (move.magnitude > 0.01f)
        {
            //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 0.15f);

            Quaternion targetRot = Quaternion.LookRotation(move);
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRot, 0.2f));
        }

        //animator.SetBool("IsMoving", move != Vector3.zero);
    }

    private void LateUpdate()
    {
        if (driver != null)
        {
            driver.transform.position = driverPos.transform.position;
        }

        for (int i = 0; i < seatNumb; i++)
        {
            if (passengers[i] != null)
            {
                passengers[i].transform.position = seats[i].transform.position;
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
                    passenger.SetPassengerMode_ClientRpc(true); // 공격/스킬 막기
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
        int currentCount = seatNumb;
        seatNumb = 0;

        if (driver != null)
        {
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.Alive;
            driver.ApplyKnockback_ClientRpc(Vector3.zero);

            driver = null;
        }

        for (int i = 0; i < currentCount; i++)
        {
            if (passengers[i] == null) continue;

            Vector3 flyingDir = (passengers[i].transform.position - transform.position).normalized;
            flyingDir.y = 1f;

            passengers[i].GetComponent<PlayerHealth>().State.Value = PlayerState.Alive;
            passengers[i].SetPassengerMode_ClientRpc(false); // 공격/스킬 다시 허용
            passengers[i].ApplyKnockback_ClientRpc(flyingDir * knockbackPower);

            passengers[i] = null;
        }

        if (IsServer) passengerRefs.Clear();
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
        Collider[] boatCols = GetComponentsInChildren<Collider>();
        foreach(Collider col in boatCols)
        {
            col.enabled = false;
        }
    }

    private IEnumerator StopSkill()
    {
        yield return new WaitForSeconds(duration);
        if (IsServer)
        {
            DisableBoat_ClientRpc();

            yield return new WaitForSeconds(0.05f);

            KickAllPassengers();

            yield return new WaitForSeconds(0.1f);

            GetComponent<NetworkObject>().Despawn();
        }
    }
}
