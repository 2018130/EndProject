using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneType
{
    TitleScene,
    LobbyScene,
    RoomScene,
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
            BroadcastingSceneContextBuilt();
            GameManager.Instance.SceneContext = sceneContext;
        }
        //

        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted += BroadcastingNetworkSceneContextBuilt;
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

            // TODO : Loading ¾À ¿¬Ãâ Ãß°¡

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

    private void BroadcastingSceneContextBuilt()
    {
        IContextListener[] myInterfaces = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).
                                            OfType<IContextListener>().ToArray();

        foreach (var item in myInterfaces)
        {
            item.OnSceneContextBuilt();
        }
    }

    private void BroadcastingNetworkSceneContextBuilt()
    {
        INetworkContextListener[] myInterfaces = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).
                                            OfType<INetworkContextListener>().ToArray();

        foreach (var item in myInterfaces)
        {
            item.OnNetworkSceneContextBuilt();
        }
        Debug.Log($"Calling network scene conext built");
    }
}
