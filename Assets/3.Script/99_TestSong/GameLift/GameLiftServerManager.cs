using System.Collections.Generic;
using UnityEngine;
using Aws.GameLift.Server;
using Aws.GameLift.Server.Model; // UpdateGameSession 모델을 사용하기 위해 필요할 수 있습니다.
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

public class GameLiftServerManager : MonoBehaviour
{
    private void Awake()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;

        System.IO.File.AppendAllText("ServerLog.txt", "Awake Called!\n");

#if UNITY_SERVER || UNITY_EDITOR
        System.IO.File.AppendAllText("ServerLog.txt", "Initializing GameLift...\n");
        InitializeGameLift();
#else
        System.IO.File.AppendAllText("ServerLog.txt", "Not a Server Build!\n");
#endif
    }

    #region GameLift
    private void InitializeGameLift()
    {
        Debug.Log($"Initialize gamelift called");
        // 1. SDK 초기화
        GenericOutcome initOutcome = GameLiftServerAPI.InitSDK();
        if (!initOutcome.Success)
        {
            Debug.LogError($"GameLift SDK 초기화 실패 : {initOutcome.Error}");
            return;
        }

        // 2. 콜백 함수 설정
        ProcessParameters processParams = new ProcessParameters(
            OnStartGameSession,
            OnUpdateGameSession,
            OnProcessTerminate,
            OnHealthCheck,
            7777,
            new LogParameters(new List<string> { "/local/game/logs/myserver.log" })
            );

        // 3. AWS에 준비 완료 보고
        GenericOutcome processReadyOutcome = GameLiftServerAPI.ProcessReady(processParams);
        if (processReadyOutcome.Success)
        {
            Debug.Log($"GameLift Process Ready 성공!! 대기중...");
        }
        else
        {
            Debug.LogError($"GameLift Process Ready 실패!! {processReadyOutcome.Error.ErrorMessage}");
        }
    }


    private void OnStartGameSession(GameSession gameSession)
    {
        Debug.Log($"GameLift로부터 게임 세션 시작 요청 받음");

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        // 서버는 자신의 IP를 몰라도 됨.
        // Client가 요청을 보내고 요청을 보낸 클라이언트의 IP를 알면 쏘기만 하면 되기 때문
        transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");

        NetworkManager.Singleton.StartServer();

        GenericOutcome activeSessionOutcome = GameLiftServerAPI.ActivateGameSession();

        if (activeSessionOutcome.Success)
        {
            Debug.Log($"게임 세션 활성화 성공");
        }
        else
        {
            Debug.LogError($"게임 세션 활성화 실패 : {activeSessionOutcome.Error.ErrorMessage}");
        }
    }

    private void OnUpdateGameSession(UpdateGameSession updateGameSession)
    {

    }

    private void OnProcessTerminate()
    {
        Debug.Log("GameLift가 프로세스 종료를 요청함");
        GameLiftServerAPI.ProcessEnding();
        Application.Quit();
    }

    private bool OnHealthCheck()
    {
        return true;
    }
    #endregion

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string jsonPayload = Encoding.UTF8.GetString(request.Payload);
        ClientConnectionPayload payloadData = JsonUtility.FromJson<ClientConnectionPayload>(jsonPayload);

        GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(payloadData.playerSessionId);

        if (outcome.Success)
        {
            Debug.Log($"유저 접속 승인 완료! playerId : {payloadData.playerId}");
            response.Approved = true;
            response.CreatePlayerObject = true;
        }
        else
        {
            Debug.LogError($"비정상적인 유저 접속 차단!");
            response.Approved = false;
        }
    }
}