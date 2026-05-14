using UnityEngine;

public class ParticleManager : SingletonBehaviour<ParticleManager>
{
    [Header("Single Pools")]
    [SerializeField]
    private ParticlePool hitPool;

    [SerializeField]
    private ParticlePool destroyPool;

    [Header("Random Pools")]
    [SerializeField] private ParticlePool[] killPools;

    [SerializeField] private ParticlePool[] winPools;

    [Header("Settings")]
    [SerializeField] private float heightOffset = 3f; // 캐릭터 머리 위 높이 보정

    // ─────────────────────────────
    // Hit
    // ─────────────────────────────

    public void PlayHit(Vector3 position)
    {
        if (hitPool == null)
            return;

        hitPool.Play(position);
    }

    // ─────────────────────────────
    // Destroy
    // ─────────────────────────────

    public void PlayDestroy(Vector3 position)
    {
        if (destroyPool == null)
            return;

        destroyPool.Play(position);
    }

    // ─────────────────────────────
    // Kill
    // ─────────────────────────────

    public void PlayKill(Transform target)
    {
        if (target == null || killPools == null || killPools.Length == 0) return;

        ParticlePool pool = killPools[Random.Range(0, killPools.Length)];

        if (pool != null)
        {
            pool.Play(target, Vector3.up * heightOffset);
        }
    }

    // ─────────────────────────────
    // Win
    // ─────────────────────────────

    public void PlayWin(Transform target)
    {
        if (target == null || winPools == null || winPools.Length == 0) return;

        ParticlePool pool = winPools[Random.Range(0, winPools.Length)];

        if (pool != null)
        {
            // 수정된 Pool의 Play를 호출 (대상과 오프셋 전달)
            pool.Play(target, Vector3.up * heightOffset);
        }
    }
}