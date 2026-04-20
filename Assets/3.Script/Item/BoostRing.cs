using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BoostRingType
{
    Spawn,      // 스폰 포인트 - 항시 유지
    Map,        // 맵 배치 - 항시 유지
    Card        // 설치형 카드 - 4초 후 사라짐
}

public class BoostRing : MonoBehaviour
{
    [SerializeField] private BoostRingType ringType;
    [SerializeField] private float boostAmount = 0.3f;
    [SerializeField] private float boostDuration = 3f;
    [SerializeField] private float lifetime = 4f;      // Card 타입만 사용

    // 흔들림
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;
    private Vector3 originPos;

    private void Start()
    {
        originPos = transform.position;

        if (ringType == BoostRingType.Card)
            StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        // Card 타입만 흔들림
        if (ringType == BoostRingType.Card)
        {
            float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = originPos + Vector3.up * y;
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        player.ApplyBoost(boostAmount, boostDuration);

        if (ringType == BoostRingType.Card)
            Destroy(gameObject);
    }
}
