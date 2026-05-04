using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class HealthUI : MonoBehaviour, INetworkContextListener
{
    [SerializeField] private Volume volume;
    private PlayerHealth playerHealth;
    private Vignette vignette;
    private Coroutine blinkCoroutine;
    private ColorAdjustments colorAdjustments;

    private float currentBlinkInterval = -1f;

    public void OnNetworkSceneContextBuilt()
    {
        bool result = volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out colorAdjustments);
        Debug.Log($"Vignette 찾음: {result}, vignette: {vignette}");
    }

    public void SetGrayscale(bool active)
    {
        if (colorAdjustments == null) return;
        colorAdjustments.saturation.value = active ? -100f : 0f;
    }

    public void SetPlayer(PlayerHealth health)
    {
        Debug.Log($"SetPlayer 호출됨: {health}");
        playerHealth = health;
        playerHealth.Hp.OnValueChanged += OnHpChanged;
        playerHealth.State.OnValueChanged += OnStateChanged;
    }

    private void OnHpChanged(float oldVal, float newVal)
    {
        Debug.Log($"OnHpChanged: {oldVal} -> {newVal}");
        if (vignette == null) return;
        if (playerHealth.State.Value != PlayerState.Alive) return;

        float ratio = newVal / 100f;

        // 색상
        if (ratio > 0.5f)
        {
            vignette.intensity.value = 0f;
            StopBlink();
            currentBlinkInterval = -1f;
        }
        else if (ratio > 0.1f)
        {
            vignette.color.value = new Color(1f, 0.5f, 0f);
            if (currentBlinkInterval != 0.6f)
            {
                currentBlinkInterval = 0.6f;
                StartBlink(0.6f);
            }
        }
        else
        {
            vignette.color.value = Color.red;
            if (currentBlinkInterval != 0.2f)
            {
                currentBlinkInterval = 0.2f;
                StartBlink(0.2f);
            }
        }
    }
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (vignette == null) return;

        switch (newState)
        {
            case PlayerState.Alive:
                // HP 기반으로 다시 계산
                StopBlink();
                currentBlinkInterval = -1f;
                OnHpChanged(0, playerHealth.Hp.Value);
                break;

            case PlayerState.Down:
                // 검은색 번쩍
                StopBlink();
                currentBlinkInterval = -1f;
                vignette.color.value = Color.black;
                StartBlink(0.4f);
                currentBlinkInterval = 0.4f;
                break;

            case PlayerState.Dead:
                // 비네트 끄고 흑백
                StopBlink();
                currentBlinkInterval = -1f;
                vignette.intensity.value = 0f;
                PlayerEffectUI.Instance?.SetGrayscale(true);
                break;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Hp.OnValueChanged -= OnHpChanged;
            playerHealth.State.OnValueChanged -= OnStateChanged;
        }
    }

    private void StartBlink(float interval)
    {
        StopBlink();
        blinkCoroutine = StartCoroutine(BlinkRoutine(interval));
    }

    private void StopBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            vignette.intensity.value = 0f;
        }
    }

    private IEnumerator BlinkRoutine(float interval)
    {
        while (true)
        {
            vignette.intensity.value = 0.5f;
            yield return new WaitForSeconds(interval);
            vignette.intensity.value = 0f;
            yield return new WaitForSeconds(interval);
        }
    }
}
