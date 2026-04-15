using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NicknameViewer : NetworkBehaviour
{
    NetworkObject owner;

    [SerializeField]
    private TMP_Text nicknameText;

    private static List<ulong> initalizedClientId = new List<ulong>();
    private void Awake()
    {
        GameManager.Instance.OnSpawnedPlayerCharacter += InitViewer_Rpc;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitViewer_Rpc(ulong clientId)
    {
        foreach (ulong visitId in initalizedClientId)
        {
            Debug.Log($"visit : {visitId}");
            if (visitId == clientId)
                return;
        }

        foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            // 해당 오브젝트가 플레이어 오브젝트이고, 주인의 ID가 찾으려는 clientId와 같다면
            if (spawnedObj.IsPlayerObject && spawnedObj.OwnerClientId == clientId)
            {
                owner = spawnedObj;
                Debug.Log(clientId + " " + owner.OwnerClientId);

                foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                {
                    if (player.OwnerClientId == clientId)
                    {
                        nicknameText.text = player.Nickname;
                        initalizedClientId.Add(clientId);
                        break;
                    }
                }
                break; // 찾았으니 반복문 종료
            }
        }
    }

    private void LateUpdate()
    {
        if(owner != null)
        {
            transform.position = owner.transform.position;
        }  
    }
}
