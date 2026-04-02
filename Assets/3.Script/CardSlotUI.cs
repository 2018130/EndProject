using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [SerializeField] private Image cardIcon;
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    public void Setup(CardData card, Action onSelected)
    {
        cardIcon.sprite = card.CardIcon;
        cardNameText.text = card.CardName;
        descriptionText.text = card.Description;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke());
    }
}