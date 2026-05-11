using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerState
{
    Alive,     // 살아있음 0
    Down,      // 기절     1
    Dead,       // 처형당함 2
    OnVehicle,
}

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] public float maxHp = 100f;    // 기본 체력
    [SerializeField] private float downedHpDrain = 10f; // 기절 중 초당 체력 감소
    [SerializeField] private GameObject actionZone;     // 처형, 살리기 존
    [SerializeField] private ParticleSystem hitParticlePrefab;

    private Coroutine downedCoroutine;

    public event Action<PlayerHealth> OnDead;
    public ulong LastDownedByClientId { get; private set; }

    public NetworkVariable<float> Hp = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<PlayerState> State = new NetworkVariable<PlayerState>(
        PlayerState.Alive,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> PlayerFactionInt = new NetworkVariable<int>(
    (int)Faction.None,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
    private void Awake()
    {
        GameManager.Instance.OnSpawnedPlayerCharacter += InitializeOnSpawned;
    }

    public override void OnNetworkSpawn()
    {
        Hp.OnValueChanged += onHPChanged;
        State.OnValueChanged += OnStateChanged;

        if (IsOwner)
        {
            HealthUI healthUI = FindAnyObjectByType<HealthUI>();
            healthUI?.SetPlayer(this);
        }

    }

    public void InitializeOnSpawned(ulong clientId)
    {
        Faction faction = (Faction)PlayerFactionInt.Value;
        Vector3 spawnPos = GameManager.Instance.SceneContext.SpawnAreaManager.GetSpawnPosition(faction);
        Debug.Log($"PlayerHealth initialzed {clientId} faction : {faction} spawnPos : {spawnPos}");
        TeleportToSpawnClientRpc(spawnPos);
        DisableInputClientRpc();
    }

    private void onHPChanged(float oldVal, float newVal)
    {
        // HP UI 업데이트
    }

    private void OnStateChanged(PlayerState oldState, PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Down:
                // 이동속도 감소, 공격 불가 처리
                Debug.Log("죽음");
                actionZone.SetActive(true);
                if (IsServer)
                    downedCoroutine = StartCoroutine(DownedDrainRoutine());
                break;
            case PlayerState.Alive:
                actionZone.SetActive(false);
                EnableInputClientRpc();
                if (downedCoroutine != null)
                {
                    StopCoroutine(downedCoroutine);
                    downedCoroutine = null;
                }
                break;
            case PlayerState.Dead:
                Debug.Log($"Dead 상태 진입 - IsServer: {IsServer}");
                actionZone.SetActive(false);
                DisableInputClientRpc();
                if (downedCoroutine != null)
                {
                    StopCoroutine(downedCoroutine);
                    downedCoroutine = null;
                }
                if (IsServer)
                {
                    Debug.Log("OnPlayerDead 호출 시도");
                    OnPlayerDead();
                }
                break;
            case PlayerState.OnVehicle:
                // 이동, 공격 불가 처리
                // PlayerNetwork의 IsGrounded 체크 후 state = alive로 변경
                DisableInputClientRpc();
                if (IsOwner)
                {
                    StartCoroutine(CheckVehicleGroundedRoutine());
                }
                break;
        }
    }

    private IEnumerator CheckVehicleGroundedRoutine()
    {
        PlayerNetwork net = GetComponent<PlayerNetwork>();

        // 튜브 파괴 혹은 넉백 등의 외부 요인으로 OnVehicle 상태가 풀리기를 대기 (탈것에 타고 있는 동안 무한 대기)
        yield return new WaitWhile(() => State.Value == PlayerState.OnVehicle);

        // 허공에 뜬 상태에서 땅에 닿을 때까지 대기
        yield return new WaitUntil(() => net.IsGrounded());

        // 착지가 확인되면 서버에 Alive 상태로 전환 요청
        RequestSetAliveServerRpc();
    }

    [ServerRpc]
    private void RequestSetAliveServerRpc()
    {
        if (State.Value != PlayerState.Down && State.Value != PlayerState.Dead)
        {
            State.Value = PlayerState.Alive;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnHitParticle_Rpc(Vector3 hitPos, Vector3 bulletPos)
    {
        Vector3 hitDir = (hitPos - bulletPos).normalized;
        Quaternion particleRot = Quaternion.LookRotation(-hitDir);
        ParticleSystem hitParticle = Instantiate(hitParticlePrefab, hitPos, particleRot);
        hitParticle.Play();

        StartCoroutine(RemoveHitParticle(1f, hitParticle));
    }

    public void TakeDamage(float damage, Faction attackerFaction = Faction.None, ulong attackerClientId = ulong.MaxValue, Vector3 hitPos = default, Vector3 bulletPos = default)
    {
        if (!IsServer) return;

        Debug.Log($"TakeDamage - damage:{damage}, attackerFaction:{attackerFaction}, attackerClientId:{attackerClientId}, myFaction:{(Faction)PlayerFactionInt.Value}, State:{State.Value}, Hp:{Hp.Value}");

        if (attackerFaction != Faction.None &&
            attackerFaction == (Faction)PlayerFactionInt.Value) return;

        SpawnHitParticle_Rpc(hitPos, bulletPos);

        Hp.Value -= damage;

        if (Hp.Value <= 0 && State.Value == PlayerState.Alive)
        {
            Hp.Value = maxHp; // 기절하고 HP 100
            State.Value = PlayerState.Down; // 기절 상태로 변환

            Faction myFaction = (Faction)PlayerFactionInt.Value;
            if (attackerFaction != Faction.None && attackerFaction != myFaction)
                LastDownedByClientId = attackerClientId;
            else
                LastDownedByClientId = ulong.MaxValue;
        }
    }

    private IEnumerator DownedDrainRoutine()
    {
        while (State.Value == PlayerState.Down)
        {
            Hp.Value -= downedHpDrain * Time.deltaTime;
            if (Hp.Value <= 0)
            {
                Hp.Value = 0;
                State.Value = PlayerState.Dead;
                yield break;
            }
            yield return null;
        }
    }

    // 아군이 부활시킬 때 호출
    public void Revive()
    {
        if (!IsServer) return;
        if (State.Value != PlayerState.Down) return;
        Debug.Log("살렸다.");
        Hp.Value = 50f;
        State.Value = PlayerState.Alive;
    }

    public void Kill()
    {
        if (!IsServer) return;
        if (State.Value != PlayerState.Down) return;
        State.Value = PlayerState.Dead;
    }
    private void OnPlayerDead()
    {
        if (!IsServer) return;

        Debug.Log($"OnPlayerDead 호출됨 - LastDownedByClientId: {LastDownedByClientId}");

        if (LastDownedByClientId != ulong.MaxValue)
            GameManager.Instance.AddKill(LastDownedByClientId);

        OnDead?.Invoke(this);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // 잠깐 대기 후 리스폰 (연출용)
        yield return new WaitForSeconds(5f);
        Respawn();
    }

    public void Respawn()
    {
        if (!IsServer) return;
        if (State.Value != PlayerState.Dead) return; // Dead일 때만

        Faction faction = (Faction)PlayerFactionInt.Value;
        Vector3 spawnPos = GameManager.Instance.SceneContext
            .SpawnAreaManager.GetSpawnPosition(faction);

        Hp.Value = maxHp;
        State.Value = PlayerState.Alive;

        TeleportToSpawnClientRpc(spawnPos);
    }

    [ClientRpc]
    public void TeleportToSpawnClientRpc(Vector3 spawnPos)
    {
        Debug.Log($"Spawn : {gameObject}");
        transform.position = spawnPos;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    public void DisableInputClientRpc()
    {
        if (!IsOwner) return;
        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = false;
    }

    [ClientRpc]
    public void EnableInputClientRpc()
    {
        if (!IsOwner) return;
        var playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
            playerInput.enabled = true;

    }

    private IEnumerator RemoveHitParticle(float aliveTime, ParticleSystem particle)
    {
        yield return new WaitForSeconds(aliveTime);

        Destroy(particle.gameObject);
    }
}
