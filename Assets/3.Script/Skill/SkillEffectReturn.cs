using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillEffectReturn : MonoBehaviour
{
    [SerializeField] private float duration = 1f;
    private float currentTimer;

    private void OnEnable()
    {
        currentTimer = duration;
    }

    private void Update()
    {
        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0f)
        {
            SkillEffectPool.Instance.Return(gameObject);
        }
    }
}
