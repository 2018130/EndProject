using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour
{
    [SerializeField]
    protected string id;

    [SerializeField]
    protected WeaponData weaponData;

    protected virtual void Awake()
    {
        weaponData = GameManager.Instance.SceneContext.GameDataManager.GetWeaponData(id);
    }

    public abstract void Attack();
}
