using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public enum GameMode
{
    TeamBattle,  // 팀전 - 아군/적군 구분
    Solo,        // 개인전 - 모두 적
    Mafia        // 마피아 - 팀 모름, 선택 가능
}

[Serializable]
public class PlayerData
{
    public ulong ClientID;
    public string Nickname = "";
}

public struct PlayerData_s : INetworkSerializable, IEquatable<PlayerData_s>
{
    public ulong ClientId;

    public FixedString32Bytes Nickname;

    public bool IsReady;

    public FixedString32Bytes CharacterID;

    public PlayerData_s(PlayerData playerData, bool isReady, string characterID)
    {
        Nickname = playerData.Nickname;
        ClientId = playerData.ClientID;
        IsReady = isReady;
        CharacterID = characterID;
    }

    public PlayerData_s(ulong clinetId, string nickname, bool isReady, string characterID)
    {
        ClientId = clinetId;
        Nickname = nickname;
        IsReady = isReady;
        CharacterID = characterID;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Nickname);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref CharacterID);
        serializer.SerializeValue(ref ClientId);
    }

    public bool Equals(PlayerData_s other)
    {
        return Nickname == other.Nickname && IsReady == other.IsReady && CharacterID == other.CharacterID && ClientId == other.ClientId;
    }
}

public class GameManager : SingletonBehaviour<GameManager>
{
    public SceneContext SceneContext { get; set; } = null;
    public GameMode CurrentGameMode { get; set; } = GameMode.TeamBattle;

    [SerializeField]
    private PlayerData playerData;
    public PlayerData PlayerData => playerData;

    private bool isGameRunning = false;

    [SerializeField]
    private int expectedPlayerCount = 2;

    private HashSet<ulong> _initializedClients = new HashSet<ulong>();
    public event Action<ulong> OnSpawnedPlayerCharacter;

    private void Start()
    {

        playerData = new PlayerData() { Nickname = UnityEngine.Random.Range(0, 10000).ToString() };

        Instance.OnSpawnedPlayerCharacter -= OnClientConnected;
        Instance.OnSpawnedPlayerCharacter += OnClientConnected;
    }

    public void AddKill(ulong killerClientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        PlayerHealth health = NetworkManager.Singleton.ConnectedClients[killerClientId]
            .PlayerObject.GetComponent<PlayerHealth>();

        Faction faction = (Faction)health.PlayerFactionInt.Value;

        GameTimerNetwork.Instance.AddKill(faction);
    }

    private void OnClientConnected(ulong clientId)
    {
        if(NetworkManager.Singleton.IsServer)
        {

            Debug.Log($"Game manager initialized on server {clientId}");

        if (!NetworkManager.Singleton.IsServer) return;
        if (_initializedClients.Contains(clientId)) return;
        _initializedClients.Add(clientId);

        Debug.Log($"Game manager initialized on server");

            PlayerHealth[] playerHealthes = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
            
            // 2. 해당 클라이언트의 PlayerObject에서 컴포넌트를 바로 가져옵니다.
            PlayerHealth health = null;
            foreach (var p in playerHealthes)
            {
                if (p.GetComponent<NetworkObject>().OwnerClientId == clientId)
                {
                    health = p;
                }
            }

            if (health != null)
            {
                Debug.Log($"[성공] Client {clientId}의 PlayerHealth를 찾았습니다!");

                Faction faction = (clientId % 2 == 0) ? Faction.TeamA : Faction.TeamB;
                //Faction faction = Faction.TeamA;
                health.PlayerFactionInt.Value = (int)faction;

                SpawnPlayer(health);

                // 이벤트 구독
                health.OnDead += OnPlayerDead;


            }

                //if (NetworkManager.Singleton.ConnectedClients.Count >= expectedPlayerCount)
                //OnAllPlayersConnected();

                SpawnWeaponsForClient(clientId);
                SceneContext.GameDataManager.StartCardSelectionForClient(clientId);
                GameTimerNetwork.Instance.StartGame();


        }
    }
    private void SpawnWeaponsForClient(ulong clientId)
    {
        SceneContext.GameDataManager.SpawnWeapon_ServerRpc("01", clientId);
        SceneContext.GameDataManager.SpawnWeapon_ServerRpc("02", clientId);
        SceneContext.GameDataManager.SpawnWeapon_ServerRpc("03", clientId);
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
        if(SceneContext == null)
        {
            SceneContext = FindAnyObjectByType<SceneContext>();
            SceneContext.Initialize();
        }

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

    // 서버에서 실행될 함수
    public void SpawnPlayerCharacter(ulong clientId)
    {
        Debug.Log($"player character가 클라이언트 상 스폰됨을 확인받음 id : {clientId}");
        OnSpawnedPlayerCharacter?.Invoke(clientId);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
