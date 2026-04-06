using System.Collections;
using Unity.Netcode;
using UnityEngine;


public class GoatMilkDispenser : NetworkBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private int segments = 128;
    [SerializeField] private float Width = 0.1f;
    [SerializeField] private float healInterval = 1f;

    private float healAmount;
    private float healRange;
    private float duration;

    public override void OnNetworkSpawn()
    {
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = Width;
        lineRenderer.endWidth = Width;
    }

    public void Initialize(float duration, float damage, float range)
    {
        this.duration = duration;
        this.healAmount = damage;
        this.healRange = range;

        DrawCircle();
        StartCoroutine(BrightenEverySecond());

        if (IsServer) StartCoroutine(GoatHealPerSecond());
    }

    private void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = i * 2f * Mathf.PI / segments;
            float x = Mathf.Cos(angle) * healRange;
            float z = Mathf.Sin(angle) * healRange;
            lineRenderer.SetPosition(i, transform.position + new Vector3(x, 0.1f, z));
        }
    }

    private IEnumerator BrightenEverySecond()
    {
        while (true)
        {
            lineRenderer.startColor = lineRenderer.endColor = Color.white * 2f;

            yield return new WaitForSeconds(0.1f);

            lineRenderer.startColor = lineRenderer.endColor = Color.white;

            yield return new WaitForSeconds(healInterval);
        }
    }

    private IEnumerator GoatHealPerSecond()
    {
        float timer = 0;

        while (timer < duration)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, healRange);
            foreach (Collider col in cols)
            {
                if (col.CompareTag("Player"))
                {
                    PlayerHealth player = col.GetComponent<PlayerHealth>();
                    if (player != null)
                    {
                        float targetHP = player.Hp.Value + healAmount;
                        player.Hp.Value = Mathf.Min(targetHP, player.maxHp);
                    }
                }
            }

            timer += healInterval;
            yield return new WaitForSeconds(healInterval);
        }

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
