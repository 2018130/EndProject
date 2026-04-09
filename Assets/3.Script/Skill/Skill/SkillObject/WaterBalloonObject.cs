using Unity.Netcode;
using UnityEngine;

public class WaterBalloonObject : NetworkBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int circleSegments = 36;
    private float range;
    private float damage;
    private ulong ownerClientId;
    private Faction ownerFaction;

    public void Initialize(float range, float damage, ulong ownerClientId, Faction ownerFaction)
    {
        this.range = range;
        this.damage = damage;
        this.ownerClientId = ownerClientId;
        this.ownerFaction = ownerFaction;
    }

    public override void OnNetworkSpawn()
    {
        lineRenderer.positionCount = circleSegments + 1;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        DrawCircle();
    }

    private void DrawCircle()
    {
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * 2f * Mathf.PI / circleSegments;
            float x = Mathf.Cos(angle) * range;
            float z = Mathf.Sin(angle) * range;
            lineRenderer.SetPosition(i, transform.position + new Vector3(x, 0.1f, z));
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent(out PlayerNetwork player))
        {
            if (player.OwnerClientId == ownerClientId) return;
            Explode();
            return;
        }

        if (other.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            range,
            LayerMask.GetMask("Player")
        );
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerHealth health))
            {
                NetworkObject no = hit.GetComponent<NetworkObject>();
                if (no != null && no.OwnerClientId == ownerClientId) continue;

                health.TakeDamage(damage, ownerFaction, ownerClientId);
            }
        }
        GetComponent<NetworkObject>().Despawn();
    }

    public void Throw(Vector3 dir, float speed)
    {
        Debug.Log($"Throw 방향: {dir}, 속도: {speed}");
        GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);
    }
}