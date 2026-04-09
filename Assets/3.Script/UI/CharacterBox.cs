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

    public void SetNicknameText(string nickname)
    {
        nicknameText.text = nickname;
    }

    public void SetCharacterIcon(Sprite newIcon)
    {
        characterIcon.sprite = newIcon;
    }
}
