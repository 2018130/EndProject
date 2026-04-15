using System;
using Unity.Netcode;
using UnityEngine;

public class RangedWeapon : BaseWeapon
{
    [SerializeField]
    private Transform waterSpawnPoint;

    [SerializeField]
    private NetworkObject waterPrefab;

    private PlayerInput playerInput;
    private float lastShootTime = -999f;

    protected override void Awake()
    {
        base.Awake();
    }
    public override void OnNetworkSpawn()
    {
        //if (!IsOwner) return;

        //// PlayerInput에서 Fire 이벤트 구독
        //playerInput = GetComponentInParent<PlayerInput>();
        //if (playerInput != null)
        //    playerInput.OnFirePerformed += Attack;
    }

    public void InitializeAfterEquip()
    {
        if (!IsOwner) return;
        Debug.Log($"InitializeAfterEquip 호출 - {gameObject.name}");
        playerInput = GetComponentInParent<PlayerInput>();
        Debug.Log($"InitializeAfterEquip 호출됨 - 횟수 확인");

        if (playerInput != null)
        {
            playerInput.OnFirePerformed -= Attack;
            playerInput.OnFirePerformed += Attack;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (playerInput != null)
            playerInput.OnFirePerformed -= Attack;
    }

    public override void Attack()
    {
        if (Time.time - lastShootTime < 1f / weaponData.FireRate) return;
        lastShootTime = Time.time;
        Shoot(transform.forward);
    }

    private void Shoot(Vector3 shootDir)
    {
        Debug.Log("Shoot 호출!");

        PlayerWater playerWater = GetComponentInParent<PlayerWater>();
        Debug.Log($"PlayerWater 찾음: {playerWater != null}");

        if (playerWater == null || !playerWater.HasWater()) return;

        // 물 있으면 클라이언트 예측 스폰
        // Projectile bullet = SpawnBullet();
        //bullet.AddForce(shootDir);


        ShootProjectileRpc(ClientIdChecker.OwnedClientId, shootDir);
    }

    private Projectile SpawnBullet()
    {
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.GetComponent<Projectile>().
            Initialize(new ProjectileData() { 
                BulletSpeed = weaponData.BulletSpeed,
                MaxHitCountPerShot = weaponData.MaxHitCountPerShot,
                GravityStartDistance = weaponData.GravityStartDistance
            });

        return projectile.GetComponent<Projectile>();
    }

    [Rpc(SendTo.Server)]
    private void ShootProjectileRpc(ulong shooterClientId, Vector3 shootDir)
    {
        Debug.Log($"waterSpawnPoint 위치: {waterSpawnPoint.position}, shootDir: {shootDir}");

        PlayerWater playerWater = NetworkManager.Singleton.ConnectedClients[OwnerClientId]
    .PlayerObject.GetComponent<PlayerWater>();

        if (playerWater == null || !playerWater.UseWaterForShot(weaponData.WaterPerShot)) return;

        // 스폰
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.Spawn();

        Debug.Log($"Projectile 스폰됨 - NetworkObjectId: {projectile.NetworkObjectId}, 위치: {projectile.transform.position}");
        // 총을 쏜 클라이언트는 제외
        //if (!NetworkManager.Singleton.IsHost || shooterClientId != NetworkManager.Singleton.LocalClientId)
        //{
        //    projectile.NetworkHide(shooterClientId);
        //}

        PlayerHealth shooterHealth = NetworkManager.Singleton.ConnectedClients[shooterClientId]
    .PlayerObject.GetComponent<PlayerHealth>();
        Faction ownerFaction = (Faction)shooterHealth.PlayerFactionInt.Value;

        // 초기화
        Projectile spawn = projectile.GetComponent<Projectile>();
        spawn.Initialize(new ProjectileData() {
            BulletSpeed = weaponData.BulletSpeed,
            MaxHitCountPerShot = weaponData.MaxHitCountPerShot,
            OwnerClientId = shooterClientId,
            OwnerFaction = ownerFaction,
            GravityStartDistance = weaponData.GravityStartDistance,
            Damage = weaponData.Damage
        });

        // 발사
        spawn.AddForce(shootDir);
    }
}
