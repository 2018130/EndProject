using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndingController : MonoBehaviour
{
    [SerializeField]
    private List<Transform> playerSpawnPoint = new List<Transform>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.OnEndGame += SetPlayerPosition;
    }

    private void SetPlayerPosition(Faction faction)
    {
        PlayerNetwork[] playerCharacters = FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        int idx = 1;

        foreach(var playerCharacter in playerCharacters)
        {
            if(playerCharacter.GetComponent<PlayerHealth>().PlayerFactionInt.Value == (int)faction)
            {
                if (playerCharacter.IsOwner)
                {
                    playerCharacter.transform.position = playerSpawnPoint[0].position;
                }
                else
                {
                    if(idx >= playerSpawnPoint.Count)
                    {
                        Debug.LogWarning($"배열 범위를 초과했습니다.");
                        return;
                    }

                    playerCharacter.transform.position = playerSpawnPoint[idx++].position;
                }
            }
        }
    }
}
