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
    private string gameLiftPermissionURL = "https://0yxisa9boc.execute-api.ap-northeast-2.amazonaws.com/prod1/startmatchmaking/GameLiftPermission";
    private string ticketId = "";

    public void RequestTicket()
    {
        StartCoroutine(RequestTicketFromServer_co());
    }

    private IEnumerator RequestTicketFromServer_co()
    {
        string jsonPlayload = "{\"playerId\":\"" + "SongJunYeop" + "\", \"skill\": 1500}";

        UnityWebRequest request = new UnityWebRequest(ticketRequestURL, "POST");
        byte[] body = Encoding.UTF8.GetBytes(jsonPlayload);
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

    //TODO 직접 구현해라
    private IEnumerator CheckMatchmakingSession()
    {
        bool isMatchmaking = true;

        while (isMatchmaking)
        {
            Debug.Log($"서버에 요청 보내는중!! {ticketId}");
            // 1. 서버에 보낼 JSON 데이터 (티켓 ID만 보냄)
            string jsonPayload = "{\"ticketId\":\"" + ticketId + "\"}";

            UnityWebRequest request = new UnityWebRequest(gameLiftPermissionURL, "POST");
            byte[] body = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-api-key", "UkRPjtuH4x9CtgdQFd3Gq6VKARQ7hSh59fbYqLkU"); // 필요하다면

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                MatchStatusResponse statusRes = JsonUtility.FromJson<MatchStatusResponse>(request.downloadHandler.text);
                Debug.Log($"현재 매칭 상태: {statusRes.status}");

                // 2. 상태값에 따른 분기 처리
                if (statusRes.status == "COMPLETED")
                {
                    Debug.Log($"매칭 성공! 서버 접속 정보: IP={statusRes.ip}, Port={statusRes.port}");
                    isMatchmaking = false;

                    // TODO: 여기서 발급받은 IP, Port, PlayerSessionId를 이용해 Netcode(NGO) 접속 시도
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

            // 3. 서버에 무리가 가지 않도록 3초 대기 후 다시 확인
            if (isMatchmaking)
            {
                yield return new WaitForSeconds(3f);
            }
        }
    }
    //
    private void OnDestroy()
    {
        ticketId = "";
        GameLiftServerAPI.ProcessEnding();
    }
}