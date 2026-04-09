using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneContext : MonoBehaviour
{
    // 여기에 다른 플레이어의 정보 및 레벨 정보 저장
    public GameDataManager GameDataManager { get; set; }
    public SpawnAreaManager SpawnAreaManager { get; set; }

    public void Initialize()
    {
        GameDataManager = FindAnyObjectByType<GameDataManager>();
        SpawnAreaManager = FindAnyObjectByType<SpawnAreaManager>();
    }
}
