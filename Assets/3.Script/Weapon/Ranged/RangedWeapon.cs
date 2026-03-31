using System;
using Unity.Netcode;
using UnityEngine;

public class RangedWeapon : BaseWeapon
{
    [SerializeField]
    private Transform waterSpawnPoint;

    [SerializeField]
    private NetworkObject waterPrefab;

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

        if(InputManager.Instance.OnClicked)
        {
            Attack();
        }
    }

    public override void Attack()
    {
        if(DateTime.Now.Subtract(lastShootTime) >= TimeSpan.FromSeconds(1 / weaponData.FireRate))
        {
            Shoot(transform.forward);
            lastShootTime = DateTime.Now;
        }
    }

    private void Shoot(Vector3 shootDir)
    {
        Projectile bullet = SpawnBullet();

        bullet.AddForce(shootDir);
    }

    private Projectile SpawnBullet()
    {
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.GetComponent<Projectile>().
            Initialize(new ProjectileData() { BulletSpeed = weaponData.BulletSpeed, MaxHitCountPerShot = weaponData.MaxHitCountPerShot });

        ShootProjectileRpc(ClientIdChecker.OwnedClientId);

        return projectile.GetComponent<Projectile>();
    }

    [Rpc(SendTo.Server)]
    private void ShootProjectileRpc(ulong shooterClientId)
    {
        // 스폰
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.Spawn();
        // 총을 쏜 클라이언트는 제외
        projectile.NetworkHide(shooterClientId);

        // 초기화
        Projectile spawn = projectile.GetComponent<Projectile>();
        spawn.Initialize(new ProjectileData() { BulletSpeed = weaponData.BulletSpeed, MaxHitCountPerShot = weaponData.MaxHitCountPerShot, OwnerClientId = shooterClientId });

        // 발사
        Vector3 shootDir = transform.forward;
        spawn.AddForce(shootDir);
    }
}
