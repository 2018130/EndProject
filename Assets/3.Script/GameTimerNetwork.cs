using Unity.Netcode;
using UnityEngine;

public class GameTimerNetwork : NetworkBehaviour
{
    public static GameTimerNetwork Instance { get; private set; }

    public NetworkVariable<float> TimeRemaining = new NetworkVariable<float>(
        300f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> TeamAKills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> TeamBKills = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isGameRunning = false;

    public override void OnNetworkSpawn()
    {
        Instance = this;
    }

    private void Update()
    {
        if (!isGameRunning) return;
        if (!IsServer) return;

        TimeRemaining.Value -= Time.deltaTime;
        if (TimeRemaining.Value <= 0)
        {
            TimeRemaining.Value = 0;
            isGameRunning = false;
            GameManager.Instance.EndGame();
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;
        isGameRunning = true;
        TimeRemaining.Value = 300f;
        TeamAKills.Value = 0;
        TeamBKills.Value = 0;
    }

    public void AddKill(Faction faction)
    {
        if (!IsServer) return;
        if (faction == Faction.TeamA) TeamAKills.Value++;
        else if (faction == Faction.TeamB) TeamBKills.Value++;
    }
}