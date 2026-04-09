using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GameMode
{
    TeamBattle,  // 팀전 - 아군/적군 구분
    Solo,        // 개인전 - 모두 적
    Mafia        // 마피아 - 팀 모름, 선택 가능
}

[Serializable]
public class PlayerData
{
    public string Nickname = "";
}

public struct PlayerData_s : INetworkSerializable, IEquatable<PlayerData_s>
{
    public FixedString32Bytes Nickname;

    public PlayerData_s(PlayerData playerData)
    {
        Nickname = playerData.Nickname;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Nickname);
    }

    public bool Equals(PlayerData_s other)
    {
        return Nickname == other.Nickname;
    }
}

public class GameManager : SingletonBehaviour<GameManager>, INetworkContextListener
{
    public SceneContext SceneContext { get; set; } = null;
    public GameMode CurrentGameMode { get; set; } = GameMode.TeamBattle;

    [SerializeField]
    private PlayerData playerData = new PlayerData();
    public PlayerData PlayerData => playerData;

    public void OnNetworkSceneContextBuilt()
    {
        Debug.Log("OnNetworkSceneContextBuilt 호출됨");
        if (NetworkManager.Singleton.IsServer)
        {
            // TODO : 나중에 게임 씬 진입시 호출되도록 변경
            //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
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

    public void ExitGame()
    {
        Application.Quit();
    }
}
