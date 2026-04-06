using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum PlayerState
{
    Alive,     // 살아있음 0
    Down,      // 기절     1
    Dead       // 처형당함 2
}

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private float maxHp = 100f;    // 기본 체력
    [SerializeField] private float downedHpDrain = 10f; // 기절 중 초당 체력 감소

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
                break;
            case PlayerState.Alive:

                break;
            case PlayerState.Dead:
                // 부활 처리
                break;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        Hp.Value -= damage;

        if (Hp.Value <= 0 && State.Value == PlayerState.Alive)
        {
            Hp.Value = maxHp; // 기절하고 HP 100
            State.Value = PlayerState.Down; // 기절 상태로 변환
        }
    }

    // 아군이 부활시킬 때 호출
    public void Revive()
    {
        if (!IsServer) return;
        if (State.Value != PlayerState.Down) return;

        Hp.Value = maxHp;
        State.Value = PlayerState.Alive;
    }

}
