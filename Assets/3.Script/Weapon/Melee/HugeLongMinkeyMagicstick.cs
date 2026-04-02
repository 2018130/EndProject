using System;
using Unity.Netcode;
using UnityEngine;

public class HugeLongMinkeyMagicstick : BaseWeapon
{
    private NetworkObject networkObject;
    private DateTime lastShootTime = DateTime.MinValue;

    protected override void Awake()
    {
        base.Awake();

        networkObject = GetComponent<NetworkObject>();
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner)
            return;

        if (InputManager.Instance.OnClicked)
        {
            Attack();
        }
    }

    public override void Attack()
    {
        if (DateTime.Now.Subtract(lastShootTime) >= TimeSpan.FromSeconds(1 / weaponData.FireRate))
        {
            //Combat.takedamage

            lastShootTime = DateTime.Now;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

    }
}
