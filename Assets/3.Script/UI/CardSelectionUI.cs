using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CardSelectionUI : MonoBehaviour, IContextListener
{
    [SerializeField] private GameObject panel;
    [SerializeField] private CardSlotUI[] cardSlots = new CardSlotUI[3];
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private float selectionTime = 15f;

    private string[] currentCardIds;
    private Coroutine timerCoroutine;
    private GameDataManager gameDataManager;

    private bool isSelected = false;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void OnSceneContextBuilt()
    {
        Debug.Log($"GameManager.Instance: {GameManager.Instance}");
        Debug.Log($"SceneContext: {GameManager.Instance?.SceneContext}");
        Debug.Log($"GameDataManager: {GameManager.Instance?.SceneContext?.GameDataManager}");

        gameDataManager = GameManager.Instance.SceneContext.GameDataManager;
        gameDataManager.OnCardSelectionRequested += OnCardSelectionRequestedHandler;
    }

    private void OnDestroy()
    {
        if (gameDataManager != null)
            gameDataManager.OnCardSelectionRequested -= OnCardSelectionRequestedHandler;
    }

    private void OnCardSelectionRequestedHandler(string[] cardIds, ulong clientId)
    {
        ShowCards(cardIds);
    }

    public void ShowCards(string[] cardIds)
    {
        isSelected = false;

        currentCardIds = cardIds;
        panel.SetActive(true);

        for (int i = 0; i < cardSlots.Length; i++)
        {
            int index = i;
            string cardId = cardIds[i];
            CardData card = gameDataManager.GetCardData(cardId);
            cardSlots[index].Setup(card, () => OnCardSelected(cardId));
        }

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());
    }

    private IEnumerator TimerRoutine()
    {
        float remaining = selectionTime;
        while (remaining > 0)
        {
            timerText.text = Mathf.CeilToInt(remaining).ToString();
            remaining -= Time.deltaTime;
            yield return null;
        }

        // 시간 초과 시 랜덤 선택
        int randomIndex = UnityEngine.Random.Range(0, currentCardIds.Length);
        OnCardSelected(currentCardIds[randomIndex]);
    }

    private void OnCardSelected(string cardId)
    {
        if (isSelected) return;
        isSelected = true;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        panel.SetActive(false);

        // 서버에 선택 알리기
        gameDataManager.SelectCard_ServerRpc(cardId, ClientIdChecker.OwnedClientId);
    }
}
