using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ServerTCP
{
    private TcpListener listener;
    private DB_Controller db;

    private TcpClient tcpClient;

    public void StartServer(int port)
    {
        db = new DB_Controller();
        db.Connect();

        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        Console.WriteLine("start server : port " + port);

        listener.BeginAcceptTcpClient(OnClientConnect, null);
    }

    private void OnClientConnect(IAsyncResult ar)
    {
        tcpClient = listener.EndAcceptTcpClient(ar);
        listener.BeginAcceptTcpClient(OnClientConnect, null);

        ThreadPool.QueueUserWorkItem(HandleClient, tcpClient);
    }

    private void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            string msg = Encoding.UTF8.GetString(buffer, 0, byteCount);
            string response = ProcessMessage(msg);
            byte[] resBytes = Encoding.UTF8.GetBytes(response);
            stream.Write(resBytes, 0, resBytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Client handle error" + e.Message);
        }
        finally
        {
            client.Close();
        }
    }

    private string ProcessMessage(string msg)
    {
        // message format = "COMMAND|id|pw|nickname"
        string[] parts = msg.Split('|');
        if (parts.Length < 2) return "ERROR";

        string command = parts[0];

        switch (command.ToUpper())
        {
            case "CHECKID":
                if (parts.Length < 2) return "ERROR";
                if (!db.CheckIdExists(parts[1]))
                {
                    return "SUCCESS_ID_CHECK";
                }
                return "FAIL_ID_CHECK";

            case "SIGNUP":
                if (parts.Length < 3) return "ERROR";
                if (db.Register(parts[1], parts[2]))
                {
                    return "OK";
                }
                return "FAIL";

            case "CHECKNICKNAME":
                if (parts.Length < 4) return "ERROR";

                return db.CheckNicknameExists(parts[3]) ? "FAIL_NICKNAME_CHECK" : "SUCCESS_NICKNAME_CHECK";

            case "SETNICKNAME":
                if (parts.Length < 4) return "ERROR";

                if (db.Register(parts[1], parts[2]))
                {
                    return db.SetNickname(parts[1], parts[3]) ? "SUCCESS_NICKNAME_SET" : "FAIL_NICKNAME_SET";
                }
                return "FAIL_REGISTER_ID";

            case "LOGIN":
                if (parts.Length < 3) return "ERROR";
                return db.Login(parts[1], parts[2]) ? "OK" : "FAIL";

            case "UPDATE_SCORE":
                if (parts.Length < 3) return "ERROR";
                if (int.TryParse(parts[2], out int score)) return db.UpdateUserScore(parts[1], score) ? "OK" : "FAIL";
                return "ERROR";

            default:
                return "UNKNOWN_COMMAND";
        }
    }

    public void StopServer()
    {
        tcpClient.Close();
        tcpClient = null;

        listener.Stop();
        db.Disconnect();
        Console.WriteLine("Stop server!");
    }
}
