using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class LobbySceneManager : NetworkBehaviour
{
    public static LobbySceneManager Instance { get; set; }


    // 서버에서 관리될 텍스트 목록
    [SerializeField]
    private string chatText = "";
    public event Action<string> OnChatTextReceived;

    // 서버에서 관리될 플레이어 데이터 목록
    [SerializeField]
    private NetworkList<PlayerData_s> userDatas;

    [SerializeField]
    private NetworkObject networkPlayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        userDatas = new NetworkList<PlayerData_s>();
    }

    public void StartClient()
    {
        Debug.Log($"Start client...");
        NetworkManager.Singleton.StartClient();
    }

    [Rpc(SendTo.Server)]
    private void SendPlayerData_Rpc(PlayerData_s playerData)
    {
        userDatas.Add(playerData);
    }

    private void OnUserDataChanged(NetworkListEvent<PlayerData_s> playerData)
    {
        int idx = playerData.Index;
        string nickname = playerData.Value.Nickname.ToString();
        LobbySceneUIManager.Instance.SetCharacterBox(idx, nickname);
    }

    public void StartServer()
    {
        Debug.Log($"Start server...");
        NetworkManager.Singleton.StartServer();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"Network spawned");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += SpawnClient;
        }
        else
        {
            Debug.Log(GameManager.Instance.PlayerData.Nickname);
            PlayerData_s playerData = new PlayerData_s(GameManager.Instance.PlayerData);
            SendPlayerData_Rpc(playerData);
            RefreshUserUI();
        }

        userDatas.OnListChanged += OnUserDataChanged;

    }

    private void SpawnClient(ulong clientId)
    {
        Debug.Log($"Spawn Network Player, id : {clientId}");
        NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(networkPlayer, clientId);
    }


    #region Chatting
    public void SendChatMessage(string text)
    {
        if(text.Length == 0)
        {
            return;
        }

        string nickname = GameManager.Instance.PlayerData.Nickname;

        SetChatMessage_ServerRpc(nickname + " : " + text + "\n");
    }

    [Rpc(SendTo.Server)]
    private void SetChatMessage_ServerRpc(string text)
    {
        chatText += $"{text}";
        BroadcatChatMessage_ClientRpc(text);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcatChatMessage_ClientRpc(string text)
    {
        OnChatTextReceived?.Invoke(text);
        chatText += $"{text}";
    }
    #endregion

    public void GoToInGameScene()
    {
        Debug.Log($"Go to ingame scene");
        SceneChangeManager.Instance.ChangeSceneForMultiPlay(SceneType.GameScene);
    }

    private void RefreshUserUI()
    {
        for(int i = 0; i < userDatas.Count; i++)
        {
            if (userDatas[i].Nickname == GameManager.Instance.PlayerData.Nickname)
                return;

            LobbySceneUIManager.Instance.SetCharacterBox(i, userDatas[i].Nickname.ToString());
        }
    }
}
