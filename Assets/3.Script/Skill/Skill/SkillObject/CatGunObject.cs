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

    public override void OnNetworkSpawn()
    {
        lineRenderer.positionCount = circleSegments + 1;
        lineRenderer.loop = true;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        DrawCircle();
    }

    public void Initialize(float duration, float damage)
    {
        this.duration = duration;
        this.damage = damage;
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

            foreach (var hit in hits)
            {
                float dist = Vector3.Distance(transform.position, hit.transform.position);
                if (dist < closestDist && hit.TryGetComponent(out PlayerHealth ph))
                {
                    closestDist = dist;
                    closest = ph;
                }
            }

            if (closest != null)
            {
                // 레이저 이펙트 (나중에 추가)
                closest.TakeDamage(damage);
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
