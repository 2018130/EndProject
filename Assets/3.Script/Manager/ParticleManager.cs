using UnityEngine;

public class ParticleManager : SingletonBehaviour<ParticleManager>
{
    [Header("Single Pools")]
    [SerializeField]
    private ParticlePool hitPool;

    [SerializeField]
    private ParticlePool destroyPool;

    [Header("Random Pools")]
    [SerializeField]
    private ParticlePool[] killPools;

    [SerializeField]
    private ParticlePool[] winPools;

    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // Hit
    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式

    public void PlayHit(Vector3 position)
    {
        if (hitPool == null)
            return;

        hitPool.Play(position);
    }

    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // Destroy
    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式

    public void PlayDestroy(Vector3 position)
    {
        if (destroyPool == null)
            return;

        destroyPool.Play(position);
    }

    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // Kill
    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式

    public void PlayKill(Vector3 position)
    {
        if (killPools == null ||
            killPools.Length == 0)
            return;

        ParticlePool pool =
            killPools[
                Random.Range(0, killPools.Length)
            ];

        if (pool == null)
            return;

        pool.Play(position);
    }

    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // Win
    // 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式

    public void PlayWin(Vector3 position)
    {
        if (winPools == null ||
            winPools.Length == 0)
            return;

        ParticlePool pool =
            winPools[
                Random.Range(0, winPools.Length)
            ];

        if (pool == null)
            return;

        pool.Play(position);
    }
}