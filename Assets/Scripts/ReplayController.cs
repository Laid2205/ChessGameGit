using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class ReplayController : MonoBehaviour
{
    public Button nextButton;
    public Button prevButton;
    public Button exitButton;

    private List<string> moves;
    private int currentMoveIndex = -1;

    private ChessBoard chessBoard;

    void Start()
    {
        chessBoard = FindObjectOfType<ChessBoard>();
        nextButton.onClick.AddListener(NextMove);
        prevButton.onClick.AddListener(PreviousMove);
        exitButton.onClick.AddListener(() => SceneManager.LoadScene("MenuScene"));

        if (PlayerPrefs.HasKey("ReplayGameID"))
        {
            int replayGameId = PlayerPrefs.GetInt("ReplayGameID");

            chessBoard.GenerateBoard();
            chessBoard.GenerateCoordinates();
            chessBoard.PlacePieces();
        }
    }

    void NextMove()
    {
        if (moves == null || currentMoveIndex + 1 >= moves.Count)
            return;

        currentMoveIndex++;
        string move = moves[currentMoveIndex];
        chessBoard.ApplyMove(move);
    }

    void PreviousMove()
    {
        if (currentMoveIndex < 0)
            return;

        currentMoveIndex--;
        chessBoard.ResetBoard();
        for (int i = 0; i <= currentMoveIndex; i++)
        {
            chessBoard.ApplyMove(moves[i]);
        }
    }
}
