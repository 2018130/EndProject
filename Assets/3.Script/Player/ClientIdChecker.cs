using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ClientIdChecker : MonoBehaviour
{
    public static ulong OwnedClientId { get; set; }


    private void Start()
    {
        NetworkObject NO = GetComponent<NetworkObject>();
        if(NO.IsOwner)
        {
            OwnedClientId = NO.OwnerClientId;
        }
    }
}
