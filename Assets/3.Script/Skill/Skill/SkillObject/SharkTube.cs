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


    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();

        Debug.Log("shark spawn");

    }

    public override void OnNetworkDespawn()
    {
        if (driver != null)
        {
            driver.GetComponent<Collider>().isTrigger = false;
        }
    }

    public void Initialize(float duration, float moveSpeed, PlayerNetwork driver)
    {

        Debug.Log("shark spawn");

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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerNetwork passenger = other.GetComponent<PlayerNetwork>();

            if(passenger != null && driver != passenger)
            {
                Vector3 knockbackDir = (passenger.transform.position - transform.position).normalized;
                knockbackDir.y = 0;
                passenger.GetComponent<PlayerNetwork>().ApplyKnockback_ClientRpc(knockbackDir * knockbackPower);
            }
            //playernetwork ÀÇ ApplyKnockback_ClientRpc »ç¿ë
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
        GetComponent<NetworkObject>().Despawn();
    }
}
