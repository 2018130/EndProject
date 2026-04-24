using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CatGunObject : NetworkBehaviour
{
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int circleSegments = 36;

    private float duration;
    private float damage;
    private ulong ownerClientId;
    private Faction ownerFaction;

    public override void OnNetworkSpawn()
    {
        lineRenderer.positionCount = circleSegments + 1;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        DrawCircle();
    }

    public void Initialize(float duration, float damage, ulong ownerClientId, Faction ownerFaction)
    {
        this.duration = duration;
        this.damage = damage;
        this.ownerClientId = ownerClientId;
        this.ownerFaction = ownerFaction;  
        StartCoroutine(LifeRoutine());
        StartCoroutine(FireRoutine());
    }

    private void DrawCircle()
    {
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * 2f * Mathf.PI / circleSegments;
            float x = Mathf.Cos(angle) * detectRange;
            float z = Mathf.Sin(angle) * detectRange;
            lineRenderer.SetPosition(i, transform.position + new Vector3(x, 0.1f, z));
        }
    }

    private IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(duration);
        GetComponent<NetworkObject>().Despawn();
    }

    private IEnumerator FireRoutine()
    {
        while (true)
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                detectRange,
                LayerMask.GetMask("Player")
            );

            PlayerHealth closest = null;
            float closestDist = float.MaxValue;
            Vector3 hitPos = Vector3.zero;

            foreach (var hit in hits)
            {

                if (!hit.TryGetComponent(out PlayerHealth ph)) continue;
                //if (ph.OwnerClientId == ownerClientId) continue;  // 자기 자신 제외
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = ph;
                    hitPos = hit.ClosestPoint(transform.position);
                }
            }

            if (closest != null)
            {
                // 레이저 이펙트 (나중에 추가)
                closest.TakeDamage(damage, ownerFaction, ownerClientId, hitPos, transform.position);
            }

            yield return new WaitForSeconds(fireRate);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
