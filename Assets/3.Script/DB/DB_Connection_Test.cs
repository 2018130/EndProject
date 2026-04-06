using UnityEngine;

public class DB_Connection_Test : MonoBehaviour
{
    private ServerTCP serverTcp;

    void Start()
    {
        serverTcp = new ServerTCP();

        Debug.Log("게임 서버 구동을 시도합니다...");
        serverTcp.StartServer(7777);
    }

    void OnApplicationQuit()
    {
        if (serverTcp != null)
        {
            serverTcp.StopServer();
        }
    }
}