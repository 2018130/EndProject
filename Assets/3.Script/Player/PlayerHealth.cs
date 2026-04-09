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

    private Coroutine downedCoroutine;

    // 추가
    public event Action<PlayerHealth> OnDead;

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
                if (downedCoroutine != null)
                {
                    StopCoroutine(downedCoroutine);
                    downedCoroutine = null;
                }
                break;
            case PlayerState.Dead:
                // 부활 처리
                actionZone.SetActive(false);
                if (downedCoroutine != null)
                {
                    StopCoroutine(downedCoroutine);
                    downedCoroutine = null;
                }
                if (IsServer)
                    OnPlayerDead();
                break;
            case PlayerState.OnVehicle:
                // 이동, 공격 불가 처리
                // PlayerNetwork의 IsGrounded 체크 후 state = alive로 변경
                break;
        }
    }

    public void TakeDamage(float damage, Faction attackerFaction = Faction.None)
    {
        if (!IsServer) return;

        if (attackerFaction != Faction.None &&
            attackerFaction == (Faction)PlayerFactionInt.Value) return;


        Hp.Value -= damage;

        if (Hp.Value <= 0 && State.Value == PlayerState.Alive)
        {
            Hp.Value = maxHp; // 기절하고 HP 100
            State.Value = PlayerState.Down; // 기절 상태로 변환
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
        OnDead?.Invoke(this);
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
        Hp.Value = maxHp;
        State.Value = PlayerState.Alive;
    }

    [ClientRpc]
    public void TeleportToSpawnClientRpc(Vector3 spawnPos)
    {
        transform.position = spawnPos;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = true;

        var playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = true;

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
        }
    }
}
