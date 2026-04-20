using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_SERVER
        SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.RoomScene);
#else
        SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.SignScene);
#endif
    }

}
