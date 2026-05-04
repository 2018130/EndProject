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

    // weaponPivot(PlayerGun)을 따라가기 위한 target
    private Transform _followTarget;

    protected virtual void Awake()
    {
    }

    public override void OnNetworkSpawn()
    {
        OnNetworkSceneContextBuilt();
    }

    public abstract void Attack();

    public void OnNetworkSceneContextBuilt()
    {
        weaponData = GameManager.Instance.SceneContext.GameDataManager.GetWeaponData(id);
    }

    // WeaponController.RegisterWeapon에서 weaponPivot을 받아 설정
    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
    }

    // NetworkTransform의 위치 동기화보다 LateUpdate가 나중에 실행되므로
    // 매 프레임 weaponPivot 위치/회전을 강제로 덮어씀
    private void LateUpdate()
    {
        if (_followTarget == null) return;

        transform.position = _followTarget.position;
        transform.rotation = _followTarget.rotation;
    }
}
