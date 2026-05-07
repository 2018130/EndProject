using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : SingletonBehaviour<ParticleManager>
{
    [SerializeField] private ParticleSystem[] killParticlePrefabs;
    [SerializeField] private ParticleSystem[] winParticlePrefab;
    [SerializeField] private float heightOffset = 3f;

    public void PlayKillParticle(Transform target)
    {
        if (killParticlePrefabs == null || killParticlePrefabs.Length == 0) return;

        ParticleSystem randomPrefab = killParticlePrefabs[UnityEngine.Random.Range(0, killParticlePrefabs.Length)];
        if (randomPrefab == null) return;

        Vector3 pos = target.position + Vector3.up * heightOffset;
        ParticleSystem particle = Instantiate(randomPrefab, pos, Quaternion.identity);
        particle.transform.SetParent(target);
        particle.Play();

        Destroy(particle.gameObject, particle.main.duration);
    }

    public void PlayWinParticle(Transform target)
    {
        if (winParticlePrefab == null || winParticlePrefab.Length == 0) return;

        ParticleSystem randomPrefab = winParticlePrefab[UnityEngine.Random.Range(0, winParticlePrefab.Length)];
        if (randomPrefab == null) return;

        Vector3 pos = target.position + Vector3.up * heightOffset;
        ParticleSystem particle = Instantiate(randomPrefab, pos, Quaternion.identity);
        particle.transform.SetParent(target);
        particle.Play();

        Destroy(particle.gameObject, particle.main.duration);
    }
}
