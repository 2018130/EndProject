using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleSceneUIManager : MonoBehaviour
{
    [SerializeField]
    private Button joinGame_Button;
    [SerializeField]
    private Button setting_Button;
    [SerializeField]
    private Button exit_Button;

    private void Start()
    {
        joinGame_Button.onClick.AddListener(() => SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.RoomScene));
        // TODO : Setting 기능 바인딩
        exit_Button.onClick.AddListener(GameManager.Instance.ExitGame);
    }

    
}
