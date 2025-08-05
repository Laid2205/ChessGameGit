using System.Collections.Generic;
using UnityEngine;

public class ChessPieceMovementController : MonoBehaviour
{
    private GameObject selectedPiece;
    private float cellSize = 1f;
    private Vector3 boardOffset;
    private Color originalColor;
    private SpriteRenderer selectedRenderer;

    // For highlighting valid moves
    private List<GameObject> highlightMarkers = new List<GameObject>();
    private Color normalMoveColor = new Color(0f, 0.8f, 0f, 0.7f); // Green for normal moves
    private Color attackMoveColor = new Color(0.9f, 0f, 0f, 0.7f); // Red for attack moves

    private Dictionary<GameObject, bool> hasMoved = new Dictionary<GameObject, bool>();

    private Dictionary<GameObject, Vector2> capturablePieces = new Dictionary<GameObject, Vector2>();

    private bool isWhiteTurn = true; // White starts first

    void Start()
    {
        boardOffset = new Vector3(-3.5f * cellSize, -3.5f * cellSize, 0);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("ChessPiece"))
            {
                GameObject clickedPiece = hit.collider.gameObject;
                bool isClickedPieceWhite = IsWhitePiece(clickedPiece);

                // Check if it's this player's turn
                if (isClickedPieceWhite != isWhiteTurn)
                {
                    // It's not this player's turn - check if clicked piece is capturable
                    if (selectedPiece != null && capturablePieces.ContainsKey(clickedPiece))
                    {
                        // Capture the enemy piece and move to its position
                        CapturePiece(clickedPiece);
                    }
                    return;
                }

                // Normal selection logic for the current player's pieces
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

    void SelectPiece(GameObject piece)
    {
        DeselectPiece();
        selectedPiece = piece;
        selectedRenderer = selectedPiece.GetComponent<SpriteRenderer>();
        originalColor = selectedRenderer.color;
        selectedRenderer.color = Color.yellow;

        // Make sure the piece is in our tracking dictionary
        if (!hasMoved.ContainsKey(piece))
        {
            hasMoved[piece] = false;
        }

        // Clear the capturable pieces dictionary
        capturablePieces.Clear();

        // Show valid moves based on piece type
        ShowValidMoves();
    }

    void DeselectPiece()
    {
        if (selectedPiece != null)
        {
            selectedRenderer.color = originalColor;
            selectedPiece = null;
        }

        // Clear highlight markers
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

    void ShowRookMoves()
    {
        Vector2 rookPosition = selectedPiece.transform.position;
        int currentX = Mathf.RoundToInt((rookPosition.x - boardOffset.x) / cellSize);
        int currentY = Mathf.RoundToInt((rookPosition.y - boardOffset.y) / cellSize);
        bool isWhiteRook = IsWhitePiece(selectedPiece);

        // The 4 orthogonal directions for rook movement
        Vector2[] directions = new Vector2[]
        {
            new Vector2(0, 1),   // Up
            new Vector2(1, 0),   // Right
            new Vector2(0, -1),  // Down
            new Vector2(-1, 0)   // Left
        };

        // For each direction, check as far as possible until hitting a piece or the edge of the board
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
                    // Empty square - can move here
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    // Hit a piece
                    if (IsWhitePiece(pieceAtTarget) != isWhiteRook)
                    {
                        // Enemy piece - can capture
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

        // All possible knight move offsets (L-shape: 2 in one direction, 1 in perpendicular direction)
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
                    // Empty square - can move here
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else if (IsWhitePiece(pieceAtTarget) != isWhiteKnight)
                {
                    // Enemy piece - can capture
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

        // The 4 diagonal directions for bishop movement
        Vector2[] directions = new Vector2[]
        {
            new Vector2(1, 1),   // Up-right
            new Vector2(1, -1),  // Down-right
            new Vector2(-1, 1),  // Up-left
            new Vector2(-1, -1)  // Down-left
        };

        // For each direction, check as far as possible until hitting a piece or the edge of the board
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
                    // Empty square - can move here
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    // Hit a piece
                    if (IsWhitePiece(pieceAtTarget) != isWhiteBishop)
                    {
                        // Enemy piece - can capture
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

        // The queen combines rook and bishop movements
        // All 8 directions (orthogonal and diagonal)
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

        // For each direction, check as far as possible until hitting a piece or the edge of the board
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
                    // Empty square - can move here
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else
                {
                    // Hit a piece
                    if (IsWhitePiece(pieceAtTarget) != isWhiteQueen)
                    {
                        // Enemy piece - can capture
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

        // All 8 directions around the king (one square in each direction)
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

        // Check each adjacent square
        foreach (Vector2 direction in directions)
        {
            Vector2 targetPos = new Vector2(currentX + direction.x, currentY + direction.y);

            if (IsValidBoardPosition(targetPos))
            {
                GameObject pieceAtTarget = GetPieceAtPosition(targetPos);

                if (pieceAtTarget == null)
                {
                    // Empty square - can move here
                    CreateHighlight(targetPos, normalMoveColor);
                }
                else if (IsWhitePiece(pieceAtTarget) != isWhiteKing)
                {
                    // Enemy piece - can capture
                    CreateHighlight(targetPos, attackMoveColor);
                    capturablePieces[pieceAtTarget] = targetPos;
                }
            }
        }

        // Check for castling
        if (!hasMoved[selectedPiece])
        {
            CheckCastling(currentX, currentY, isWhiteKing);
        }
    }

    void CheckCastling(int kingX, int kingY, bool isWhiteKing)
    {
        // Kingside castling
        CheckCastlingSide(kingX, kingY, 1, isWhiteKing);

        // Queenside castling
        CheckCastlingSide(kingX, kingY, -1, isWhiteKing);
    }

    void CheckCastlingSide(int kingX, int kingY, int direction, bool isWhiteKing)
    {
        // Kingside (direction = 1) or Queenside (direction = -1)
        int rookX = direction == 1 ? 7 : 0;
        int castleDistance = direction == 1 ? 2 : 3; // King moves 2 squares for kingside, 3 for queenside

        // Find the rook
        Vector2 rookPos = new Vector2(rookX, kingY);
        GameObject rook = GetPieceAtPosition(rookPos);

        // Check if there's a rook of the same color that hasn't moved
        if (rook != null && IsRook(rook) && IsWhitePiece(rook) == isWhiteKing &&
            hasMoved.ContainsKey(rook) && !hasMoved[rook])
        {
            // Check that all squares between king and rook are empty
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
                // Highlight the castling move
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

        // Forward move (1 square)
        Vector2 forwardPos = new Vector2(currentX, currentY + direction);
        if (IsValidBoardPosition(forwardPos) && !IsOccupied(forwardPos))
        {
            CreateHighlight(forwardPos, normalMoveColor);

            // Check for 2 square move from starting position
            if (!hasMoved[selectedPiece])
            {
                Vector2 doubleForwardPos = new Vector2(currentX, currentY + 2 * direction);
                if (IsValidBoardPosition(doubleForwardPos) && !IsOccupied(forwardPos) && !IsOccupied(doubleForwardPos))
                {
                    CreateHighlight(doubleForwardPos, normalMoveColor);
                }
            }
        }

        CheckAndHighlightCapture(new Vector2(currentX - 1, currentY + direction), isWhitePawn);
        CheckAndHighlightCapture(new Vector2(currentX + 1, currentY + direction), isWhitePawn);
    }

    void CheckAndHighlightCapture(Vector2 capturePos, bool isWhitePiece)
    {
        if (IsValidBoardPosition(capturePos))
        {
            GameObject enemyPiece = GetPieceAtPosition(capturePos);
            if (enemyPiece != null && IsWhitePiece(enemyPiece) != isWhitePiece)
            {
                // Highlight the piece itself with a red outline
                CreateHighlight(capturePos, attackMoveColor);

                // Add to capturable pieces
                capturablePieces[enemyPiece] = capturePos;

                // Optionally, add an effect to the enemy piece to indicate it can be captured
                SpriteRenderer enemyRenderer = enemyPiece.GetComponent<SpriteRenderer>();
                if (enemyRenderer != null)
                {
                    // You could add a temporary outline effect here if desired
                }
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

    bool IsOccupiedByOpponent(Vector2 boardPos, bool isWhitePiece)
    {
        GameObject piece = GetPieceAtPosition(boardPos);
        if (piece != null)
        {
            bool pieceIsWhite = IsWhitePiece(piece);
            return pieceIsWhite != isWhitePiece; // Return true if colors don't match (opponent)
        }
        return false;
    }

    void CreateHighlight(Vector2 boardPos, Color color)
    {
        Vector3 worldPos = new Vector3(boardPos.x * cellSize, boardPos.y * cellSize, -0.05f) + boardOffset;

        GameObject highlight = new GameObject("MoveHighlight");
        highlight.transform.position = worldPos;

        SpriteRenderer renderer = highlight.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateHighlightSprite();
        renderer.color = color;

        // Add a BoxCollider2D for click detection
        BoxCollider2D collider = highlight.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.9f, 0.9f); // Slightly smaller than the cell
        collider.isTrigger = true;

        highlightMarkers.Add(highlight);
    }

    Sprite CreateHighlightSprite()
    {
        // Create a highlight sprite with a border to improve visibility on all cells
        Texture2D texture = new Texture2D(64, 64);
        Color fillColor = Color.white;
        Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark border

        // Fill texture with transparent pixels first
        for (int x = 0; x < texture.width; x++)
        {
            for (int y = 0; y < texture.height; y++)
            {
                // Create a 3-pixel border
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

        ClearHighlights();
        Vector2 targetPos = capturablePieces[enemyPiece];
        Vector3 targetWorldPos = new Vector3(targetPos.x * cellSize, targetPos.y * cellSize, -0.1f) + boardOffset;

        selectedPiece.transform.position = targetWorldPos;
        enemyPiece.SetActive(false);

        hasMoved[selectedPiece] = true;
        isWhiteTurn = !isWhiteTurn;
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
                GameObject pieceAtTarget = GetPieceAtPosition(targetBoardPos);
                if (pieceAtTarget != null && pieceAtTarget != selectedPiece)
                {
                    pieceAtTarget.SetActive(false);
                }

                // Check if this is a castling move for king
                if (IsKing(selectedPiece) && !hasMoved[selectedPiece])
                {
                    int currentX = Mathf.RoundToInt((selectedPiece.transform.position.x - boardOffset.x) / cellSize);
                    if (Mathf.Abs(targetX - currentX) == 2)
                    {
                        // This is a castling move, move the rook too
                        HandleCastling(currentX, targetX, targetY);
                    }
                }

                break;
            }
        }

        if (validMoveFound)
        {
            Vector3 targetWorldPos = new Vector3(targetX * cellSize, targetY * cellSize, -0.1f) + boardOffset;
            selectedPiece.transform.position = targetWorldPos;
            hasMoved[selectedPiece] = true;

            // Pawn promotion check
            if (IsPawn(selectedPiece) && (targetY == 7 || targetY == 0))
            {
                PromotePawn(selectedPiece);
            }

            isWhiteTurn = !isWhiteTurn;
        }

        DeselectPiece();
    }

    void HandleCastling(int fromX, int toX, int y)
    {
        // Determine if kingside or queenside castling
        bool isKingside = toX > fromX;

        // Find the rook
        int rookX = isKingside ? 7 : 0;
        Vector2 rookPos = new Vector2(rookX, y);
        GameObject rook = GetPieceAtPosition(rookPos);

        if (rook != null && IsRook(rook))
        {
            // Calculate new position for rook
            int newRookX = isKingside ? toX - 1 : toX + 1;
            Vector3 newRookWorldPos = new Vector3(newRookX * cellSize, y * cellSize, -0.1f) + boardOffset;

            // Move the rook
            rook.transform.position = newRookWorldPos;
            hasMoved[rook] = true;
        }
    }

    void PromotePawn(GameObject pawn)
    {
        Debug.Log("Pawn promotion! Replace with queen.");
    }
}