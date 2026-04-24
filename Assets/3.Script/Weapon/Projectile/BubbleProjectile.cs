using Unity.Netcode;
using UnityEngine;

public class BubbleProjectile : NetworkBehaviour
{
    private Rigidbody rb;
    private float speed;
    private ulong shooterClientId;
    private Vector3 shootDir;

    [SerializeField] private GameObject effectPrefab;

    private void Awake()
    {
        TryGetComponent(out rb);
    }

    public void Initialize(float speed, ulong shooterClientId, Vector3 dir)
    {
        this.speed = speed;
        this.shooterClientId = shooterClientId;
        rb.AddForce(dir * speed, ForceMode.Impulse);
    }


    public override void OnNetworkSpawn()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.TryGetComponent(out PlayerNetwork player))
        {
            if (player.OwnerClientId == shooterClientId) return;
            SkillEffectPool.Instance.Get(effectPrefab, transform.position, Quaternion.identity);
            player.ApplyBubbleEffect_ClientRpc(3.5f);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}