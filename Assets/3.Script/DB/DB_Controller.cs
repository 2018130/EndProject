using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using MySql.Data.MySqlClient;

public class DB_Controller
{
    private MySqlConnection connection;

    private string server = "127.0.0.1"; //"your-rds-endpoint.amazonaws.com";
    private string database = "projectLoginData";
    private string username = "root";//"your_username";
    private string password = "23568900";//"your_password";

    public void Connect()
    {
        string connString = $"Server={server};Database={database};User ID={username};Password={password};Pooling=false;";

        connection = new MySqlConnection(connString);

        try
        {
            connection.Open();
            Debug.Log("DB connection successful.");
        }
        catch (Exception e)
        {
            Debug.Log("DB connection failed: " + e.Message);
        }
    }

    public void Disconnect()
    {
        if (connection != null)
        {
            connection.Close();
            connection = null;
            Debug.Log("DB connection closed.");
        }
    }

    private string GenerateSalt(int length = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        char[] salt = new char[length];
        for (int i = 0; i < length; i++)
        {
            salt[i] = chars[random.Next(chars.Length)];
        }
        return new string(salt);
    }

    private string HashPassword(string password, string salt)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password + salt);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }

    public bool Register(string id, string password)
    {
        if (connection == null)
        {
            Debug.LogError("No DB connection.");
            return false;
        }

        if (CheckIdExists(id))
        {
            Debug.LogError("ID already exists.");
            return false;
        }

        string salt = GenerateSalt();
        string hashedPw = HashPassword(password, salt);

        string query = "INSERT INTO users (id,password_hash,salt,score) VALUES (@id,@pw,@salt,0)";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@pw", hashedPw);
            cmd.Parameters.AddWithValue("@salt", salt);

            try
            {
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Registration failed: " + e.Message);
                return false;
            }
        }
    }

    public bool SetNickname(string userId, string nickname)
    {
        if (connection == null)
        {
            Debug.LogError("No DB connection.");
            return false;
        }

        if (CheckNicknameExists(nickname))
        {
            Debug.LogError("Nickname already exists.");
            return false;
        }

        string query = "UPDATE users SET nickname=@nick WHERE id=@id";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@nick", nickname);
            cmd.Parameters.AddWithValue("@id", userId);

            try
            {
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Nickname setting failed: " + e.Message);
                return false;
            }
        }
    }

    public bool CheckIdExists(string id)
    {
        string query = "SELECT COUNT(*) FROM users WHERE id=@id";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@id", id);

            try
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("ID duplication check failed: " + e.Message);
                return false;
            }
        }
    }

    public bool CheckNicknameExists(string nickname)
    {
        string query = "SELECT COUNT(*) FROM users WHERE nickname=@nick";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@nick", nickname);

            try
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Nickname duplication check failed: " + e.Message);
                return false;
            }
        }
    }

    public bool Login(string inputId, string inputPw)
    {
        if (connection == null)
        {
            Debug.LogError("No DB connection.");
            return false;
        }

        string saltQuery = "SELECT salt FROM users WHERE id=@id";
        string salt;
        using (MySqlCommand cmd = new MySqlCommand(saltQuery, connection))
        {
            cmd.Parameters.AddWithValue("@id", inputId);
            object result = cmd.ExecuteScalar();
            if (result == null) return false;
            salt = result.ToString();
        }

        string hashedInput = HashPassword(inputPw, salt);

        string loginQuery = "SELECT COUNT(*) FROM users WHERE id=@id AND password_hash=@pw";
        using (MySqlCommand cmd = new MySqlCommand(loginQuery, connection))
        {
            cmd.Parameters.AddWithValue("@id", inputId);
            cmd.Parameters.AddWithValue("@pw", hashedInput);

            try
            {
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Login failed: " + e.Message);
                return false;
            }
        }
    }

    public string GetUserNickname(string userId)
    {
        string query = "SELECT nickname FROM users WHERE id=@id";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@id", userId);

            try
            {
                object result = cmd.ExecuteScalar();
                return result != null ? result.ToString() : null;
            }
            catch (Exception e)
            {
                Debug.LogError("Data retrieval failed: " + e.Message);
                return null;
            }
        }
    }

    public bool UpdateUserScore(string userId, int score)
    {
        string query = "UPDATE users SET score=@score WHERE id=@id";

        using (MySqlCommand cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@score", score);
            cmd.Parameters.AddWithValue("@id", userId);

            try
            {
                int rows = cmd.ExecuteNonQuery();
                return rows > 0;
            }
            catch (Exception e)
            {
                Debug.LogError("Data update failed: " + e.Message);
                return false;
            }
        }
    }
}