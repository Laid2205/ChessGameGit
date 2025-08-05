using System.Data.SqlClient;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public GameObject modePanel; // Панель с кнопками выбора
    public Button localPlayButton;
    public Button cancelButton;

    private void Start()
    {
        modePanel.SetActive(false); // Скрываем панель выбора режима
        localPlayButton.onClick.AddListener(StartLocalGame);
        cancelButton.onClick.AddListener(() => modePanel.SetActive(false));
    }

    public void PlayButtonPressed()
    {
        modePanel.SetActive(true); // Показываем выбор режимов
    }

    private void StartLocalGame()
    {
        if (PlayerPrefs.HasKey("ReplayGameID"))
        {
            PlayerPrefs.DeleteKey("ReplayGameID");
        }

        if (!PlayerPrefs.HasKey("PlayerID"))
        {
            string email = File.ReadAllText(Application.persistentDataPath + "/loginData.json");
            int playerId = GetPlayerIdFromEmail(email);
            PlayerPrefs.SetInt("PlayerID", playerId);
        }

        int currentId = PlayerPrefs.GetInt("PlayerID");
        PlayerPrefs.SetInt("Player2ID", currentId); // Устанавливаем второго игрока = первому

        SceneManager.LoadScene("ChessGameScene");
    }

    private int GetPlayerIdFromEmail(string email)
    {
        int playerId = -1;

        using (SqlConnection conn = new SqlConnection("Server=localhost;Database=ChessDB;Integrated Security=True;"))
        {
            conn.Open();
            string query = "SELECT PlayerID FROM Players WHERE Email = @Email";
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                object result = cmd.ExecuteScalar();
                if (result != null)
                {
                    playerId = Convert.ToInt32(result);
                }
            }
        }

        return playerId;
    }
    public void OpenGameHistory()
    {
        SceneManager.LoadScene("GameHistoryScene");
    }

    public void Exit()
    {
        PlayerPrefs.DeleteKey("PlayerID");
        string path = Application.persistentDataPath + "/loginData.json";
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        SceneManager.LoadScene("LoginScene");
    }
}