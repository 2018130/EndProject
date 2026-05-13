using System.Collections;
using UnityEngine;

public class PooledParticle : MonoBehaviour
{
    private ParticleSystem[] _particles;

    private Coroutine _routine;

    private void Awake()
    {
        _particles =
            GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play(Vector3 position)
    {
        transform.position = position;

        gameObject.SetActive(true);

        foreach (ParticleSystem ps in _particles)
        {
            ps.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        foreach (ParticleSystem ps in _particles)
        {
            ps.Play();
        }

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(ReturnAfterLifetime());
    }

    private IEnumerator ReturnAfterLifetime()
    {
        float maxLifetime = 0f;

        foreach (ParticleSystem ps in _particles)
        {
            float lifetime =
                ps.main.duration +
                ps.main.startLifetime.constantMax;

            if (lifetime > maxLifetime)
            {
                maxLifetime = lifetime;
            }
        }

        yield return new WaitForSeconds(maxLifetime);

        gameObject.SetActive(false);
    }
}