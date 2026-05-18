using System.Collections.Generic;
using UnityEngine;

public class ParticlePool : MonoBehaviour
{
    [SerializeField] private PooledParticle prefab;
    [SerializeField] private int initialSize = 20;

    private readonly List<PooledParticle> _pool = new List<PooledParticle>();

    private void Awake()
    {
        for (int i = 0; i < initialSize; i++)
        {
            CreateParticle();
        }
    }

    private PooledParticle CreateParticle()
    {
        // 생성 시 이 풀(Manager 하위)의 자식으로 생성
        PooledParticle particle = Instantiate(prefab, transform);
        particle.gameObject.SetActive(false);
        _pool.Add(particle);
        return particle;
    }

    //  월드 좌표에 고정 재생
    public void Play(Vector3 position)
    {
        PooledParticle particle = GetAvailable();
        particle.transform.SetParent(null); // 부모 연결 해제
        particle.Play(position);
    }

    // 타겟의 자식으로 들어가서 재생 (킬로그/감정표현용)
    public void Play(Transform target, Vector3 offset)
    {
        PooledParticle particle = GetAvailable();

        // 타겟(플레이어)의 자식으로 설정하여 움직임을 따라가게 함
        particle.transform.SetParent(target);

        // 타겟 위치 + 오프셋 좌표 전달
        particle.Play(target.position + offset);
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