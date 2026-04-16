using System;
using System.Collections;
using System.Collections.Generic;
<<<<<<< Updated upstream
using Unity.Netcode;
=======
>>>>>>> Stashed changes
using UnityEngine;

public class TitleSceneManager : MonoBehaviour
{
<<<<<<< Updated upstream
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

=======
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_SERVER
        SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.RoomScene);
#else
        SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.SignScene);
#endif
    }
>>>>>>> Stashed changes
}
