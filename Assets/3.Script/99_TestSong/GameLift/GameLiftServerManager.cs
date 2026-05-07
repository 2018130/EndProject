using System.Collections.Generic;
using UnityEngine;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model;
using Unity.Netcode;
using System.Text;
using Aws.GameLift;
using System;
using Unity.Netcode.Transports.UTP;

[System.Serializable]
public class ClientConnectionPayload
{
    public string playerId;
    public string playerSessionId;
}

public class GameLiftServerManager : SingletonBehaviour<GameLiftServerManager>
{
    private bool isInitialized = false; 
    private bool _sessionStartRequested = false;

    private Dictionary<ulong, string> _activePlayerSessions = new Dictionary<ulong, string>();

    private void Start()
    {
        if (!Instance.isInitialized)
        {
            isInitialized = true;
#if UNITY_SERVER && !UNITY_EDITOR
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
            InitializeGameLift();
#endif
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

    }

    private void Update()
    {
        if (_sessionStartRequested)
        {
            _sessionStartRequested = false;
            StartNGOServerAndActivate(); // 메인 스레드에서 안전하게 실행!
        }
    }

    #region GameLift Core Logic
    private void InitializeGameLift()
    {
        Debug.Log("[GameLift] Starting SDK Initialization sequence...");

        // 1. Fetch Environment Variables (Crucial for Managed EC2 / SDK 5.x)
        string websocketUrl = Environment.GetEnvironmentVariable("GAMELIFT_SDK_WEBSOCKET_URL");
        string authToken = Environment.GetEnvironmentVariable("GAMELIFT_SDK_AUTH_TOKEN");
        string processId = Environment.GetEnvironmentVariable("GAMELIFT_PROCESS_ID");
        string hostId = Environment.GetEnvironmentVariable("GAMELIFT_ANYWHERE_HOST_ID");
        string fleetId = Environment.GetEnvironmentVariable("GAMELIFT_ANYWHERE_FLEET_ID");

        // Log variable status (Do not log the actual AuthToken for security)
        Debug.Log($"[GameLift] Environment Variables Load Result: \n" +
                  $"- WebSocketURL: {(!string.IsNullOrEmpty(websocketUrl) ? "LOADED" : "MISSING!")}\n" +
                  $"- AuthToken: {(!string.IsNullOrEmpty(authToken) ? "LOADED" : "MISSING!")}\n" +
                  $"- ProcessId: {processId}\n" +
                  $"- FleetId: {fleetId}");

        ServerParameters serverParameters = new ServerParameters(
            websocketUrl,
            authToken,
            fleetId,
            hostId,
            processId
        );

        // 2. Initialize SDK
        GenericOutcome initOutcome = GameLiftServerAPI.InitSDK(serverParameters);
        if (initOutcome.Success)
        {
            Debug.Log("[GameLift] InitSDK successful.");
        }
        else
        {
            Debug.LogError($"[GameLift] InitSDK FAILED: {initOutcome.Error.ErrorMessage}");
            return;
        }

        // 3. Setup Process Parameters
        ProcessParameters processParams = new ProcessParameters(
            OnStartGameSession,
            OnUpdateGameSession,
            OnProcessTerminate,
            OnHealthCheck,
            7777, // Port your server is listening on
            new LogParameters(new List<string> { "/local/game/logs/myserver.log" })
        );

        // 4. Report Process Ready to AWS
        GenericOutcome processReadyOutcome = GameLiftServerAPI.ProcessReady(processParams);
        if (processReadyOutcome.Success)
        {
            Debug.Log("[GameLift] ProcessReady successful! Server is now waiting for a GameSession.");
        }
        else
        {
            Debug.LogError($"[GameLift] ProcessReady FAILED: {processReadyOutcome.Error.ErrorMessage}");
        }
    }

    private void OnStartGameSession(GameSession gameSession)
    {
        Debug.Log($"[GameLift] Game Session Start Request Received.\n" +
                  $"- Session ID: {gameSession.GameSessionId}");

        // 주의: 여기서는 유니티 API를 호출하면 안 됩니다! 
        // 메인 스레드(Update)에서 처리하도록 플래그만 true로 바꿔줍니다.
        _sessionStartRequested = true;
    }
    private void StartNGOServerAndActivate()
    {
        // 1. Configure Transport
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");

        // 2. Start the NGO Server
        if (NetworkManager.Singleton.StartServer())
        {
            Debug.Log("[Netcode] NGO Server started successfully on port 7777.");
        }
        else
        {
            Debug.LogError("[Netcode] NGO Server failed to start!");
            return;
        }

        // 3. Activate the session so GameLift knows players can join
        GenericOutcome activeSessionOutcome = GameLiftServerAPI.ActivateGameSession();
        if (activeSessionOutcome.Success)
        {
            Debug.Log("[GameLift] ActivateGameSession successful. Session is now ACTIVE.");
        }
        else
        {
            Debug.LogError($"[GameLift] ActivateGameSession FAILED: {activeSessionOutcome.Error.ErrorMessage}");
        }
    }

    private void OnUpdateGameSession(UpdateGameSession updateGameSession)
    {
        Debug.Log($"[GameLift] Game Session Update received. Reason: {updateGameSession.UpdateReason}");
    }

    private void OnProcessTerminate()
    {
        Debug.LogWarning("[GameLift] Termination request received from AWS. Shutting down...");
        GameLiftServerAPI.ProcessEnding();
        Application.Quit();
    }

    private bool OnHealthCheck()
    {
        // Return true if the server is healthy. AWS will terminate the process if this returns false repeatedly.
        return true;
    }
    #endregion

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            string jsonPayload = Encoding.UTF8.GetString(request.Payload);
            ClientConnectionPayload payloadData = JsonUtility.FromJson<ClientConnectionPayload>(jsonPayload);

            Debug.Log($"[Netcode] Connection request: PlayerID={payloadData.playerId}, PlayerSessionID={payloadData.playerSessionId}");

            // Validate the Player Session with GameLift
            GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(payloadData.playerSessionId);

            if (outcome.Success)
            {
                Debug.Log($"[GameLift] Player session ACCEPTED: {payloadData.playerId}");
                response.Approved = true;
                response.CreatePlayerObject = true;
            }
            else

            {
                Debug.LogError($"[GameLift] Player session REJECTED: {outcome.Error.ErrorMessage}");
                response.Approved = false;
                response.Reason = "GameLift validation failed.";
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Netcode] Exception in ApprovalCheck: {ex.Message}");
            response.Approved = false;
        }
    }

    private void OnClientDisconnect(ulong clientId)
    {
        // 1. 게임리프트에 플레이어 퇴장 알림
        if (_activePlayerSessions.TryGetValue(clientId, out string playerSessionId))
        {
            GameLiftServerAPI.RemovePlayerSession(playerSessionId);
            _activePlayerSessions.Remove(clientId);
            Debug.Log($"[GameLift] Player removed from session: {clientId}");
        }

        // 2. 남은 유저 확인 후 서버 종료
        // 콜백 발생 시점에 해당 clientId가 딕셔너리에 남아있을 수 있으므로 
        // 딕셔너리 크기가 0이거나(모두 나감), Count <= 1 이면 빈 방으로 간주
        if (NetworkManager.Singleton.ConnectedClients.Count <= 1)
        {
            Debug.Log("[GameLift] All players left. Ending Process.");

            // NGO 서버를 먼저 안전하게 내리고
            if (NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
            }

            // 게임리프트 프로세스 종료 보고 후 앱 종료
            GameLiftServerAPI.ProcessEnding();
            Application.Quit();
        }
    }
}