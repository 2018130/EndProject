using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneInteraction : MonoBehaviour
{
    private PlayerHealth ownerHealth;
    private PlayerHealth interactor;

    private void OnEnable()
    {
        ownerHealth = GetComponentInParent<PlayerHealth>();
        Debug.Log($"ownerHealth 찾음: {ownerHealth != null}");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerNetwork player)) return;
        if (!player.IsOwner) return; // 로컬 플레이어만

        interactor = other.GetComponent<PlayerHealth>();
        player.SetCurrentZone(this); // 플레이어에게 현재 Zone 알려주기
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PlayerNetwork player)) return;
        if (!player.IsOwner) return;

        interactor = null;
        player.ClearCurrentZone(this);
    }

    public void TryRevive()
    {
        Debug.Log($"TryRevive 호출 interactor:{interactor != null} state:{ownerHealth?.State.Value}");
        if (interactor == null) return;
        if (ownerHealth.State.Value != PlayerState.Down) return;

        GameMode mode = GameManager.Instance.CurrentGameMode;
        Debug.Log($"GameMode: {mode}");

        Debug.Log($"TryRevive 호출 - interactor: {interactor != null}, ownerState: {ownerHealth?.State.Value}");
        if (interactor == null) return;

        switch (mode)
        {
            case GameMode.TeamBattle:
                // 아군만 살리기 가능
                if ((Faction)interactor.PlayerFactionInt.Value !=
                    (Faction)ownerHealth.PlayerFactionInt.Value) return;
                Debug.Log($"팀 체크 - interactor faction: {(Faction)interactor.PlayerFactionInt.Value}, owner faction: {(Faction)ownerHealth.PlayerFactionInt.Value}");
                break;

            case GameMode.Solo:
                // 개인전은 살리기 불가
                return;

            case GameMode.Mafia:
                // 팀 상관없이 살리기 가능
                break;
        }

        Debug.Log("ReviveAlly_ServerRpc 호출");
        interactor.GetComponent<PlayerNetwork>()
            .ReviveAlly_ServerRpc(ownerHealth.OwnerClientId);
    }

    public void TryExecute()
    {
        if (interactor == null) return;
        if (ownerHealth.State.Value != PlayerState.Down) return;

        GameMode mode = GameManager.Instance.CurrentGameMode;

        switch (mode)
        {
            case GameMode.TeamBattle:
                // 적군만 처형 가능
                if ((Faction)interactor.PlayerFactionInt.Value ==
                    (Faction)ownerHealth.PlayerFactionInt.Value) return;
                break;

            case GameMode.Solo:
                // 개인전은 모두 처형 가능
                break;

            case GameMode.Mafia:
                // 팀 상관없이 처형 가능
                break;
        }

        interactor.GetComponent<PlayerNetwork>()
            .ExecuteEnemy_ServerRpc(ownerHealth.OwnerClientId);
    }
}