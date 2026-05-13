using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    [SerializeField]
    private PooledParticle prefab;

    [SerializeField]
    private int initialSize = 20;

    private readonly List<PooledParticle> _pool =
        new List<PooledParticle>();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateParticle();
        }
    }

    private PooledParticle CreateParticle()
    {
        PooledParticle particle =
            Instantiate(prefab, transform);

        particle.gameObject.SetActive(false);

        _pool.Add(particle);

        return particle;
    }

    public void Play(Vector3 position)
    {
        PooledParticle particle = GetAvailable();

        particle.Play(position);
    }

    private PooledParticle GetAvailable()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].gameObject.activeSelf)
            {
                return _pool[i];
            }
        }

        return CreateParticle();
    }
}