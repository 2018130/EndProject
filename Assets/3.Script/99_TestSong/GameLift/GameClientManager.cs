using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Text;
using System.Collections;
using UnityEngine.Networking;
using System;
using Aws.GameLift.Server;



[System.Serializable]
public class TicketResponse
{
    public string ticketId;
}


[System.Serializable]
public class MatchStatusResponse
{
    public string status; // 예: SEARCHING, PLACING, COMPLETED, FAILED 등
    public string ip;
    public ushort port;
    public string playerSessionId;
}

public class GameClientManager : MonoBehaviour
{
    private string ticketRequestURL = "https://0yxisa9boc.execute-api.ap-northeast-2.amazonaws.com/prod1/startmatchmaking";
    private string gameLiftPermissionURL = "https://0yxisa9boc.execute-api.ap-northeast-2.amazonaws.com/prod1/GameLiftPermission";
    private string ticketId = "";
    private string playerId = "SongJunYeop";

    private void Start()
    {
        playerId = "Tester_" + UnityEngine.Random.Range(1000, 10000).ToString();
<<<<<<< Updated upstream
=======
#if !UNITY_SERVER
        InitializeCognito();
#endif
>>>>>>> Stashed changes
    }

    public void RequestTicket()
    {
<<<<<<< Updated upstream
=======
        try
        {
            // 1. UnityInitializer 삭제 (최신 SDK에서는 불필요)
            AWSConfigs.RegionEndpoint = RegionEndpoint.APNortheast2;

            // 2. 익명 자격 증명 객체 생성
            credentials = new CognitoAWSCredentials(
                identityPoolId,
                RegionEndpoint.APNortheast2
            );

            // 3. 최신 async/await 방식으로 자격 증명(AccessKey, SecretKey, Token) 받아오기
            var creds = await credentials.GetCredentialsAsync();

            Debug.Log("Cognito 자격 증명 획득 성공!");
            // 이제 creds.AccessKey, creds.SecretKey, creds.Token 형태로 바로 접근 가능합니다.

            isCognitoReady = true; // 발급 완료 플래그 켜기

            JoinRoom();
        }
        catch (Exception ex)
        {
            // 발급 실패 시 에러 로그 출력
            Debug.LogError("Cognito 자격 증명 실패: " + ex.Message);
        }
    }

    public void JoinRoom()
    {
        if (!isCognitoReady)
        {
            Debug.LogWarning("아직 AWS 자격 증명을 받아오지 못했습니다. 잠시 후 다시 시도해주세요.");
            return;
        }

>>>>>>> Stashed changes
        StartCoroutine(RequestTicketFromServer_co());
    }

    private IEnumerator RequestTicketFromServer_co()
    {
        string jsonPayload = $"{{\"playerId\": \"{playerId}\", \"skill\": 1500}}";

        UnityWebRequest request = new UnityWebRequest(ticketRequestURL, "POST");
        byte[] body = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-api-key", "UkRPjtuH4x9CtgdQFd3Gq6VKARQ7hSh59fbYqLkU");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            TicketResponse response = JsonUtility.FromJson<TicketResponse>(request.downloadHandler.text);
            ticketId = response.ticketId;

            Debug.Log($"티켓 발급 완료!! ID : {ticketId}");

            StartCoroutine(CheckMatchmakingSession());
        }
        else
        {
            Debug.Log($"티켓 발급 실패");
        }
    }


    private IEnumerator CheckMatchmakingSession()
    {
        bool isMatchmaking = true;

        while (isMatchmaking)
        {
            Debug.Log($"서버에 요청 보내는중!! {ticketId}");

            string jsonPayload = $"{{\"ticketId\":\"{ticketId}\", \"playerId\":\"{playerId}\"}}";

            UnityWebRequest request = new UnityWebRequest(gameLiftPermissionURL, "POST");
            byte[] body = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", "UkRPjtuH4x9CtgdQFd3Gq6VKARQ7hSh59fbYqLkU");


            yield return request.SendWebRequest();

            Debug.Log($"서버 응답 원본: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                MatchStatusResponse statusRes = JsonUtility.FromJson<MatchStatusResponse>(request.downloadHandler.text);
                Debug.Log($"현재 매칭 상태: {statusRes.status}");

                // 2. 상태값에 따른 분기 처리
                if (statusRes.status == "COMPLETED")
                {
                    Debug.Log($"매칭 성공! 서버 접속 정보: IP={statusRes.ip}, Port={statusRes.port}");
                    isMatchmaking = false;

                    ConnetToServer(statusRes);
                }
                else if (statusRes.status == "FAILED" || statusRes.status == "TIMED_OUT")
                {
                    Debug.Log("매치메이킹 실패 또는 시간 초과");
                    isMatchmaking = false;
                }
            }
            else
            {
                Debug.LogError("상태 확인 요청 실패: " + request.error);
            }

            if (isMatchmaking)
            {
                yield return new WaitForSeconds(3f);
            }
        }
    }

    private void ConnetToServer(MatchStatusResponse matchStatusResponse)
    {
        ClientConnectionPayload clientPayload =
            new ClientConnectionPayload()
            {
                playerId = this.playerId,
                playerSessionId = matchStatusResponse.playerSessionId,
            };

        string jsonPayload = JsonUtility.ToJson(clientPayload);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(matchStatusResponse.ip, matchStatusResponse.port);

        NetworkManager.Singleton.StartClient();

        Debug.Log($"NGO 서버 접속 시도중...");
    }


    private void OnDestroy()

    {

        ticketId = "";

    }

}