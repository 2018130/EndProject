using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class SingletonBehaviourWithNetwork<T> : NetworkBehaviour where T : NetworkBehaviour
{
    private static T instance;
    public static T Instance => instance;

    protected NetworkObject networkObject;

    public ulong GetOwnerClientId => networkObject.OwnerClientId;

    protected virtual void Awake()
    {
        T typeOfClass = FindAnyObjectByType<T>();

        if (typeOfClass == this || typeOfClass == null)
        {
            Debug.Log($"{typeof(T)} singleton created");
            T targetTypeObj = GetComponent<T>();
            instance = targetTypeObj;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(targetTypeObj.gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }

        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log($"Network client id : {networkObject.OwnerClientId}");
    }
}
