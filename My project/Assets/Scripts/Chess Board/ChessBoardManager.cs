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
    [SerializeField] private GameObject[] whitePrefabs;
    [SerializeField] private GameObject[] blackPrefabs;

    [Header("Piece Naming & Roles")]
    public TextAsset namesTextFile;
    private string[] availableNames;

    // Constants & Logic Data
    public const int TILE_WIDTH = 8;
    public const int TILE_HEIGHT = 16;
    public int globalChargeCounter = 0;
    public bool isGameOver = false;
    public Vector3 bounds { get; private set; }
    public static ChessBoardManager Instance;

    // Public variable that will be used for ref in GetAvailableMoves() method of each chess piece
    public ChessPiece[,] chessPieces;
    public ChessPiece[,] prisonerPieces;

    public int whiteJailCount = 0;
    public int blackJailCount = 0;

    private void Start()
    {
        if (turnManager != null) turnManager.OnTurnChanged += ProcessTurnLogic;
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);
        }
    }

    private void Awake()
    {
        Instance = this;
        
        yOffSet += transform.position.y;
        bounds = new Vector3((TILE_WIDTH / 2f) * tileSize, 0, (TILE_HEIGHT / 2f) * tileSize) + boardCenter;

        if (namesTextFile != null)
        {
            availableNames = namesTextFile.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            availableNames = new string[] { "Player1", "Player2", "Player3", "Player4", "Player5" };
        }

        if (generateBoard != null)
            generateBoard.GenerateAllTiles(tileSize, TILE_WIDTH, TILE_HEIGHT, yOffSet, bounds);

        SpawnAllPieces();
        PositionAllPieces();
    }

    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_WIDTH, TILE_HEIGHT];
        prisonerPieces = new ChessPiece[TILE_WIDTH, TILE_HEIGHT];
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
        GameObject[] selectedPrefabs = (team == 0) ? whitePrefabs : blackPrefabs;

        Quaternion startRotation = (team == 0) ? Quaternion.identity : Quaternion.Euler(0, 180, 0);

        GameObject spawnedObj = Instantiate(selectedPrefabs[(int)type - 1], transform.position, startRotation, transform);
        ChessPiece cp = spawnedObj.GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;

        // Give name from list of names in the text file
        string randomName = availableNames[Random.Range(0, availableNames.Length)];
        string teamPrefix = (team == 0) ? "[A]" : "[B]";
        cp.pieceName = $"{teamPrefix} {randomName}";

        // Give role name based on the type of chess piece
        switch (type)
        {
            case ChessPieceType.Pawn:
                cp.roleName = "Runner";
                break;
            case ChessPieceType.Knight:
                cp.roleName = "Scout";
                break;
            case ChessPieceType.Bishop:
                cp.roleName = "Flanker";
                break;
            case ChessPieceType.Rook:
                cp.roleName = "Sprinter";
                break;
            case ChessPieceType.Queen:
                cp.roleName = "Captain";
                break;
            case ChessPieceType.King:
                cp.roleName = "Guardian";
                break;
            default:
                cp.roleName = "Unknown";
                break;
        }

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

            //vaughan goal 6
            Animator rechargeAnim = cp.GetComponentInChildren<Animator>();
            if (rechargeAnim != null) rechargeAnim.SetTrigger("doHappy");

        }

        if (prisonerPieces[x, y] != null)
        {
            ChessPiece prisoner = prisonerPieces[x, y];

            if (prisoner.team == cp.team)
            {
                ReleaseChain(prisoner.team, prisoner.jailSequenceNumber);
            }
        }

        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if (ocp.team == cp.team)
            {
                return false; 
            }
            else
            {
                ocp.isJailed = true;

                // Ambil animator dari kedua belah pihak - Vaughan Goal 6
                Animator attackerAnim = cp.GetComponentInChildren<Animator>();
                Animator victimAnim = ocp.GetComponentInChildren<Animator>();

                if (attackerAnim != null) attackerAnim.SetTrigger("doCapture");
                if (victimAnim != null) 
                {
                    victimAnim.SetTrigger("isCaptured");
                    victimAnim.SetBool("isPrisoner", true);
                }
                
                int jailX = 3;
                int jailY;

                if (ocp.team == 0)
                {
                    whiteJailCount++;
                    ocp.jailSequenceNumber = whiteJailCount;
                    jailY = 15 - whiteJailCount;
                }
                else
                {
                    blackJailCount++;
                    ocp.jailSequenceNumber = blackJailCount;
                    jailY = blackJailCount;
                }

                ocp.currentX = jailX;
                ocp.currentY = jailY;
                prisonerPieces[jailX, jailY] = ocp;
                UpdateTileVisuals(jailX, jailY);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        Vector3 startPos = GetTileCenter(previousPosition.x, previousPosition.y);
        Vector3 endPos = GetTileCenter(x, y);

        cp.currentX = x;
        cp.currentY = y;

        cp.MoveWithAnimation(startPos, endPos, 0.15f, 1f);

        UpdateTileVisuals(x, y);
        UpdateTileVisuals(previousPosition.x, previousPosition.y);

        // Check win condition
        if (matchManager != null)
        {
            matchManager.CheckWinConditions(cp, x, y);
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);
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
        Dictionary<ChessPiece, Vector2Int> jailPositions = new Dictionary<ChessPiece, Vector2Int>();

        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                ChessPiece piece = prisonerPieces[x, y];

                if (piece != null && piece.team == teamToFree && piece.isJailed)
                {
                    if (piece.jailSequenceNumber >= triggerSequence)
                    {
                        freedPieces.Add(piece);
                        jailPositions.Add(piece, new Vector2Int(x, y));

                        prisonerPieces[x, y] = null;
                        UpdateTileVisuals(x, y);
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

            //vaughan goal 6
            Animator pAnim = piece.GetComponentInChildren<Animator>();
            if (pAnim != null) 
            {
                pAnim.SetBool("isPrisoner", false);
                pAnim.SetTrigger("doHappy");
            }            

            piece.SetScale(Vector3.one, true);
        }

        // Place chess piece in original place
        List<ChessPiece> unplacedPieces = new List<ChessPiece>();

        foreach (ChessPiece piece in freedPieces)
        {
            if (chessPieces[piece.originalX, piece.originalY] == null)
            {
                AssignFinalPosition(piece, piece.originalX, piece.originalY, jailPositions[piece]);
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
                    AssignFinalPosition(piece, x, baseRow, jailPositions[piece]);
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

                AssignFinalPosition(piece, targetX, targetY, jailPositions[piece]);
            }
        }
    }

    // Helper function to assign final position and animate the release of a piece from jail
    private void AssignFinalPosition(ChessPiece piece, int targetX, int targetY, Vector2Int jailPos)
    {
        piece.currentX = targetX;
        piece.currentY = targetY;
        chessPieces[targetX, targetY] = piece;

        Vector3 startPos = GetTileCenter(jailPos.x, jailPos.y);
        Vector3 finalPos = GetTileCenter(targetX, targetY);

        int rallyY = (piece.team == 0) ? -1 : 16;
        Vector3 rallyPos = GetTileCenter(targetX, rallyY);

        piece.AnimateGhostRelease(startPos, rallyPos, finalPos, 10f);
    }

    // Visual for prisoner and normal piece in one tile
    public void UpdateTileVisuals(int x, int y)
    {
        if (x < 0 || x >= TILE_WIDTH || y < 0 || y >= TILE_HEIGHT) return;

        ChessPiece active = chessPieces[x, y];
        ChessPiece prisoner = prisonerPieces[x, y];
        Vector3 center = GetTileCenter(x, y);

        if (active != null && prisoner != null)
        {
            active.SetScale(Vector3.one * 0.5f, false);
            prisoner.SetScale(Vector3.one * 0.5f, false);

            Vector3 activePos = center; activePos.x -= 0.25f;
            Vector3 prisonerPos = center; prisonerPos.x += 0.25f;

            active.SetPosition(activePos, false);
            prisoner.SetPosition(prisonerPos, false);
        }
        else if (active != null)
        {
            active.SetScale(Vector3.one, false);
            active.SetPosition(center, false);
        }
        else if (prisoner != null)
        {
            prisoner.SetScale(Vector3.one, false);
            prisoner.SetPosition(center, false);
        }
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