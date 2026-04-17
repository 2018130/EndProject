using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NicknameViewer : NetworkBehaviour
{
    [SerializeField]
    private NetworkObject owner;

    [SerializeField]
    private TMP_Text nicknameText;

    [SerializeField]
    private Vector3 offset;

    [SerializeField]
    private Vector3 endOffset;

    private static List<ulong> initalizedClientId;

    private bool isEnding = false;

    private void Awake()
    {
        isEnding = false;
        initalizedClientId = new List<ulong>();
        GameManager.Instance.OnSpawnedPlayerCharacter += InitViewer_Rpc;
        GameManager.Instance.OnEndGame += OnEndingStart;
    }

    private void OnEndingStart(Faction faction)
    {
        if (owner == null)
            return;

        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta =  new Vector2(20f, 8);
        isEnding = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void InitViewer_Rpc(ulong clientId)
    {
        if (owner != null)
            return;

        foreach (ulong visitId in initalizedClientId)
        {
            Debug.Log($"visit : {visitId} {visitId}");
            if (visitId == clientId)
                return;
        }

        foreach (var spawnedObj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            // 해당 오브젝트가 플레이어 오브젝트이고, 주인의 ID가 찾으려는 clientId와 같다면
            if (spawnedObj.IsPlayerObject && spawnedObj.OwnerClientId == clientId)
            {
                owner = spawnedObj;

                foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
                {
                    Debug.Log($"{gameObject} nickname : {player.OwnerClientId} {clientId}");
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
            transform.position = isEnding? owner.transform.position + endOffset : owner.transform.position + offset;
        }  
    }
}
