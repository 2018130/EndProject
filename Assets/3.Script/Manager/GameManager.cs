using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : SingletonBehaviour<GameManager>, INetworkContextListener
{
    public SceneContext SceneContext { get; set; } = null;

    public void OnNetworkSceneContextBuilt()
    {
        Debug.Log("OnNetworkSceneContextBuilt »£√‚µ ");
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        //StartGame();
        SceneContext.GameDataManager.StartCardSelectionForClient(clientId);
    }

    public void StartGame()
    {
        Debug.Log($"IsServer: {NetworkManager.Singleton.IsServer}");
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("StartCardSelection »£√‚");
        SceneContext.GameDataManager.StartCardSelection();
    }
}
