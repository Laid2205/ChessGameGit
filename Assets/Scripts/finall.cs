using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class final : MonoBehaviour
{
    private GameObject selectedPiece;
    private float cellSize = 1f;
    private Vector3 boardOffset;
    private Color originalColor;
    private SpriteRenderer selectedRenderer;

    private List<GameObject> highlightMarkers = new List<GameObject>();
    private Color normalMoveColor = new Color(0f, 0.8f, 0f, 0.7f); // Green for normal moves
    private Color attackMoveColor = new Color(0.9f, 0f, 0f, 0.7f); // Red for attack moves

    private Dictionary<GameObject, bool> hasMoved = new Dictionary<GameObject, bool>();
    private Dictionary<GameObject, Vector2> capturablePieces = new Dictionary<GameObject, Vector2>();
    private bool isWhiteTurn = true; // White starts first

    public GameObject checkPanel;
    public Button checkButton;
    public Text checkText;

    [SerializeField] private GameObject promotionUI;
    [SerializeField] private GameObject queenButton;
    [SerializeField] private GameObject rookButton;
    [SerializeField] private GameObject bishopButton;
    [SerializeField] private GameObject knightButton;

    [SerializeField] private GameObject whiteQueenPrefab;
    [SerializeField] private GameObject whiteRookPrefab;
    [SerializeField] private GameObject whiteBishopPrefab;
    [SerializeField] private GameObject whiteKnightPrefab;
    [SerializeField] private GameObject blackQueenPrefab;
    [SerializeField] private GameObject blackRookPrefab;
    [SerializeField] private GameObject blackBishopPrefab;
    [SerializeField] private GameObject blackKnightPrefab;

    private GameObject pawnToPromote;
    private Vector3 promotionPosition;
    private bool isPromotionInProgress = false;

    void Start()
    {
        boardOffset = new Vector3(-3.5f * cellSize, -3.5f * cellSize, 0);

        if (promotionUI != null)
        {
            promotionUI.SetActive(false);
        }
        checkPanel.SetActive(false);
        checkButton.onClick.AddListener(CloseCheckMessage);
    }
    private bool isCheckDisplayed = false; // Флаг для контроля вывода шаха
    void Update()
    {
        bool kingInCheck = IsKingInCheck(isWhiteTurn);
        

        if (isPromotionInProgress)
        {
            return;
        }
        
        if (kingInCheck && !isCheckDisplayed)
        {
            ShowCheckMessage(isWhiteTurn);
            isCheckDisplayed = true; // Отмечаем, что сообщение уже показано
        }
        else if (!kingInCheck)
        {
            isCheckDisplayed = false; // Сбрасываем флаг, когда шах пропадает
        }

        if (kingInCheck && !isCheckDisplayed)
        {
            ShowCheckMessage(isWhiteTurn);
            isCheckDisplayed = true;
        }
        else if (!kingInCheck && isCheckDisplayed)
        {
            isCheckDisplayed = false;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("ChessPiece"))
            {
                GameObject clickedPiece = hit.collider.gameObject;
                bool isClickedPieceWhite = IsWhitePiece(clickedPiece);

                if (isClickedPieceWhite != isWhiteTurn)
                {
                    if (selectedPiece != null && capturablePieces.ContainsKey(clickedPiece))
                    {
                        CapturePiece(clickedPiece);
                    }
                    return;
                }

                if (selectedPiece == clickedPiece)
                {
                    DeselectPiece();
                }
                else
                {
                    SelectPiece(clickedPiece);
                }
            }
            else if (selectedPiece != null)
            {
                TryMovePiece(mousePosition);
            }
        }
    }
    private string GenerateMoveNotation(Vector2 fromPos, Vector2 toPos)
    {
        char fromFile = (char)('a' + fromPos.x);
        int fromRank = (int)(fromPos.y + 1);
        char toFile = (char)('a' + toPos.x);
        int toRank = (int)(toPos.y + 1);

        return $"{fromFile}{fromRank}{toFile}{toRank}";
    }

    private void RecordMove(GameObject piece, Vector2 fromPos, Vector2 toPos)
    {
        if (PlayerPrefs.HasKey("ReplayGameID"))
            return;

        int gameId = PlayerPrefs.GetInt("CurrentGameID", -1);
        if (gameId == -1)
            return;

        string moveNotation = GenerateMoveNotation(fromPos, toPos);

        DatabaseManager.RecordMove(gameId, moveNotation);

        Debug.Log($"Move recorded: {moveNotation} for game {gameId}");
    }
    void ShowCheckMessage(bool isWhite)
    {
        checkText.text = (isWhite ? "Белый" : "Черный") + " король под шахом!";
        checkPanel.SetActive(true);
    }
    void ShowCheckmateMessage(bool isWhite)
    {
        checkText.text = (isWhite ? "Белые" : "Черные") + " проиграли!";
        checkButton.onClick.RemoveAllListeners();
        checkButton.onClick.AddListener(LoadMainMenu);
        checkPanel.SetActive(true);
    }
    public void CloseCheckMessage()
    {
        checkPanel.SetActive(false);
    }
    void LoadMainMenu()
    {
        SceneManager.LoadScene("MenuScene");
    }
    bool IsKingInCheck(bool isWhite)
    {
        
        GameObject king = FindKing(isWhite);
        if (king == null) return false;
        Vector2 kingPos = new Vector2(Mathf.RoundToInt((king.transform.position.x - boardOffset.x) / cellSize),
                                      Mathf.RoundToInt((king.transform.position.y - boardOffset.y) / cellSize));

        return IsUnderAttack(kingPos, isWhite);
    }
    

    GameObject FindKing(bool isWhite)
    {
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("ChessPiece");
        foreach (GameObject piece in pieces)
        {
            if (IsKing(piece) && IsWhitePiece(piece) == isWhite)
            {
                return piece;
            }
        }
        return null;
    }

    bool IsUnderAttack(Vector2 position, bool isWhite)
    {
        GameObject[] pieces = GameObject.FindGameObjectsWithTag("ChessPiece");
        foreach (GameObject piece in pieces)
        {
            if (IsWhitePiece(piece) != isWhite)
            {
                List<Vector2> attackMoves = GetPieceAttackMoves(piece);
                if (attackMoves.Contains(position))
                {
                    return true;
                }
            }
        }
        return false;
    }
    List<Vector2> GetPieceAttackMoves(GameObject piece)
    {
        List<Vector2> moves = new List<Vector2>();
        Vector2 piecePos = new Vector2(
            Mathf.RoundToInt((piece.transform.position.x - boardOffset.x) / cellSize),
            Mathf.RoundToInt((piece.transform.position.y - boardOffset.y) / cellSize)
        );

        if (IsPawn(piece))
        {
            int direction = IsWhitePiece(piece) ? 1 : -1;
            moves.Add(new Vector2(piecePos.x - 1, piecePos.y + direction));
            moves.Add(new Vector2(piecePos.x + 1, piecePos.y + direction));
        }
        else if (IsKnight(piece))
        {
            int[] dx = { -2, -1, 1, 2, 2, 1, -1, -2 };
            int[] dy = { 1, 2, 2, 1, -1, -2, -2, -1 };
            for (int i = 0; i < 8; i++)
            {
                moves.Add(new Vector2(piecePos.x + dx[i], piecePos.y + dy[i]));
            }
        }
        else if (IsBishop(piece))
        {
            AddDiagonalMoves(piecePos, moves);
        }
        else if (IsRook(piece))
        {
            AddStraightMoves(piecePos, moves);
        }
        else if (IsQueen(piece))
        {
            AddDiagonalMoves(piecePos, moves);
            AddStraightMoves(piecePos, moves);
        }
        else if (IsKing(piece))
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx != 0 || dy != 0)
                        moves.Add(new Vector2(piecePos.x + dx, piecePos.y + dy));
                }
            }
        }

        return moves;
    }
    void AddDiagonalMoves(Vector2 piecePos, List<Vector2> moves)
    {
        int[] directions = { -1, 1 };
        foreach (int dx in directions)
        {
            foreach (int dy in directions)
            {
                for (int i = 1; i < 8; i++)
                {
                    Vector2 newPos = new Vector2(piecePos.x + i * dx, piecePos.y + i * dy);
                    if (IsValidBoardPosition(newPos))
                    {
                        moves.Add(newPos);
                        if (IsOccupied(newPos)) break;
                    }
                }
            }
        }
    }

    void AddStraightMoves(Vector2 piecePos, List<Vector2> moves)
    {
        int[] directions = { -1, 1 };
        foreach (int d in directions)
        {
            for (int i = 1; i < 8; i++)
            {
                Vector2 newX = new Vector2(piecePos.x + i * d, piecePos.y);
                Vector2 newY = new Vector2(piecePos.x, piecePos.y + i * d);
                if (IsValidBoardPosition(newX))
                {
                    moves.Add(newX);
                    if (IsOccupied(newX)) break;
                }
                if (IsValidBoardPosition(newY))
                {
                    moves.Add(newY);
                    if (IsOccupied(newY)) break;
                }
            }
        }
    }

    #region Piece Type Checks
    bool IsPawn(GameObject piece)
    {
        return piece.name.ToLower().Contains("pawn");
    }

    bool IsKnight(GameObject piece)
    {
        return piece.name.ToLower().Contains("knight");
    }

    bool IsBishop(GameObject piece)
    {
        return piece.name.ToLower().Contains("bishop");
    }

    bool IsRook(GameObject piece)
    {
        return piece.name.ToLower().Contains("rook");
    }

    bool IsQueen(GameObject piece)
    {
        return piece.name.ToLower().Contains("queen");
    }

    bool IsKing(GameObject piece)
    {
        return piece.name.ToLower().Contains("king");
    }
    #endregion

    void SelectPiece(GameObject piece)
    {
        DeselectPiece();
        selectedPiece = piece;
        selectedRenderer = selectedPiece.GetComponent<SpriteRenderer>();
        originalColor = selectedRenderer.color;
        selectedRenderer.color = Color.yellow;

        if (!hasMoved.ContainsKey(piece))
        {
            hasMoved[piece] = false;
        }

        capturablePieces.Clear();

        ShowValidMoves();
    }

    void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            selectedRenderer.color = originalColor;
            selectedPiece = null;
        }

        ClearHighlights();
        capturablePieces.Clear();
    }

    void ShowValidMoves()
    {
        ClearHighlights();

        if (IsPawn(selectedPiece))
        {
            ShowPawnMoves();
        }
        else if (IsKnight(selectedPiece))
        {
            ShowKnightMoves();
        }
        else if (IsBishop(selectedPiece))
        {
            ShowBishopMoves();
        }
        else if (IsRook(selectedPiece))
        {
            ShowRookMoves();
        }
        else if (IsQueen(selectedPiece))
        {
            ShowQueenMoves();
        }
        else if (IsKing(selectedPiece))
        {
            ShowKingMoves();
        }
    }

    #region Show Moves Functions for Each Piece Type
    void ShowRookMoves()
    {
        Vector2 rookPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((rookPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((rookPosition.y - boardOffset.y) / cellSize);
        bool isWhiteRook = IsWhitePiece(selectedPiece);

        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),   // Up
            new Vector2(1, 0),   // Right
            new Vector2(0, -1),  // Down
            new Vector2(-1, 0)   // Left
        };

        foreach (Vector2 direction in directions)
        {
            for (int distance = 1; distance <= 7; distance++)
            {
                Vector2 targetPos = new Vector2(currentX + direction.x * distance, currentY + direction.y * distance);

                if (!IsValidBoardPosition(targetPos))
                {
                    break; // Hit the edge of the board
                }

                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                if (pieceAtTarget == null)
                {
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    if (IsWhitePiece(pieceAtTarget) != isWhiteRook)
                    {
                        CreateHighlight(targetPos, attackMoveColor);
                        capturablePieces[pieceAtTarget] = targetPos;
                    }
                    break; // Can't move past pieces
                }
            }
        }
    }

    void ShowKnightMoves()
    {
        Vector2 knightPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((knightPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((knightPosition.y - boardOffset.y) / cellSize);
        bool isWhiteKnight = IsWhitePiece(selectedPiece);

        Vector2[] knightMoves = new Vector2[]
        {
            new Vector2(1, 2), new Vector2(2, 1),
            new Vector2(-1, 2), new Vector2(-2, 1),
            new Vector2(1, -2), new Vector2(2, -1),
            new Vector2(-1, -2), new Vector2(-2, -1)
        };

        foreach (Vector2 move in knightMoves)
        {
            Vector2 targetPos = new Vector2(currentX + move.x, currentY + move.y);

            if (IsValidBoardPosition(targetPos))
            {
                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                if (pieceAtTarget == null)
                {
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else if (IsWhitePiece(pieceAtTarget) != isWhiteKnight)
                {
                    CreateHighlight(targetPos, attackMoveColor);
                    capturablePieces[pieceAtTarget] = targetPos;
                }
            }
        }
    }

    void ShowBishopMoves()
    {
        Vector2 bishopPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((bishopPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((bishopPosition.y - boardOffset.y) / cellSize);
        bool isWhiteBishop = IsWhitePiece(selectedPiece);

        Vector2[] directions = new Vector2[]
        {
            new Vector2(1, 1),   // Up-right
            new Vector2(1, -1),  // Down-right
            new Vector2(-1, 1),  // Up-left
            new Vector2(-1, -1)  // Down-left
        };

        foreach (Vector2 direction in directions)
        {
            for (int distance = 1; distance <= 7; distance++)
            {
                Vector2 targetPos = new Vector2(currentX + direction.x * distance, currentY + direction.y * distance);

                if (!IsValidBoardPosition(targetPos))
                {
                    break; // Hit the edge of the board
                }

                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                if (pieceAtTarget == null)
                {
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    if (IsWhitePiece(pieceAtTarget) != isWhiteBishop)
                    {
                        CreateHighlight(targetPos, attackMoveColor);
                        capturablePieces[pieceAtTarget] = targetPos;
                    }
                    break; // Can't move past pieces
                }
            }
        }
    }

    void ShowQueenMoves()
    {
        Vector2 queenPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((queenPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((queenPosition.y - boardOffset.y) / cellSize);
        bool isWhiteQueen = IsWhitePiece(selectedPiece);

        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),   // Up
            new Vector2(1, 1),   // Up-right
            new Vector2(1, 0),   // Right
            new Vector2(1, -1),  // Down-right
            new Vector2(0, -1),  // Down
            new Vector2(-1, -1), // Down-left
            new Vector2(-1, 0),  // Left
            new Vector2(-1, 1)   // Up-left
        };

        foreach (Vector2 direction in directions)
        {
            for (int distance = 1; distance <= 7; distance++)
            {
                Vector2 targetPos = new Vector2(currentX + direction.x * distance, currentY + direction.y * distance);

                if (!IsValidBoardPosition(targetPos))
                {
                    break; // Hit the edge of the board
                }

                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                if (pieceAtTarget == null)
                {
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    if (IsWhitePiece(pieceAtTarget) != isWhiteQueen)
                    {
                        CreateHighlight(targetPos, attackMoveColor);
                        capturablePieces[pieceAtTarget] = targetPos;
                    }
                    break; // Can't move past pieces
                }
            }
        }
    }

    void ShowKingMoves()
    {
        Vector2 kingPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((kingPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((kingPosition.y - boardOffset.y) / cellSize);
        bool isWhiteKing = IsWhitePiece(selectedPiece);

        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),   // Up
            new Vector2(1, 1),   // Up-right
            new Vector2(1, 0),   // Right
            new Vector2(1, -1),  // Down-right
            new Vector2(0, -1),  // Down
            new Vector2(-1, -1), // Down-left
            new Vector2(-1, 0),  // Left
            new Vector2(-1, 1)   // Up-left
        };

        foreach (Vector2 direction in directions)
        {
            Vector2 targetPos = new Vector2(currentX + direction.x, currentY + direction.y);

            if (IsValidBoardPosition(targetPos))
            {
                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                // Проверяем, не под атакой ли клетка
                if (!IsUnderAttack(targetPos, isWhiteKing))
                {
                    if (pieceAtTarget == null)
                    {
                        CreateHighlight(targetPos, normalMoveColor);
                    }
                    else if (IsWhitePiece(pieceAtTarget) != isWhiteKing)
                    {
                        CreateHighlight(targetPos, attackMoveColor);
                        capturablePieces[pieceAtTarget] = targetPos;
                    }
                }
            }
        }

        if (!hasMoved[selectedPiece])
        {
            CheckCastling(currentX, currentY, isWhiteKing);
        }
    }

    void CheckCastling(int kingX, int kingY, bool isWhiteKing)
    {
        CheckCastlingSide(kingX, kingY, 1, isWhiteKing);

        CheckCastlingSide(kingX, kingY, -1, isWhiteKing);
    }

    void CheckCastlingSide(int kingX, int kingY, int direction, bool isWhiteKing)
    {
        int rookX = direction == 1 ? 7 : 0;
        int castleDistance = direction == 1 ? 2 : 3; // King moves 2 squares for kingside, 3 for queenside

        Vector2 rookPos = new Vector2(rookX, kingY);
        GameObject rook = GetPieceAtPosition(rookPos);

        if (rook != null && IsRook(rook) && IsWhitePiece(rook) == isWhiteKing &&
            hasMoved.ContainsKey(rook) && !hasMoved[rook])
        {
            bool pathClear = true;

            for (int x = kingX + direction; direction > 0 ? x < rookX : x > rookX; x += direction)
            {
                if (IsOccupied(new Vector2(x, kingY)))
                {
                    pathClear = false;
                    break;
                }
            }

            if (pathClear)
            {
                Vector2 castlePos = new Vector2(kingX + castleDistance * direction, kingY);
                CreateHighlight(castlePos, normalMoveColor);
            }
        }
    }

    void ShowPawnMoves()
    {
        Vector2 pawnPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((pawnPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((pawnPosition.y - boardOffset.y) / cellSize);

        bool isWhitePawn = IsWhitePiece(selectedPiece);
        int direction = isWhitePawn ? 1 : -1; // White pawns move up, black pawns move down

        Vector2 forwardPos = new Vector2(currentX, currentY + direction);
        if (IsValidBoardPosition(forwardPos) && !IsOccupied(forwardPos))
        {
            CreateHighlight(forwardPos, normalMoveColor);

            if (!hasMoved[selectedPiece])
            {
                Vector2 doubleForwardPos = new Vector2(currentX, currentY + 2 * direction);
                if (IsValidBoardPosition(doubleForwardPos) && !IsOccupied(doubleForwardPos))
                {
                    CreateHighlight(doubleForwardPos, normalMoveColor);
                }
            }
        }

        CheckAndHighlightCapture(new Vector2(currentX - 1, currentY + direction), isWhitePawn);
        CheckAndHighlightCapture(new Vector2(currentX + 1, currentY + direction), isWhitePawn);
    }
    #endregion

    void CheckAndHighlightCapture(Vector2 capturePos, bool isWhitePiece)
    {
        if (IsValidBoardPosition(capturePos))
        {
            GameObject enemyPiece = GetPieceAtPosition(capturePos);
            if (enemyPiece != null && IsWhitePiece(enemyPiece) != isWhitePiece)
            {
                CreateHighlight(capturePos, attackMoveColor);

                capturablePieces[enemyPiece] = capturePos;
            }
        }
    }

    bool IsWhitePiece(GameObject piece)
    {
        return piece.name.ToLower().Contains("white");
    }

    bool IsValidBoardPosition(Vector2 boardPos)
    {
        return boardPos.x >= 0 && boardPos.x <= 7 && boardPos.y >= 0 && boardPos.y <= 7;
    }

    bool IsOccupied(Vector2 boardPos)
    {
        return GetPieceAtPosition(boardPos) != null;
    }

    GameObject GetPieceAtPosition(Vector2 boardPos)
    {
        Vector3 worldPos = new Vector3(boardPos.x * cellSize, boardPos.y * cellSize, -0.1f) + boardOffset;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPos, 0.4f);

        foreach (Collider2D collider in colliders)
        {
            if (collider.CompareTag("ChessPiece"))
            {
                return collider.gameObject;
            }
        }

        return null;
    }

    void CreateHighlight(Vector2 boardPos, Color color)
    {
        Vector3 worldPos = new Vector3(boardPos.x * cellSize, boardPos.y * cellSize, -0.05f) + boardOffset;
        GameObject highlight = new GameObject("MoveHighlight");
        highlight.transform.position = worldPos;
        SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateHighlightSprite();
        renderer.color = color;

        BoxCollider2D collider = highlight.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.9f);
        collider.isTrigger = true;

        highlightMarkers.Add(highlight);
    }

    Sprite CreateHighlightSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color fillColor = Color.white;
        Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                if (x < 3 || x >= texture.width - 3 || y < 3 || y >= texture.height - 3)
                {
                    texture.SetPixel(x, y, borderColor);
                }
                else
                {
                    texture.SetPixel(x, y, fillColor);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    void ClearHighlights()
    {
        foreach (GameObject highlight in highlightMarkers)
        {
            Destroy(highlight);
        }
        highlightMarkers.Clear();
    }

    void CapturePiece(GameObject enemyPiece)
    {
        if (!capturablePieces.ContainsKey(enemyPiece)) return;

        Vector2 targetPos = capturablePieces[enemyPiece];
        Vector3 targetWorldPos = new Vector3(targetPos.x * cellSize, targetPos.y * cellSize, -0.1f) + boardOffset;

        Vector2 fromPos = new Vector2(
            Mathf.RoundToInt((selectedPiece.transform.position.x - boardOffset.x) / cellSize),
            Mathf.RoundToInt((selectedPiece.transform.position.y - boardOffset.y) / cellSize)
        );

        selectedPiece.transform.position = targetWorldPos;
        enemyPiece.SetActive(false);

        RecordMove(selectedPiece, fromPos, targetPos);

        hasMoved[selectedPiece] = true;

        if (IsKing(enemyPiece))
        {
            int gameId = PlayerPrefs.GetInt("CurrentGameID", -1);
            int playerId = PlayerPrefs.GetInt("PlayerID", -1);
            if (gameId != -1 && playerId != -1)
            {
                DatabaseManager.EndGame(gameId, playerId);
            }

            ShowCheckmateMessage(IsWhitePiece(enemyPiece));
        }
        else if (IsPawn(selectedPiece) && ShouldPromotePawn(targetPos))
        {
            StartPromotion(selectedPiece, targetWorldPos);
        }
        else
        {
            isWhiteTurn = !isWhiteTurn;
        }
        DeselectPiece();
    }

    void TryMovePiece(Vector2 mousePosition)
    {
        if (selectedPiece == null) return;

        int targetX = Mathf.RoundToInt((mousePosition.x - boardOffset.x) / cellSize);
        int targetY = Mathf.RoundToInt((mousePosition.y - boardOffset.y) / cellSize);
        Vector2 targetBoardPos = new Vector2(targetX, targetY);

        bool validMoveFound = false;
        foreach (GameObject highlight in highlightMarkers)
        {
            Vector2 highlightBoardPos = new Vector2(
                Mathf.RoundToInt((highlight.transform.position.x - boardOffset.x) / cellSize),
                Mathf.RoundToInt((highlight.transform.position.y - boardOffset.y) / cellSize)
            );

            if (highlightBoardPos == targetBoardPos)
            {
                validMoveFound = true;

                Vector2 fromPos = new Vector2(
                    Mathf.RoundToInt((selectedPiece.transform.position.x - boardOffset.x) / cellSize),
                    Mathf.RoundToInt((selectedPiece.transform.position.y - boardOffset.y) / cellSize)
                );

                GameObject pieceAtTarget = GetPieceAtPosition(targetBoardPos);
                if (pieceAtTarget != null && pieceAtTarget != selectedPiece)
                {
                    pieceAtTarget.SetActive(false);
                }

                if (IsKing(selectedPiece) && !hasMoved[selectedPiece])
                {
                    int currentX = Mathf.RoundToInt((selectedPiece.transform.position.x - boardOffset.x) / cellSize);
                    if (Mathf.Abs(targetX - currentX) == 2)
                    {
                        HandleCastling(currentX, targetX, targetY);
                    }
                }

                Vector3 targetWorldPos = new Vector3(targetX * cellSize, targetY * cellSize, -0.1f) + boardOffset;
                selectedPiece.transform.position = targetWorldPos;

                RecordMove(selectedPiece, fromPos, targetBoardPos);

                hasMoved[selectedPiece] = true;

                if (IsPawn(selectedPiece) && ShouldPromotePawn(targetBoardPos))
                {
                    StartPromotion(selectedPiece, targetWorldPos);
                }
                else
                {
                    isWhiteTurn = !isWhiteTurn;
                }
                break;
            }
        }

        DeselectPiece();
    }

    void HandleCastling(int fromX, int toX, int y)
    {
        bool isKingside = toX > fromX;

        int rookX = isKingside ? 7 : 0;
        Vector2 rookPos = new Vector2(rookX, y);
        GameObject rook = GetPieceAtPosition(rookPos);

        if (rook != null && IsRook(rook))
        {
            int newRookX = isKingside ? toX - 1 : toX + 1;
            Vector3 newRookWorldPos = new Vector3(newRookX * cellSize, y * cellSize, -0.1f) + boardOffset;

            rook.transform.position = newRookWorldPos;
            hasMoved[rook] = true;
        }
    }

    #region Pawn Promotion
    bool ShouldPromotePawn(Vector2 boardPos)
    {
        bool isWhite = IsWhitePiece(selectedPiece);
        return IsPawn(selectedPiece) && ((isWhite && boardPos.y == 7) || (!isWhite && boardPos.y == 0));
    }

    void StartPromotion(GameObject pawn, Vector3 position)
    {
        if (promotionUI == null)
        {
            Debug.LogError("Promotion UI not assigned! Can't promote pawn.");
            isWhiteTurn = !isWhiteTurn;
            return;
        }

        isPromotionInProgress = true;
        pawnToPromote = pawn;
        promotionPosition = position;

        promotionUI.SetActive(true);
    }

    public void PromoteTo(string pieceType)
    {
        if (!isPromotionInProgress || pawnToPromote == null)
        {
            return;
        }

        bool isWhite = IsWhitePiece(pawnToPromote);
        GameObject newPiece = null;

        switch (pieceType.ToLower())
        {
            case "queen":
                newPiece = isWhite ? whiteQueenPrefab : blackQueenPrefab;
                break;
            case "rook":
                newPiece = isWhite ? whiteRookPrefab : blackRookPrefab;
                break;
            case "bishop":
                newPiece = isWhite ? whiteBishopPrefab : blackBishopPrefab;
                break;
            case "knight":
                newPiece = isWhite ? whiteKnightPrefab : blackKnightPrefab;
                break;
            default:
                Debug.LogError("Unknown piece type for promotion: " + pieceType);
                break;
        }

        if (newPiece != null)
        {
            float pieceScale = cellSize * 0.25f;
            GameObject promotedPiece = Instantiate(newPiece, promotionPosition, Quaternion.identity);

            promotedPiece.transform.localScale = Vector3.one * pieceScale;
            promotedPiece.tag = "ChessPiece";

            Vector2 promotionPos = new Vector2(
                Mathf.RoundToInt((promotionPosition.x - boardOffset.x) / cellSize),
                Mathf.RoundToInt((promotionPosition.y - boardOffset.y) / cellSize)
            );


            int gameId = PlayerPrefs.GetInt("CurrentGameID", -1);
            if (gameId != -1)
            {
                char file = (char)('a' + promotionPos.x);
                int rank = (int)(promotionPos.y + 1);
                string promotionNotation = $"{file}{rank}={pieceType[0]}";
                DatabaseManager.RecordMove(gameId, promotionNotation);
            }

            Destroy(pawnToPromote);
        }

        promotionUI.SetActive(false);

        isPromotionInProgress = false;
        pawnToPromote = null;

        isWhiteTurn = !isWhiteTurn;
    }
    public void PromoteToQueen() { PromoteTo("queen"); }
    public void PromoteToRook() { PromoteTo("rook"); }
    public void PromoteToBishop() { PromoteTo("bishop"); }
    public void PromoteToKnight() { PromoteTo("knight"); }
    #endregion
}