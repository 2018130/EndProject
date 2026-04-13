using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour, INetworkContextListener
{
    // Test Line
    [SerializeField]
    private List<Button> spawnBtn = new List<Button>();

    public void OnNetworkSceneContextBuilt()
    {
        /*
        for (int i = 0; i < spawnBtn.Count; i++)
        {
            string weaponId = "0" + (i + 1);
            spawnBtn[i].onClick.AddListener(() =>
            {
                GameManager.Instance.SceneContext.GameDataManager.SpawnWeapon_ServerRpc(weaponId, ClientIdChecker.OwnedClientId);
                Debug.Log($"Spawn weapon id : {weaponId} owner client id : {ClientIdChecker.OwnedClientId}");
            });
        }*/
    }
}
