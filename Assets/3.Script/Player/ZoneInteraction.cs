using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneInteraction : MonoBehaviour
{
    private PlayerHealth ownerHealth;
    private PlayerHealth interactor;

    [SerializeField] private SpriteRenderer circleZone;

    private Color allyColor = new Color(0f, 1f, 0f, 0.4f);   // 초록 - 살리기
    private Color enemyColor = new Color(1f, 0f, 0f, 0.4f);  // 빨강 - 처형
    private Color defaultColor = new Color(0f, 0.8f, 1f, 0.4f); // 하늘색 - 기본

    private void OnEnable()
    {
        ownerHealth = GetComponentInParent<PlayerHealth>();
        Debug.Log($"ownerHealth 찾음: {ownerHealth != null}");
        circleZone?.gameObject.SetActive(true);
        UpdateCircleZoneColor();
    }

    private void OnDisable()
    {
        circleZone?.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerNetwork player)) return;
        if (!player.IsOwner) return; // 로컬 플레이어만

        interactor = other.GetComponent<PlayerHealth>();
        player.SetCurrentZone(this); // 플레이어에게 현재 Zone 알려주기
        UpdateCircleZoneColor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out PlayerNetwork player)) return;
        if (!player.IsOwner) return;

        interactor = null;
        player.ClearCurrentZone(this);
        UpdateCircleZoneColor();
    }

    private void UpdateCircleZoneColor()
    {
        if (circleZone == null) return;
        if (interactor == null)
        {
            circleZone.color = defaultColor;
            return;
        }

        GameMode mode = GameManager.Instance.CurrentGameMode;
        if (mode == GameMode.Mafia)
        {
            // 마피아 모드는 팀 모르니까 기본색
            circleZone.color = defaultColor;
            return;
        }

        bool isAlly = (Faction)interactor.PlayerFactionInt.Value ==
                      (Faction)ownerHealth.PlayerFactionInt.Value;

        circleZone.color = isAlly ? allyColor : enemyColor;
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