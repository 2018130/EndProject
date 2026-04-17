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
    public NetworkList<PlayerData_s> UserDatas => userDatas;

    [SerializeField]
    private NetworkObject networkPlayer;

    [SerializeField]
    private List<CharacterData> characterDatas = new List<CharacterData>();

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
        Debug.Log($"Player data : {playerData.CharacterID} {playerData.ClientId} {playerData.Nickname}");
        userDatas.Add(playerData);
    }

    private void OnUserDataChanged(NetworkListEvent<PlayerData_s> playerData)
    {
        int idx = playerData.Index;
        string nickname = playerData.Value.Nickname.ToString();
        Sprite icon = GetCharacterIcon(playerData.Value.CharacterID.ToString());

        LobbySceneUIManager.Instance.SetCharacterBox(idx, nickname, icon);
        LobbySceneUIManager.Instance.SetReadyStateButtonActive(idx, playerData.Value.IsReady);

        Debug.Log($"Change userdata idx : {idx} nickname : {nickname} ready state : {userDatas[playerData.Index].IsReady}");
        /*
        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            Debug.Log(player.OwnerClientId + " " + playerData.Value.ClientId);
            if(player.OwnerClientId == playerData.Value.ClientId)
            {
                player.SetPlayerNickname_Rpc(playerData.Value.Nickname.ToString());
            }
        }
        */
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
            GameManager.Instance.PlayerData.ClientID = NetworkManager.Singleton.LocalClientId;
            // 각 클라이언트 개인 초기화
            Debug.Log(GameManager.Instance.PlayerData.Nickname);
            PlayerData_s playerData = new PlayerData_s(GameManager.Instance.PlayerData, false, "01");
            SendPlayerData_Rpc(playerData);
            RefreshUserUI();
        }

        userDatas.OnListChanged += OnUserDataChanged;

    }

    // 서버에서 실행되어 클라이언트에게 할당
    private void SpawnClient(ulong clientId)
    {
        Debug.Log($"Spawn Network Player, id : {clientId}");
        NetworkObject spawn = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(networkPlayer, clientId);
        int clientCount = NetworkManager.Singleton.ConnectedClientsIds.Count;

        RpcParams rpcParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
            }
        };

        foreach (var player in userDatas)
        {
            if (player.ClientId == clientId)
            {
                spawn.GetComponent<NetworkPlayer>().SetPlayerNickname_Rpc(player.Nickname.ToString());
                Debug.Log($"client id : {clientId} {player.Nickname.ToString()}");
            }
        }

        SetClientRoomAuthority_Rpc(clientCount - 1, clientCount == 1 ? true : false, rpcParams);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SetClientRoomAuthority_Rpc(int idx, bool isRoomManager, RpcParams rpcParams = default)
    {
        Debug.Log($"Set client room authority");
        LobbySceneUIManager.Instance.SetRoomManageState(idx, isRoomManager);
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

    [Rpc(SendTo.Server)]
    public void GoToInGameScene_Rpc()
    {
        for(int i = 1; i < userDatas.Count; i++)
        {
            if(!userDatas[i].IsReady)
            {
                Debug.Log($"Failed to go inGmaeScene");
                return;
            }
        }

        Debug.Log($"Go to ingame scene");
        SceneChangeManager.Instance.ChangeSceneForMultiPlay(SceneType.IngameScene);
    }

    private void RefreshUserUI()
    {
        for(int i = 0; i < userDatas.Count; i++)
        {
            if (userDatas[i].Nickname == GameManager.Instance.PlayerData.Nickname)
                return;

            LobbySceneUIManager.Instance.SetCharacterBox(i, userDatas[i].Nickname.ToString(), characterDatas[i].Icon);
        }
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyState_ServerRpc(int idx)
    {
        if(userDatas.Count <= idx)
        {
            Debug.LogWarning($"Out of bound in userdata");
            return;
        }

        PlayerData_s currentData = userDatas[idx];

        userDatas[idx] = new PlayerData_s
        {
            Nickname = currentData.Nickname,
            IsReady = !currentData.IsReady
        };

        Debug.Log($"change ready state idx {idx} ready State : {userDatas[idx].IsReady}");
    }

    public int GetIdxFromNickname(string nickname)
    {
        for(int i = 0; i < userDatas.Count; i++)
        {
            if (nickname == userDatas[i].Nickname)
                return i;
        }

        return -1;
    }



    public Sprite GetCharacterIcon(string id)
    {
        foreach(var characterData in characterDatas)
        {
            if (characterData.ID == id)
                return characterData.Icon;
        }

        return null;
    }

    [Rpc(SendTo.Server)]
    public void SetCharacterIcon_ServerRpc(string userNickname, bool isPre)
    {
        int idx = GetIdxFromNickname(userNickname);
        if (userDatas.Count <= idx || idx == -1)
        {
            Debug.LogWarning($"Out of bound in userdata");
            return;
        }

        string characterId = userDatas[idx].CharacterID.ToString();
        CharacterData characterData = isPre ? GetPreCharacterData(characterId) : GetPostCharacterData(characterId);

        if(characterData.ID == default)
        {
            Debug.LogWarning($"Failed to get characterData");
            return;
        }

        userDatas[idx] = new PlayerData_s(OwnerClientId, userDatas[idx].Nickname.ToString(), userDatas[idx].IsReady, characterData.ID);
    }

    public CharacterData GetPreCharacterData(string id)
    {
        for (int i = 0; i < characterDatas.Count; i++)
        {
            if (characterDatas[i].ID == id)
            {
                if (i == 0)
                    return characterDatas[characterDatas.Count - 1];
                else
                    return characterDatas[i - 1];
            }
                
        }

        return default;
    }

    public CharacterData GetPostCharacterData(string id)
    {
        for (int i = 0; i < characterDatas.Count; i++)
        {
            if (characterDatas[i].ID == id)
            {
                if (i == characterDatas.Count - 1)
                    return characterDatas[0];
                else
                    return characterDatas[i + 1];
            }

        }

        return default;
    }
}
