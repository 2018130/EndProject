using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ClientTCP : MonoBehaviour
{
    public void SendRequest(string msg)
    {
        try
        {
            TcpClient client = new TcpClient("127.0.0.1", 7777);
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(msg);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[1024];
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, byteCount);
            Debug.Log($"서버 응답 ({msg.Split('|')[0]}) : " + response);

            client.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("클라이언트 전송 에러 : " + e.Message);
        }
    }

    public void SignIn(string id, string pwd)
    {
        SendRequest($"LOGIN|{id}|{pwd}");
    }

    public void Register(string id, string pwd, string nickname)
    {
        SendRequest($"REGISTER|{id}|{pwd}|{nickname}");
    }

    public void UpdaterScore(string id, string score)
    {
        SendRequest($"UPDATE_SCORE|{id}|{score}");
    }
}
