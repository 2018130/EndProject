using UnityEngine;

public class DB_Connection_Test : MonoBehaviour
{
    private ServerTCP serverTcp;

    void Start()
    {
        serverTcp = new ServerTCP();

        Debug.Log("Connect to server...");
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