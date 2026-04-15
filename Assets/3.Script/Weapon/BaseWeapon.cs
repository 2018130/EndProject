using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour, INetworkContextListener
{
    [SerializeField]
    protected string id;

    [SerializeField]
    protected WeaponData weaponData;

    protected virtual void Awake()
    {
    }

    public override void OnNetworkSpawn()
    {
        // SceneContext가 준비된 후 초기화
        if (GameManager.Instance?.SceneContext?.GameDataManager != null)
            weaponData = GameManager.Instance.SceneContext.GameDataManager.GetWeaponData(id);
    }


    public abstract void Attack();

    public void OnNetworkSceneContextBuilt()
    {
        weaponData = GameManager.Instance.SceneContext.GameDataManager.GetWeaponData(id);
    }
}
