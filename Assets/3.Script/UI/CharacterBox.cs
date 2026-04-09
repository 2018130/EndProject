using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBox : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nicknameText;
    [SerializeField]
    private Image characterIcon;
    [SerializeField]
    private TMP_Text readyText;

    public void SetNicknameText(string nickname)
    {
        nicknameText.text = nickname;
    }

    public void SetCharacterIcon(Sprite newIcon)
    {
        characterIcon.sprite = newIcon;
    }

    public void SetActiveReadyText(bool active)
    {
        readyText.gameObject.SetActive(active);
    }
}
