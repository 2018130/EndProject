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

    [SerializeField] private ParticleSystem healEffect;

    private GameObject spawnedEffect;

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

        healEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        StartCoroutine(PlayEffectNextFrame());

        if (IsServer)
        {
            ApplyVisuals(range);
            StartCoroutine(GoatHealPerSecond());
            StartCoroutine(SetupVisualsDelay(range));
        }
    }

    private void ApplyVisuals(float range)
    {
        this.healRange = range;
        DrawCircle();
        StartCoroutine(BrightenEverySecond());
    }

    private IEnumerator PlayEffectNextFrame()
    {
        yield return null;
        healEffect.Play(true);
    }


    private IEnumerator SetupVisualsDelay(float range)
    {
        yield return null;
        SetupVisuals_ClientRpc(range);
    }

    [ClientRpc]
    private void SetupVisuals_ClientRpc(float range)
    {
        if (IsServer) return;

        StartCoroutine(PlayEffectNextFrame());
        ApplyVisuals(range);
        AudioManager.Instance.PlaySFX("GoatSkillStart");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayHealSound_Rpc()
    {
        AudioManager.Instance.PlaySFX("GoatHeal");
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
            lineRenderer.startColor = lineRenderer.endColor = Color.white;

            yield return new WaitForSeconds(0.1f);

            lineRenderer.startColor = lineRenderer.endColor = Color.coral;

            yield return new WaitForSeconds(healInterval);
        }
    }

    [ClientRpc]
    private void StopEffect_ClientRpc()
    {
        if (IsServer) return;
        healEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
                        PlayHealSound_Rpc();
                    }
                }
            }

            timer += healInterval;
            yield return new WaitForSeconds(healInterval);
        }

        Debug.Log("»˙ ¡æ∑· - ¿Ã∆Â∆Æ π›»Ø");
        if (spawnedEffect != null)
            SkillEffectPool.Instance.Return(spawnedEffect);
            StopEffect_ClientRpc();

        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
