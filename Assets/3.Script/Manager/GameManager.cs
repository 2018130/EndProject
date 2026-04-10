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

    private bool isGameRunning = false;

    private int expectedPlayerCount = 2;

    public void AddKill(ulong killerClientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        PlayerHealth health = NetworkManager.Singleton.ConnectedClients[killerClientId]
            .PlayerObject.GetComponent<PlayerHealth>();

        Faction faction = (Faction)health.PlayerFactionInt.Value;

        GameTimerNetwork.Instance.AddKill(faction);
    }

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

         Faction faction = (clientId % 2 == 0) ? Faction.TeamA : Faction.TeamB;
        //Faction faction = Faction.TeamA;
        health.PlayerFactionInt.Value = (int)faction;

        SpawnPlayer(health);

        // 이벤트 구독
        health.OnDead += OnPlayerDead;

        if (NetworkManager.Singleton.ConnectedClients.Count >= expectedPlayerCount)
            OnAllPlayersConnected();

    }

    private void OnAllPlayersConnected()
    {
        Debug.Log("OnAllPlayersConnected 호출됨");
        GameTimerNetwork.Instance.StartGame();

        // 모든 클라이언트한테 카드 선택 UI
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            SceneContext.GameDataManager.StartCardSelectionForClient(client.Key);

            SceneContext.GameDataManager.SpawnWeapon_ServerRpc("01", client.Key);
            SceneContext.GameDataManager.SpawnWeapon_ServerRpc("02", client.Key);
            SceneContext.GameDataManager.SpawnWeapon_ServerRpc("03", client.Key);
        }
    }

    private void SpawnPlayer(PlayerHealth health)
    {
        Faction faction = (Faction)health.PlayerFactionInt.Value;
        Vector3 spawnPos = SceneContext.SpawnAreaManager.GetSpawnPosition(faction);
        health.TeleportToSpawnClientRpc(spawnPos);
        health.DisableInputClientRpc();
    }

    private void OnPlayerDead(PlayerHealth health)
    {
        StartCoroutine(RespawnRoutine(health));
    }

    private IEnumerator RespawnRoutine(PlayerHealth health)
    {
        yield return new WaitForSeconds(5f);
        SpawnPlayer(health);
        health.Respawn();
    }

    public void EndGame()
    {
        Faction winner = GameTimerNetwork.Instance.TeamAKills.Value >=
                         GameTimerNetwork.Instance.TeamBKills.Value
                         ? Faction.TeamA : Faction.TeamB;
    }
}
