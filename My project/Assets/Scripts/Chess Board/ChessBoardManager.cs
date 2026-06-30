using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Xml.Serialization;

public class ChessBoardManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GenerateBoard generateBoard;
    public TurnManager turnManager;
    public MatchManager matchManager;

    [Header("Board Settings")]
    public float tileSize = 1.0f;
    public float yOffSet = 0.2f;
    public Vector3 boardCenter = Vector3.zero;
    public float deathSize = 0.5f;
    public float deathSpacing = 0.3f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // Constants & Logic Data
    public const int TILE_WIDTH = 8;
    public const int TILE_HEIGHT = 16;
    public int globalChargeCounter = 0;
    public bool isGameOver = false;
    public Vector3 bounds { get; private set; }

    // Public variable that will be used for ref in GetAvailableMoves() method of each chess piece
    public ChessPiece[,] chessPieces;

    public int whiteJailCount = 0;
    public int blackJailCount = 0;

    private void Start()
    {
        if (turnManager != null) turnManager.OnTurnChanged += ProcessTurnLogic;
    }

    private void Awake()
    {
        yOffSet += transform.position.y;
        bounds = new Vector3((TILE_WIDTH / 2f) * tileSize, 0, (TILE_HEIGHT / 2f) * tileSize) + boardCenter;

        if (generateBoard != null)
            generateBoard.GenerateAllTiles(tileSize, TILE_WIDTH, TILE_HEIGHT, yOffSet, bounds);

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_WIDTH, TILE_HEIGHT];
        int whiteTeam = 0;
        int blackTeam = 1;

        //White Team
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);

        //Black Team
        chessPieces[0, 15] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 15] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 15] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 15] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[4, 15] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[5, 15] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 15] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 15] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                    chessPieces[x, y].SaveOriginalPosition(x, y);
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    public Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    public bool ContainsValidMove(List<Vector2Int> moves, Vector2Int pos)
    {
        if (moves == null) return false;
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y) return true;
        }
        return false;
    }

    // Update order charge heirarki
    private void UpdateOrderCharge(ChessPiece resettingPiece)
    {
        if (resettingPiece.orderCharge == 0)
        {
            globalChargeCounter++;
            resettingPiece.orderCharge = globalChargeCounter;
        }
        else
        {
            // When chess piece once before get out of the first tile and reset
            int oldCharge = resettingPiece.orderCharge;

            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    ChessPiece piece = chessPieces[x, y];

                    if (piece != null && piece.orderCharge > oldCharge)
                    {
                        piece.orderCharge--;
                    }
                }
            }

            resettingPiece.orderCharge = globalChargeCounter;
        }
    }

    public bool MoveTo(ChessPiece cp, int x, int y, List<Vector2Int> availableMove)
    {
        if (!ContainsValidMove(availableMove, new Vector2Int(x, y))) return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        int baseRow = (cp.team == 0) ? 0 : 15;
        bool wasInBase = (previousPosition.y == baseRow);
        bool isMovingOut = (y != baseRow);

        if (wasInBase && isMovingOut)
        {
            UpdateOrderCharge(cp);
        }

        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            // Release team chess piece
            if (ocp.team == cp.team)
            {
                if (ocp.isJailed)
                {
                    ReleaseChain(ocp.team, ocp.jailSequenceNumber);
                }
                else
                {
                    return false;
                }
            }

            else
            {
                // Jail system
                ocp.isJailed = true;
                int jailX = 3;
                int jailY;

                // White team jail
                if (ocp.team == 0)
                {
                    whiteJailCount++;
                    ocp.jailSequenceNumber = whiteJailCount;
                    jailY = 15 - whiteJailCount;
                }
                else // Black team jail
                {
                    blackJailCount++;
                    ocp.jailSequenceNumber = blackJailCount;
                    jailY = blackJailCount;
                }

                ocp.currentX = jailX;
                ocp.currentY = jailY;
                chessPieces[jailX, jailY] = ocp;
                ocp.SetPosition(GetTileCenter(jailX, jailY));
            }
            
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        Vector3 startPos = GetTileCenter(previousPosition.x, previousPosition.y);
        Vector3 endPos = GetTileCenter(x, y);

        cp.currentX = x;
        cp.currentY = y;

        cp.MoveWithAnimation(startPos, endPos, 0.15f, 10f);

        // Check win condition
        if (matchManager != null)
        {
            matchManager.CheckWinConditions(cp, x, y);
        }

        return true;
    }

    // Choose random move
    public void ExecuteRandomMove(int team)
    {
        List<ChessPiece> movablePieces = new List<ChessPiece>();
        Dictionary<ChessPiece, List<Vector2Int>> pieceMoves = new Dictionary<ChessPiece, List<Vector2Int>>();

        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                ChessPiece cp = chessPieces[x, y];

                if (cp != null && cp.team == team && !cp.isJailed && !cp.isGhost)
                {
                    List<Vector2Int> moves = cp.GetAvailableMoves(ref chessPieces, TILE_WIDTH, TILE_HEIGHT);
                    if (moves.Count > 0)
                    {
                        movablePieces.Add(cp);
                        pieceMoves.Add(cp, moves);
                    }
                }
            }
        }

        // Logic to choose random
        if (movablePieces.Count > 0)
        {
            // Choose piece
            ChessPiece randomPiece = movablePieces[Random.Range(0, movablePieces.Count)];
            List<Vector2Int> availableMoves = pieceMoves[randomPiece];

            // Choose tile
            Vector2Int randomDest = availableMoves[Random.Range(0, availableMoves.Count)];

            Debug.Log($"[Auto-Move] Memindahkan {randomPiece.type} dari ({randomPiece.currentX}, {randomPiece.currentY}) ke ({randomDest.x}, {randomDest.y})");

            MoveTo(randomPiece, randomDest.x, randomDest.y, availableMoves);

            // Change turn after random move executed
            if (turnManager != null)
            {
                turnManager.EndTurn();
            }
        }
        else
        {
            // If there is no available move, skip the turn completely
            Debug.Log("Skip turn");
            if (turnManager != null)
            {
                turnManager.EndTurn();
            }
        }
    }

    // Release any piece who have bigger number that the one released
    private void ReleaseChain(int teamToFree, int triggerSequence)
    {
        List<ChessPiece> freedPieces = new List<ChessPiece>();

        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null && piece.team == teamToFree && piece.isJailed)
                {
                    if (piece.jailSequenceNumber >= triggerSequence)
                    {
                        freedPieces.Add(piece);
                        chessPieces[x, y] = null; 
                    }
                }
            }
        }

        foreach (ChessPiece piece in freedPieces)
        {
            piece.isJailed = false;
            piece.jailSequenceNumber = 0;
            piece.isGhost = true;
            piece.rechargeCountdown = 4;
        }

        // Place chess piece in original place
        List<ChessPiece> unplacedPieces = new List<ChessPiece>();

        foreach (ChessPiece piece in freedPieces)
        {
            if (chessPieces[piece.originalX, piece.originalY] == null)
            {
                piece.currentX = piece.originalX;
                piece.currentY = piece.originalY;
                chessPieces[piece.currentX, piece.currentY] = piece;
                piece.SetPosition(GetTileCenter(piece.currentX, piece.currentY));
            }
            else
            {
                unplacedPieces.Add(piece);
            }
        }

        // Search for other place if the original place is ocuppied and to avoid piled up in a same tile
        foreach (ChessPiece piece in unplacedPieces)
        {
            int baseRow = (piece.team == 0) ? 0 : 15;
            bool isPlaced = false;

            for (int x = 0; x < TILE_WIDTH; x++)
            {
                if (chessPieces[x, baseRow] == null)
                {
                    piece.currentX = x;
                    piece.currentY = baseRow;
                    chessPieces[x, baseRow] = piece;
                    piece.SetPosition(GetTileCenter(x, baseRow));
                    isPlaced = true;
                    break; 
                }
            }

            // Add one (or -1 for black team) if there is no empty tiles in the first tiles
            if (!isPlaced)
            {
                int targetX = piece.originalX;
                int targetY = piece.originalY;
                int dirY = (piece.team == 0) ? 1 : -1;

                while (chessPieces[targetX, targetY] != null)
                {
                    targetY += dirY;
                    if (targetY < 0 || targetY >= TILE_HEIGHT)
                    {
                        targetY -= dirY;
                        break;
                    }
                }

                piece.currentX = targetX;
                piece.currentY = targetY;
                chessPieces[targetX, targetY] = piece;
                piece.SetPosition(GetTileCenter(targetX, targetY));
            }
        }

        if (teamToFree == 0) whiteJailCount = triggerSequence - 1;
        else blackJailCount = triggerSequence - 1;
    }

    private void ProcessTurnLogic(bool isWhiteTurn)
    {
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    chessPieces[x, y].TickRechargeCountdown();
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnChanged -= ProcessTurnLogic;
    }
}