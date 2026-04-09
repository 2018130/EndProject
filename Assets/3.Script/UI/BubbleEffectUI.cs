using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BubbleEffectUI : MonoBehaviour
{
    public static BubbleEffectUI Instance { get; private set; }

    [SerializeField] private Image bubbleImage; // 반투명 버블 이미지
    private Coroutine effectCoroutine;

    private void Awake()
    {
        Instance = this;
        bubbleImage.gameObject.SetActive(false);
    }

    public void Show(float duration)
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);
        effectCoroutine = StartCoroutine(ShowRoutine(duration));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        bubbleImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        bubbleImage.gameObject.SetActive(false);
    }
}