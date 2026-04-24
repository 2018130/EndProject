using System;
using Unity.Netcode;
using UnityEngine;

public class RangedWeapon : BaseWeapon
{
    [SerializeField]
    private Transform waterSpawnPoint;

    [SerializeField]
    private NetworkObject waterPrefab;

    private DateTime lastShootTime = DateTime.MinValue;
    private PlayerInput playerInput;

    private bool isSubscribed = false;

    [SerializeField]
    private ParticleSystem shotParticle;

    protected override void Awake()
    {
        base.Awake();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void InitializeAfterEquip()
    {
        if (!IsOwner) return;

        playerInput = GetComponentInParent<PlayerInput>();
        Debug.Log($"InitializeAfterEquip 호출됨 - 횟수 확인");

        if (playerInput == null) return;

        if (isSubscribed) return;

        if (playerInput != null)
        {
            playerInput.OnFirePerformed -= Attack;
            playerInput.OnFirePerformed += Attack;
            Debug.Log($"InitializeAfterEquip 호출됨 - 무기: {gameObject.name}");
        }
        
    }

    public void UnsubscribeInput()
    {
        if (playerInput != null)
            playerInput.OnFirePerformed -= Attack;
            isSubscribed = false;

    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        UnsubscribeInput();
    }

    public override void Attack()
    {

        if (DateTime.Now.Subtract(lastShootTime) >= TimeSpan.FromSeconds(1 / weaponData.FireRate))
        {
            AimController aimController = GetComponentInParent<AimController>();
            Vector3 shootDir = transform.forward;

            if(aimController != null)
            {
                shootDir = aimController.GetProjectileDirection(waterSpawnPoint.position);
            }


            Shoot(shootDir);
            lastShootTime = DateTime.Now;
        }
    }

    private void Shoot(Vector3 shootDir)
    {
        Debug.Log("Shoot 호출!");
        Debug.Log($"shootDir: {shootDir}");

        PlayerWater playerWater = GetComponentInParent<PlayerWater>();
        Debug.Log($"PlayerWater 찾음: {playerWater != null}");

        if (playerWater == null || !playerWater.HasWater()) return;

        shotParticle.Play();
        ShootProjectileRpc(NetworkManager.Singleton.LocalClientId, shootDir);
    }

    private Projectile SpawnBullet()
    {
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.GetComponent<Projectile>().
            Initialize(new ProjectileData() { BulletSpeed = weaponData.BulletSpeed, MaxHitCountPerShot = weaponData.MaxHitCountPerShot });

        return projectile.GetComponent<Projectile>();
    }

    [Rpc(SendTo.Server)]
    private void ShootProjectileRpc(ulong shooterClientId, Vector3 shootDir)
    {
        PlayerWater playerWater = NetworkManager.Singleton.ConnectedClients[OwnerClientId]
    .PlayerObject.GetComponent<PlayerWater>();

        if (playerWater == null || !playerWater.UseWaterForShot(weaponData.WaterPerShot)) return;

        // 스폰
        NetworkObject projectile = Instantiate(waterPrefab, waterSpawnPoint.position, waterSpawnPoint.rotation);
        projectile.Spawn();

        Debug.Log($"Projectile 스폰됨 - NetworkObjectId: {projectile.NetworkObjectId}, 위치: {projectile.transform.position}");

        PlayerHealth shooterHealth = NetworkManager.Singleton.ConnectedClients[shooterClientId]
    .PlayerObject.GetComponent<PlayerHealth>();
        Faction ownerFaction = (Faction)shooterHealth.PlayerFactionInt.Value;

        // 초기화
        Projectile spawn = projectile.GetComponent<Projectile>();
        spawn.Initialize(new ProjectileData() { BulletSpeed = weaponData.BulletSpeed, 
            MaxHitCountPerShot = weaponData.MaxHitCountPerShot, 
            OwnerClientId = shooterClientId, 
            OwnerFaction = ownerFaction,
            Damage = weaponData.Damage
        });
        Debug.Log($"Damage 값: {weaponData.Damage}");
        // 발사
        spawn.AddForce(shootDir);
    }
}
