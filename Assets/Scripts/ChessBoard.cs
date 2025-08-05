using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChessBoard : MonoBehaviour
{
    public GameObject cellPrefab;
    public GameObject textPrefab;
    public GameObject whitePawnPrefab;
    public GameObject blackPawnPrefab;
    public GameObject whiteKingPrefab;
    public GameObject blackKingPrefab;
    public GameObject whiteQueenPrefab;
    public GameObject blackQueenPrefab;
    public GameObject whiteBishopPrefab;
    public GameObject blackBishopPrefab;
    public GameObject whiteKnightPrefab;
    public GameObject blackKnightPrefab;
    public GameObject whiteRookPrefab;
    public GameObject blackRookPrefab;

    //public Button menuButton;

    public int boardSize = 8;
    public float cellSize = 1f;

    private GameObject piecesContainer;

    void Start()
    {
        
            GenerateBoard();
            GenerateCoordinates();
            PlacePieces();
    }
    Vector2 ParseCoord(string notation)
    {
        int x = notation[0] - 'a';                    // 'e' -> 4
        int y = int.Parse(notation[1].ToString()) - 1; // '2' -> 1
        return new Vector2(x, y);
    }
    GameObject GetPieceAt(Vector2 boardPos)
    {
        foreach (Transform piece in piecesContainer.transform)
        {
            Vector2 pos = WorldToBoard(piece.position);
            if (pos == boardPos)
                return piece.gameObject;
        }
        return null;
    }
    Vector2 WorldToBoard(Vector3 worldPos)
    {
        Vector3 offset = new Vector3(-boardSize * cellSize / 2f + cellSize / 2f,
                                     -boardSize * cellSize / 2f + cellSize / 2f,
                                     0);
        Vector3 local = worldPos - offset;
        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);
        return new Vector2(x, y);
    }
    public void ApplyMove(string move)
    {
        Vector2 from = ParseCoord(move.Substring(0, 2));
        Vector2 to = ParseCoord(move.Substring(2, 2));
        GameObject piece = GetPieceAt(from);
        if (piece != null)
        {
            GameObject captured = GetPieceAt(to);
            if (captured != null) Destroy(captured); // съедаем фигуру
            MovePiece(piece, to);
        }
    }
    public void ResetBoard()
    {
        foreach (Transform child in piecesContainer.transform)
        {
            Destroy(child.gameObject);
        }
        PlacePieces();
    }
    void MovePiece(GameObject piece, Vector2 boardPos)
    {
        Vector3 offset = new Vector3(-boardSize * cellSize / 2f + cellSize / 2f,
                                     -boardSize * cellSize / 2f + cellSize / 2f,
                                     0);
        Vector3 newPos = new Vector3(boardPos.x * cellSize, boardPos.y * cellSize, -0.1f) + offset;
        piece.transform.position = newPos;
    }
    public void Menu()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void GenerateBoard()
    {
        Vector3 offset = new Vector3(-boardSize * cellSize / 2f + cellSize / 2f,
                                     -boardSize * cellSize / 2f + cellSize / 2f,
                                     0);

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                GameObject cell = Instantiate(cellPrefab, new Vector3(x * cellSize, y * cellSize, 0) + offset, Quaternion.identity);
                cell.transform.parent = transform;
                SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
                sr.color = (x + y) % 2 == 0 ? Color.white : Color.black;
            }
        }
    }

    public void GenerateCoordinates()
    {
        string[] letters = { "A", "B", "C", "D", "E", "F", "G", "H" };
        float padding = cellSize * 0.8f;

        Vector3 offset = new Vector3(-boardSize * cellSize / 2f + cellSize / 2f,
                                     -boardSize * cellSize / 2f + cellSize / 2f,
                                     0);

        for (int i = 0; i < boardSize; i++)
        {
            CreateText(letters[i], new Vector3(i * cellSize, -padding - cellSize * 0.3f, 0) + offset);
            CreateText((i + 1).ToString(), new Vector3(-padding - cellSize * 0.3f, i * cellSize, 0) + offset);
        }
    }

    void CreateText(string text, Vector3 position)
    {
        GameObject textObj = Instantiate(textPrefab, transform);
        textObj.GetComponent<TextMeshPro>().text = text;
        textObj.transform.position = position;
        textObj.transform.localScale = Vector3.one * 0.3f;
    }

    public void PlacePieces()
    {
        piecesContainer = new GameObject("PiecesContainer");
        Vector3 offset = new Vector3(-boardSize * cellSize / 2f + cellSize / 2f,
                                     -boardSize * cellSize / 2f + cellSize / 2f,
                                     0);

        float pieceScale = cellSize * 0.25f;
        float pieceZ = -0.1f;

        for (int i = 0; i < boardSize; i++)
        {
            Vector3 whitePos = new Vector3(i * cellSize, 1 * cellSize, pieceZ) + offset;
            Vector3 blackPos = new Vector3(i * cellSize, 6 * cellSize, pieceZ) + offset;

            GameObject wp = Instantiate(whitePawnPrefab, whitePos, Quaternion.identity, piecesContainer.transform);
            GameObject bp = Instantiate(blackPawnPrefab, blackPos, Quaternion.identity, piecesContainer.transform);

            wp.transform.localScale = Vector3.one * pieceScale;
            bp.transform.localScale = Vector3.one * pieceScale;
        }

        GameObject[] whitePieces = { whiteRookPrefab, whiteKnightPrefab, whiteBishopPrefab, whiteQueenPrefab, whiteKingPrefab, whiteBishopPrefab, whiteKnightPrefab, whiteRookPrefab };
        GameObject[] blackPieces = { blackRookPrefab, blackKnightPrefab, blackBishopPrefab, blackQueenPrefab, blackKingPrefab, blackBishopPrefab, blackKnightPrefab, blackRookPrefab };

        for (int i = 0; i < boardSize; i++)
        {
            Vector3 whitePos = new Vector3(i * cellSize, 0 * cellSize, pieceZ) + offset;
            Vector3 blackPos = new Vector3(i * cellSize, 7 * cellSize, pieceZ) + offset;

            GameObject whitePiece = Instantiate(whitePieces[i], whitePos, Quaternion.identity, piecesContainer.transform);
            GameObject blackPiece = Instantiate(blackPieces[i], blackPos, Quaternion.identity, piecesContainer.transform);

            whitePiece.transform.localScale = Vector3.one * pieceScale;
            blackPiece.transform.localScale = Vector3.one * pieceScale;
        }
    }
}