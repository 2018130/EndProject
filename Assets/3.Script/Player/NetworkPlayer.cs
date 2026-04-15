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
    }

    [Rpc(SendTo.Server)]
    public void SetPlayerNickname_Rpc(string nickname)
    {
        this.nickname.Value = nickname;
    }
}
