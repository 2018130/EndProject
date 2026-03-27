using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Bootstrapper
{
    private const string BootstrapSceneName = "BootstrapScene";

    [RuntimeInitializeOnLoadMethod]
    public static void LoadBootstrapScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        SceneManager.LoadScene(BootstrapSceneName);
        SceneManager.LoadScene(currentScene);
    }
}
