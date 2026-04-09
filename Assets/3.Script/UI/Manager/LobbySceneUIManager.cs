using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneUIManager : MonoBehaviour
{
    public static LobbySceneUIManager Instance { get; set; }

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Chat"), Space]
    [SerializeField]
    private ScrollRect contents_SR;
    [SerializeField]
    private TMP_Text contents_Text;
    [SerializeField]
    private TMP_InputField text_InputField;
    [SerializeField]
    private Button send_Button;
    [SerializeField]
    private float textSizeHeight = 10f;

    [Header("Users")]
    [SerializeField]
    private List<CharacterBox> lobbyCharacterBoxes = new List<CharacterBox>();

    [Header("Etc..."), Space]
    [SerializeField]
    private Button gameStart_Button;

    private void Start()
    {
        send_Button.onClick.AddListener(() => {
            LobbySceneManager.Instance.SendChatMessage(text_InputField.text);
            text_InputField.text = "";
            });

        gameStart_Button.onClick.AddListener(() => LobbySceneManager.Instance.GoToInGameScene());

        LobbySceneManager.Instance.OnChatTextReceived += SetChatTextUI;
    }

    private void SetChatTextUI(string text)
    {
        contents_Text.text += text;
        RectTransform chatContentsRect = contents_Text.transform.parent.GetComponent<RectTransform>();
        chatContentsRect.sizeDelta = new Vector2(chatContentsRect.sizeDelta.x, chatContentsRect.sizeDelta.y + textSizeHeight);
    }

    public void SetCharacterBox(int idx, string nickname, Sprite icon = null)
    {
        if(lobbyCharacterBoxes.Count <= idx)
        {
            Debug.LogWarning($"Out of range character box list");
            return;
        }

        lobbyCharacterBoxes[idx].SetNicknameText(nickname);

        if(icon != null)
        {
            lobbyCharacterBoxes[idx].SetCharacterIcon(icon);
        }
    }
}
