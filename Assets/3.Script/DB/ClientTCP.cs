using System;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class ClientTCP : SingletonBehaviour<ClientTCP>
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
            ProcessMessage(response);

            client.Close();
        }
        catch (Exception e)
        {
            Debug.LogError("클라이언트 전송 에러 : " + e.Message);
        }
    }

    private void ProcessMessage(string msg)
    {
        switch(msg)
        {
            case "SUCCESS_ID_CHECK":
                SignSceneUIManager.Instance.SetInteractIdInputField(false);
                SignSceneUIManager.Instance.ToggleNicknamePopup();
                break;

            case "FAIL_ID_CHECK":
                SignSceneUIManager.Instance.SetErrorText("Already exist id");
                break;

            case "SUCCESS_NICKNAME_CHECK":
                SignSceneUIManager.Instance.SetInteractNicknameInputField(false);
                break;

            case "FAIL_NICKNAME_CHECK":
                SignSceneUIManager.Instance.SetNicknameErrorText("Already exist nickname");
                break;

            case "SUCCESS_NICKNAME_SET":
                SignSceneUIManager.Instance.ToggleNicknamePopup();
                break;

            case "FAIL_NICKNAME_SET":
                SignSceneUIManager.Instance.SetNicknameErrorText("Error to set nickname");
                break;
        }
    }
    public void CheckId(string id)
    {
        SendRequest($"CHECKID|{id}");
    }

    public void SignUp(string id, string pwd)
    {
        SendRequest($"SIGNUP|{id}|{pwd}");
    }

    public void SignIn(string id, string pwd)
    {
        SendRequest($"LOGIN|{id}|{pwd}");
    }

    public void CheckNickname(string id, string pwd, string nickname)
    {
        SendRequest($"CHECKNICKNAME|{id}|{pwd}|{nickname}");
    }
    public void SetNickname(string id, string pwd, string nickname)
    {
        SendRequest($"SETNICKNAME|{id}|{pwd}|{nickname}");
    }

    public void UpdaterScore(string id, string score)
    {
        SendRequest($"UPDATE_SCORE|{id}|{score}");
    }
}
