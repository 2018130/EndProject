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

    public void OnNetworkSceneContextBuilt()
    {
        bool result = volume.profile.TryGet(out vignette);
        Debug.Log($"Vignette Ã£À½: {result}, vignette: {vignette}");
    }

    public void SetPlayer(PlayerHealth health)
    {
        Debug.Log($"SetPlayer È£ÃâµÊ: {health}");
        playerHealth = health;
        playerHealth.Hp.OnValueChanged += OnHpChanged;
        playerHealth.State.OnValueChanged += OnStateChanged;
    }

    private void OnHpChanged(float oldVal, float newVal)
    {
        Debug.Log($"OnHpChanged: {oldVal} -> {newVal}");
        if (vignette == null) return;
        if (playerHealth.State.Value == PlayerState.Down) return;

        float ratio = newVal / 100f;

        // »ö»ó
        if (ratio > 0.5f)
        {
            vignette.intensity.value = 0f;
            StopBlink();
        }
        else if (ratio > 0.1f)
        {
            vignette.color.value = new Color(1f, 0.5f, 0f);
            StartBlink(0.6f); // 
        }
        else
        {
            vignette.color.value = Color.red;
            StartBlink(0.2f);
        }
    }
    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        if (vignette == null) return;

        if (newState == PlayerState.Down)
        {
            StopBlink();
            vignette.color.value = Color.black;
            StartBlink(0.4f); // È¸»öÀ¸·Î ±ôºý
        }
        else if (newState == PlayerState.Alive)
        {
            StopBlink();
            OnHpChanged(0, playerHealth.Hp.Value);
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
