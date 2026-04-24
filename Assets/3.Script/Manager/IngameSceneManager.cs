using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IngameSceneManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AudioManager.Instance.PlayBGM("BGM01");
    }

}
