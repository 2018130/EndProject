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
    private int seatNumb;

    private float moveSpeed;
    private float duration;


    public override void OnNetworkSpawn()
    {
        seatNumb = 0;
        passengers = new PlayerNetwork[seats.Length];
        rb = GetComponent<Rigidbody>();

        Debug.Log("duck spawn");
    }

    public override void OnNetworkDespawn()
    {
        if (driver != null)
        {
            driver.GetComponent<Collider>().isTrigger = false;
        }

        //터져서 모든 승객들 날라감
        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i] == null) continue;

            Vector3 flyingDir = (passengers[i].transform.position - transform.position).normalized;
            passengers[i].ApplyKnockback_ClientRpc(flyingDir * knockbackPower);

            passengers[i].GetComponent<Collider>().isTrigger = false;
            passengers[i] = null;
        }
    }

    public void Initialize(float duration, float moveSpeed, PlayerNetwork driver)
    {
        Debug.Log("duck initialize");

        this.duration = duration;
        this.moveSpeed = moveSpeed;
        this.driver = driver;

        if (IsServer)
        {
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;
            driver.GetComponent<Collider>().isTrigger = true;
        }

        StartCoroutine(StopSkill());
    }

    private void FixedUpdate()
    {
        if (driver != null) moveInput = driver.GetMoveInput();

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        rb.MovePosition(rb.position + move * moveSpeed * Time.fixedDeltaTime);

        if (move.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(move),
                0.15f
            );

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
                TakePassengers(passenger);
            }
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

    private void TakePassengers(PlayerNetwork passenger)
    {
        for (int i = 0; i < seatNumb; i++)
        {
            if (passengers[i] == passenger) return;
        }

        passengers[seatNumb] = passenger;
        passenger.GetComponent<Collider>().isTrigger = true;
        passenger.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;

        seatNumb++;
    }

    private IEnumerator StopSkill()
    {
        yield return new WaitForSeconds(duration);
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
