using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text myTeamKillsText;
    [SerializeField] private TMP_Text enemyTeamKillsText;

    private PlayerHealth myHealth;

    private void Start()
    {
        // TimeRemaining이 변하면 게임 시작된 거니까 그때 초기화
        if (GameTimerNetwork.Instance == null)
        {
            StartCoroutine(WaitForTimerNetwork());
            return;
        }
        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged += OnGameStarted;
    }

    private IEnumerator WaitForTimerNetwork()
    {
        yield return new WaitUntil(() => GameTimerNetwork.Instance != null);
        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged += OnGameStarted;
    }

    private void OnGameStarted(float oldVal, float newVal)
    {
        Debug.Log($"OnGameStarted 호출됨: {oldVal} -> {newVal}");
        // 한 번만 실행
        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged -= OnGameStarted;

        if (NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.IsServer) return;

        PlayerHealth health = NetworkManager.Singleton.LocalClient
            .PlayerObject.GetComponent<PlayerHealth>();
        Debug.Log($"PlayerHealth: {health}");
        Initialize(health);
    }

    // 초기값 세팅
    public void Initialize(PlayerHealth health)
    {
        Debug.Log("GameUI Initialize 호출됨");
        myHealth = health;
        myHealth.PlayerFactionInt.OnValueChanged += OnFactionChanged;

        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged += OnTimerChanged;
        GameTimerNetwork.Instance.TeamAKills.OnValueChanged += (old, newVal) => UpdateKillUI();
        GameTimerNetwork.Instance.TeamBKills.OnValueChanged += (old, newVal) => UpdateKillUI();

        // 초기값 세팅
        float time = GameTimerNetwork.Instance.TimeRemaining.Value;
        int minutes = (int)time / 60;
        int seconds = (int)time % 60;
        timerText.text = string.Format("{0}:{1:D2}", minutes, seconds);

        if (myHealth.PlayerFactionInt.Value != (int)Faction.None)
            UpdateKillUI();
    }



    private void OnFactionChanged(int oldVal, int newVal)
    {
        if (newVal != (int)Faction.None)
            UpdateKillUI();
    }

    private void OnTimerChanged(float oldVal, float newVal)
    {
        int minutes = (int)newVal / 60;
        int seconds = (int)newVal % 60;
        timerText.text = string.Format("{0}:{1:D2}", minutes, seconds);
    }

    private void UpdateKillUI()
    {
        if (myHealth == null) return;

        Faction myFaction = (Faction)myHealth.PlayerFactionInt.Value;

        int myKills = myFaction == Faction.TeamA
            ? GameTimerNetwork.Instance.TeamAKills.Value
            : GameTimerNetwork.Instance.TeamBKills.Value;

        int enemyKills = myFaction == Faction.TeamA
            ? GameTimerNetwork.Instance.TeamBKills.Value
            : GameTimerNetwork.Instance.TeamAKills.Value;

        myTeamKillsText.text = myKills.ToString();
        enemyTeamKillsText.text = enemyKills.ToString();
    }

    private void OnDestroy()
    {
        if (GameTimerNetwork.Instance == null) return;
        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged -= OnTimerChanged;
        GameTimerNetwork.Instance.TimeRemaining.OnValueChanged -= OnGameStarted;

        if (myHealth != null)
            myHealth.PlayerFactionInt.OnValueChanged -= OnFactionChanged;
    }
}
