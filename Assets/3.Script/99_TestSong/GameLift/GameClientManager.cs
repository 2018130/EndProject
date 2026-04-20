using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.Runtime;

[System.Serializable]
public class TicketResponse
{
    public string ticketId;
}

[System.Serializable]
public class MatchStatusResponse
{
    public string status;
    public string ip;
    public ushort port;
    public string playerSessionId;
}

public class GameClientManager : MonoBehaviour
{
    public static GameClientManager Instance { get; set; }
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("AWS Configuration")]
    [SerializeField] private string identityPoolId = "ap-northeast-2:YOUR_IDENTITY_POOL_ID";
    [SerializeField] private string awsRegion = "ap-northeast-2";

    [Header("API Endpoints")]
    private string ticketRequestURL = "https://0yxisa9boc.execute-api.ap-northeast-2.amazonaws.com/prod1/startmatchmaking";
    private string gameLiftPermissionURL = "https://0yxisa9boc.execute-api.ap-northeast-2.amazonaws.com/prod1/GameLiftPermission";

    private string ticketId = "";
    private string playerId = "";

    // AWS Cognito 자격 증명 상태 관리
    private CognitoAWSCredentials credentials;
    private bool isCognitoReady = false;

    private void Start()
    {
        playerId = "Tester_" + UnityEngine.Random.Range(1000, 10000).ToString();

#if !UNITY_SERVER || UNITY_EDITOR
        InitializeCognito();
#endif
    }

    private async void InitializeCognito()
    {
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

        StartCoroutine(RequestTicketFromServer_co());
    }

    private IEnumerator RequestTicketFromServer_co()
    {
        string jsonPayload = $"{{\"playerId\": \"{playerId}\", \"skill\": 1500}}";

        using (UnityWebRequest request = CreateSignedRequest(ticketRequestURL, "POST", jsonPayload))
        {
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
                Debug.LogError($"티켓 발급 실패: {request.error}\n응답: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator CheckMatchmakingSession()
    {
        bool isMatchmaking = true;

        while (isMatchmaking)
        {
            Debug.Log($"서버에 매칭 상태 확인 요청 중... TicketID: {ticketId}");

            string jsonPayload = $"{{\"ticketId\":\"{ticketId}\", \"playerId\":\"{playerId}\"}}";

            using (UnityWebRequest request = CreateSignedRequest(gameLiftPermissionURL, "POST", jsonPayload))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    MatchStatusResponse statusRes = JsonUtility.FromJson<MatchStatusResponse>(request.downloadHandler.text);
                    Debug.Log($"현재 매칭 상태: {statusRes.status}");

                    if (statusRes.status == "COMPLETED")
                    {
                        Debug.Log($"매칭 성공! 서버 접속 정보: IP={statusRes.ip}, Port={statusRes.port}");
                        isMatchmaking = false;
                        ConnectToServer(statusRes);
                    }
                    else if (statusRes.status == "FAILED" || statusRes.status == "TIMED_OUT")
                    {
                        Debug.Log("매치메이킹 실패 또는 시간 초과");
                        isMatchmaking = false;
                    }
                }
                else
                {
                    Debug.LogError($"상태 확인 요청 실패: {request.error}");
                }
            }

            if (isMatchmaking)
            {
                yield return new WaitForSeconds(3f);
            }
        }
    }

    /// <summary>
    /// AWS IAM 인증(SigV4)이 적용된 UnityWebRequest를 생성합니다.
    /// </summary>
    private UnityWebRequest CreateSignedRequest(string url, string method, string jsonPayload)
    {
        UnityWebRequest request = new UnityWebRequest(url, method);
        byte[] body = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");

        // 1. 발급받은 임시 자격 증명 꺼내기
        ImmutableCredentials creds = credentials.GetCredentials();

        // 2. 방금 만든 헬퍼 클래스를 사용해 AWS SigV4 서명 처리!
        // API Gateway를 호출할 때의 서비스 이름은 "execute-api" 입니다.
        AwsSigV4Helper.SignRequest(
            request: request,
            region: awsRegion,          // 예: "ap-northeast-2"
            service: "execute-api",
            accessKey: creds.AccessKey,
            secretKey: creds.SecretKey,
            sessionToken: creds.Token,
            payload: jsonPayload        // Body 내용도 서명 계산에 포함되어야 함
        );

        return request;
    }

    public void ConnectToServer(MatchStatusResponse matchStatusResponse)
    {
        if(matchStatusResponse == null || matchStatusResponse.status != "COMPLETED")
        {
            Debug.Log($"Failed to complete matching");
            return;
        }

        ClientConnectionPayload clientPayload = new ClientConnectionPayload()
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