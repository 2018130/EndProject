using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    TitleScene,
    SignScene,
    LobbyScene,
    RoomScene,
    IngameScene,
}

public class SceneChangeManager : SingletonBehaviour<SceneChangeManager>
{
    private const string LoadingSceneName = "LoadingScene";

    [SerializeField]
    private float minLoadingTime = 3f;

    private void Start()
    {
        // Test line
        SceneContext sceneContext = FindAnyObjectByType<SceneContext>();

        if (sceneContext != null)
        {
            sceneContext.Initialize();
            GameManager.Instance.SceneContext = sceneContext;
            BroadcastingSceneContextBuilt();
        }
        //

        if(NetworkManager.Singleton != null)
        {
            Debug.Log("OnServerStarted 등록함");
            NetworkManager.Singleton.OnClientStarted += BroadcastingNetworkSceneContextBuilt;
            NetworkManager.Singleton.OnServerStarted += BroadcastingNetworkSceneContextBuilt; // 추가
        }
    }

    public void ChangeSceneForSinglePlay(SceneType sceneType)
    {
        StartCoroutine(ChangeSceneForSinglePlay_co(sceneType));
    }

    private IEnumerator ChangeSceneForSinglePlay_co(SceneType sceneType)
    {
        SceneManager.LoadScene(LoadingSceneName);
        AsyncOperation op = SceneManager.LoadSceneAsync((int)sceneType, LoadSceneMode.Additive);
        op.allowSceneActivation = false;
        float timer = 0f;

        while(true)
        {
            timer += Time.deltaTime;

            yield return null;

            // TODO : Loading 씬 연출 추가

            if(op.progress >= 0.9f && timer >= minLoadingTime)
            {
                op.allowSceneActivation = true;
                break;
            }
        }

        SceneContext sceneContext = FindAnyObjectByType<SceneContext>();

        if(sceneContext != null)
        {
            sceneContext.Initialize();
            BroadcastingSceneContextBuilt();
        }

        GameManager.Instance.SceneContext = sceneContext;

        SceneManager.UnloadSceneAsync(LoadingSceneName);
    }

    public void ChangeSceneForMultiPlay(SceneType sceneType)
    {
        string scenePath = SceneUtility.GetScenePathByBuildIndex((int)sceneType);
        string sceneName = Path.GetFileNameWithoutExtension(scenePath);

        Debug.Log($"Change scene name : {sceneName}");

        SceneEventProgressStatus status = NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    

    public void BroadcastingSceneContextBuilt()
    {
        IContextListener[] myInterfaces = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).
                                            OfType<IContextListener>().ToArray();

        foreach (var item in myInterfaces)
        {
            item.OnSceneContextBuilt();
        }
    }

    public void BroadcastingNetworkSceneContextBuilt()
    {
        INetworkContextListener[] myInterfaces = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).
                                            OfType<INetworkContextListener>().ToArray();

        foreach (var item in myInterfaces)
        {
            item.OnNetworkSceneContextBuilt();
        }
        Debug.Log($"Calling network scene conext built");
    }
}
