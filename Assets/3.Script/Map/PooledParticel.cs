using System.Collections;
using UnityEngine;

public class PooledParticle : MonoBehaviour
{
    private ParticleSystem[] _particles;
    private Coroutine _routine;
    private Transform _originalParent; // 원래 풀의 위치를 기억

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(true);
        _originalParent = transform.parent; // 최초 생성 시 부모(ParticlePool) 저장
    }

    public void Play(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);

        foreach (ParticleSystem ps in _particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ReturnAfterLifetime());
    }

    private IEnumerator ReturnAfterLifetime()
    {
        float maxLifetime = 0f;
        foreach (ParticleSystem ps in _particles)
        {
            float lifetime = ps.main.duration + ps.main.startLifetime.constantMax;
            if (lifetime > maxLifetime) maxLifetime = lifetime;
        }

        yield return new WaitForSeconds(maxLifetime);

        transform.SetParent(_originalParent);
        gameObject.SetActive(false);
    }
}