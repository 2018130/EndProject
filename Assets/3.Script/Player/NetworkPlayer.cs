using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<FixedString32Bytes> nickname;
    public string Nickname => nickname.Value.ToString();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        nickname = new NetworkVariable<FixedString32Bytes>();

        GameManager.Instance.OnEndGame += RestartGameGoToLobby;
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerNickname_Rpc(string nickname)
    {
        this.nickname.Value = nickname;
    }

    private void RestartGameGoToLobby(Faction faction)
    {
        StartCoroutine(RestartGameGoToLobby());
    }

    private IEnumerator RestartGameGoToLobby()
    {
        yield return new WaitForSeconds(5f);

        RestartGameGoToLobby_Rpc();
    }

    [Rpc(SendTo.Server)]
    private void RestartGameGoToLobby_Rpc()
    {
        SceneChangeManager.Instance.ChangeSceneForMultiPlay(SceneType.RoomScene);
    }
}
