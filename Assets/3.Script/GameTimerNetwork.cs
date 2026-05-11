using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTimerNetwork : NetworkBehaviour
{
    private bool playedTenSecondWarning = false;

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

        if (!playedTenSecondWarning && TimeRemaining.Value <= 10f)
        {
            playedTenSecondWarning = true;
            PlayTenSecondWarning_Rpc();
        }

        if (TimeRemaining.Value <= 0)
        {
            TimeRemaining.Value = 0;
            isGameRunning = false;
            EndGame_Rpc();
        }
    }

    public void StartGame()
    {
        if (!IsServer) return;
        isGameRunning = true;
        TimeRemaining.Value = 302f;
        TeamAKills.Value = 0;
        TeamBKills.Value = 0;
        playedTenSecondWarning = false;
    }

    public void AddKill(Faction faction, ulong killerClientId)
    {
        if (!IsServer) return;
        if (faction == Faction.TeamA) TeamAKills.Value++;
        else if (faction == Faction.TeamB) TeamBKills.Value++;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(killerClientId, out var client))
        {
            ulong killerNetObjId = client.PlayerObject.NetworkObjectId;
            PlayKillParticle_Rpc(killerNetObjId);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayKillParticle_Rpc(ulong killerNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(killerNetworkObjectId, out var netObj)) return;

        AudioManager.Instance.PlaySFX("Yipee");
        ParticleManager.Instance.PlayKillParticle(netObj.transform);

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EndGame_Rpc()
    {
        GameManager.Instance.EndGame();

        StartCoroutine(DisconnectAndReturnToOfflineRoutine());
    }

    private IEnumerator DisconnectAndReturnToOfflineRoutine()
    {
        yield return new WaitForSeconds(10f);

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        yield return new WaitUntil(() => !NetworkManager.Singleton.IsConnectedClient && !NetworkManager.Singleton.IsServer);

        SceneChangeManager.Instance.ChangeSceneForSinglePlay(SceneType.RoomScene);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayTenSecondWarning_Rpc()
    {
        StartCoroutine(PlayWarningLoop_Co());
    }

    private IEnumerator PlayWarningLoop_Co()
    {
        AudioManager.Instance.PlaySFX("Warning", loop: true);
        yield return new WaitForSeconds(10f);
        AudioManager.Instance.StopSFX();
    }
}