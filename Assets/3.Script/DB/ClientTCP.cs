using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ClientTCP : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("--- 1. 회원가입 테스트 ---");
        TestRegister();

        Debug.Log("--- 2. 로그인 테스트 시작 ---");
        TestLogin();

        Debug.Log("--- 3. 점수 업데이트 테스트 시작 ---");
        TestUpdaterScore();
    }

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

    public void TestLogin()
    {
        SendRequest("LOGIN|newUser|1234");
    }

    public void TestRegister()
    {
        SendRequest("REGISTER|newUser|1234|Nickname");
    }

    public void TestUpdaterScore()
    {
        SendRequest("UPDATE_SCORE|newUser|500");
    }
}
