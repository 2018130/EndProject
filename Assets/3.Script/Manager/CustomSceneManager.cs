using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CustomSceneManager : NetworkBehaviour
{
    public static CustomSceneManager Instance { get; set; }

    [SerializeField]
    private NetworkObject playerPrefab;

    public event Action<string, ulong> OnSceneChanged;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if(NetworkManager.Singleton.IsServer)
        {
           NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        switch (sceneEvent.SceneEventType)
        {
            case SceneEventType.LoadEventCompleted:
                Debug.Log($"Scene loaded {sceneEvent.SceneName}");

                if (sceneEvent.SceneName == "04_IngameScene")
                {
                    foreach (ulong clientId in sceneEvent.ClientsThatCompleted)
                    {
                        // 플레이어 생성 및 위치 잡기 (필요시 위치 수정)
                        NetworkObject player = Instantiate(playerPrefab);

                        // 해당 클라이언트에게 소유권 부여하며 스폰
                        player.SpawnAsPlayerObject(clientId);

                        OnSceneChanged_Rpc(sceneEvent.SceneName, clientId);
                        Debug.Log($"Scene loaded spawned client id : {clientId}");
                    }
                }
                break;
        }
    }

    [Rpc(SendTo.Everyone)]
    private void OnSceneChanged_Rpc(string sceneName, ulong clientId)
    {
        SceneContext sceneContext = FindAnyObjectByType<SceneContext>();
        GameManager.Instance.SceneContext = sceneContext;

        if (sceneContext != null)
        {
            sceneContext.Initialize();
            SceneChangeManager.Instance.BroadcastingSceneContextBuilt();
            SceneChangeManager.Instance.BroadcastingNetworkSceneContextBuilt();
        }

        OnSceneChanged?.Invoke(sceneName, clientId);
    }
}
