using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System;

public class DatabaseManager : MonoBehaviour
{
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TextMeshProUGUI statusText;

    private readonly string connectionString = "Server=localhost;Database=ChessDB;Integrated Security=True;";

    void Start()
    {
        CheckAutoLogin();
        //SceneManager.LoadScene("LoginScene");
    }

    public void Register()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            statusText.text = "Заполните все поля!";
            return;
        }

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();
        string query = "INSERT INTO Players (Email, PlayerName, Password) VALUES (@Email, @Email, @Password)";
        SqlCommand command = new SqlCommand(query, conn);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@Password", password);

        try
        {
            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                statusText.text = "Регистрация успешна!";
            }
        }
        catch (SqlException)
        {
            statusText.text = "Email уже зарегистрирован!";
        }
    }

    public void Login()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        string query = "SELECT PlayerID, PlayerName FROM Players WHERE Email = @Email AND Password = @Password";
        SqlCommand command = new SqlCommand(query, conn);
        command.Parameters.AddWithValue("@Email", email);
        command.Parameters.AddWithValue("@Password", password);

        using SqlDataReader reader = command.ExecuteReader();
        if (reader.Read())
        {
            int playerId = reader.GetInt32(0);
            PlayerPrefs.SetInt("PlayerID", playerId);
            Debug.Log($"Login successful. PlayerID {playerId} saved to PlayerPrefs.");

            SaveLoginData(email, playerId);
            statusText.text = "Вход выполнен!";
            SceneManager.LoadScene("MenuScene");
        }
        else
        {
            statusText.text = "Неверный email или пароль!";
        }
    }

    public void Logout()
    {
        string path = Application.persistentDataPath + "/loginData.json";
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    void SaveLoginData(string email, int playerId)
    {
        string jsonData = $"{{\"email\":\"{email}\",\"playerId\":{playerId}}}";
        File.WriteAllText(Application.persistentDataPath + "/loginData.json", jsonData);

        PlayerPrefs.SetInt("PlayerID", playerId);
        PlayerPrefs.Save();
    }
    [Serializable]
    public class LoginData
    {
        public int playerId;
    }
    void CheckAutoLogin()
    {
        string path = Application.persistentDataPath + "/loginData.json";

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            try
            {
                LoginData loginData = JsonUtility.FromJson<LoginData>(jsonData);

                if (loginData != null && loginData.playerId > 0)
                {
                    PlayerPrefs.SetInt("PlayerID", loginData.playerId);
                    PlayerPrefs.Save();
                    Debug.Log($"Auto-login: PlayerID {loginData.playerId} loaded from saved data and stored in PlayerPrefs.");
                }
                else
                {
                    Debug.LogError("Failed to parse PlayerID from saved login data.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during auto-login: {e.Message}");
            }

            SceneManager.LoadScene("MenuScene");
        }
    }
    private void OnApplicationQuit()
    {
        if (PlayerPrefs.HasKey("CurrentGameID"))
        {
            int gameId = PlayerPrefs.GetInt("CurrentGameID");
            int playerId = PlayerPrefs.GetInt("PlayerID");
            RegisterLoss(gameId, playerId);
        }
    }

    private void RegisterLoss(int gameId, int playerId)
    {
        string query = "UPDATE Games SET WinnerID = CASE WHEN Player1ID = @PlayerID THEN Player2ID ELSE Player1ID END WHERE GameID = @GameID";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PlayerID", playerId);
                command.Parameters.AddWithValue("@GameID", gameId);
                command.ExecuteNonQuery();
            }
        }
    }
    public static List<(int gameId, string gameName)> GetCompletedGames()
    {
        List<(int, string)> games = new List<(int, string)>();

        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();
            string query = "SELECT GameID, CONCAT(Player1ID, ' vs ', Player2ID, ' - ', GameEnd) FROM Games WHERE GameEnd IS NOT NULL";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    games.Add((reader.GetInt32(0), reader.GetString(1)));
                }
            }
        }

        return games;
    }
    public static List<string> GetGameMoves(int gameId)
    {
        List<string> moves = new List<string>();

        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();
            string query = "SELECT MoveNotation FROM Moves WHERE GameID = @GameID ORDER BY MoveID ASC";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@GameID", gameId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        moves.Add(reader.GetString(0));
                    }
                }
            }
        }

        return moves;
    }
    public static int CreateNewGame(int player1Id, int player2Id = -1)
    {
        int gameId = -1;

        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();

            // First verify the player exists
            string checkQuery = "SELECT COUNT(*) FROM Players WHERE PlayerID = @PlayerId";
            using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
            {
                checkCmd.Parameters.AddWithValue("@PlayerId", player1Id);
                int playerExists = (int)checkCmd.ExecuteScalar();

                if (playerExists == 0)
                {
                    Debug.LogError($"Player with ID {player1Id} does not exist in the database");
                    return -1; // Return an error code
                }
            }

            // For Player2ID, if it's not -1, also verify it exists
            if (player2Id != -1)
            {
                string checkP2Query = "SELECT COUNT(*) FROM Players WHERE PlayerID = @Player2Id";
                using (SqlCommand checkP2Cmd = new SqlCommand(checkP2Query, conn))
                {
                    checkP2Cmd.Parameters.AddWithValue("@Player2Id", player2Id);
                    int player2Exists = (int)checkP2Cmd.ExecuteScalar();

                    if (player2Exists == 0)
                    {
                        Debug.LogError($"Player with ID {player2Id} does not exist in the database");
                        return -1;
                    }
                }
            }

            // Now create the game
            string query = "INSERT INTO Games (Player1ID, Player2ID, GameStart) VALUES (@PlayerId, @Player2Id, GETDATE()); SELECT SCOPE_IDENTITY();";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@PlayerId", player1Id);
                cmd.Parameters.AddWithValue("@Player2Id", player2Id);

                gameId = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        PlayerPrefs.SetInt("CurrentGameID", gameId);
        return gameId;
    }


    public static void EndGame(int gameId, int winnerId)
    {
        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();
            string query = "UPDATE Games SET GameEnd = GETDATE(), WinnerID = @WinnerId WHERE GameID = @GameId";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@GameId", gameId);
                cmd.Parameters.AddWithValue("@WinnerId", winnerId);
                cmd.ExecuteNonQuery();
            }
        }
    }
    public static void RecordMove(int gameId, string moveNotation)
    {
        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();
            string query = "INSERT INTO Moves (GameID, MoveNotation, MoveTime) VALUES (@GameId, @MoveNotation, GETDATE())";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@GameId", gameId);
                cmd.Parameters.AddWithValue("@MoveNotation", moveNotation);
                cmd.ExecuteNonQuery();
            }
        }
    }
    
}