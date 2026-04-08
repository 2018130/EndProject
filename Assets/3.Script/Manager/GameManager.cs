using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum GameMode
{
    TeamBattle,  // 팀전 - 아군/적군 구분
    Solo,        // 개인전 - 모두 적
    Mafia        // 마피아 - 팀 모름, 선택 가능
}

public class GameManager : SingletonBehaviour<GameManager>, INetworkContextListener
{
    public SceneContext SceneContext { get; set; } = null;
    public GameMode CurrentGameMode { get; set; } = GameMode.TeamBattle;

    public void OnNetworkSceneContextBuilt()
    {
        Debug.Log("OnNetworkSceneContextBuilt 호출됨");
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        PlayerHealth health = NetworkManager.Singleton.ConnectedClients[clientId]
        .PlayerObject.GetComponent<PlayerHealth>();

        // Faction faction = (clientId % 2 == 0) ? Faction.TeamA : Faction.TeamB;
        Faction faction = Faction.TeamA;
        health.PlayerFactionInt.Value = (int)faction;

        //StartGame();
        SceneContext.GameDataManager.StartCardSelectionForClient(clientId);
    }

    public void StartGame()
    {
        Debug.Log($"IsServer: {NetworkManager.Singleton.IsServer}");
        if (!NetworkManager.Singleton.IsServer) return;
        Debug.Log("StartCardSelection 호출");
        SceneContext.GameDataManager.StartCardSelection();
    }
}
