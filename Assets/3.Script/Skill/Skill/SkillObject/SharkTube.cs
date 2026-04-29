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

        rb = GetComponent<Rigidbody>();

        driverRef.OnValueChanged += OnDriverChanged;

        if (driverRef.Value.TryGet(out NetworkObject driverNetObj))
        {
            SetupDriverPhysics(driverNetObj.GetComponent<PlayerNetwork>(), true);
        }
    }

    public override void OnNetworkDespawn()
    {
        driverRef.OnValueChanged -= OnDriverChanged;

        if (driver != null)
        {
            SetupDriverPhysics(driver, false);
        }
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

        if (IsServer)
        {
            driverRef.Value = new NetworkObjectReference(driver.NetworkObject);
            driver.GetComponent<PlayerHealth>().State.Value = PlayerState.OnVehicle;

            isMoving.Value = true;
        }

        StartCoroutine(StopSkill());
    }

    private void FixedUpdate()
    {
        //if (driver != null) moveInput = driver.GetMoveInput();
        if (driver != null) moveInput = driver.netMoveInput.Value;

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
