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

    [Header("Game Mode")]
    public bool isPvEMode = true;

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
    // Counter urutan charge seumur match untuk nomor di Charge Log (01, 02, ... 23, ...).
    public int chargeSequenceCounter = 0;
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
        if (turnManager != null)
        {
            turnManager.OnTurnChanged += ProcessTurnLogic;
            turnManager.OnOwnerTurnEnding += ProcessEndOfOwnerTurn;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);

            int maxPieces = (matchManager != null) ? matchManager.maxPiecesPerTeam : 8;
            UIManager.Instance.UpdateTeamStats(maxPieces, whiteJailCount, blackJailCount);   // <-- baru
        }
    }

    private void Awake()
    {
        Instance = this;
        GameVfxManager.EnsureInstance();

        isPvEMode = PlayerPrefs.GetInt("IsPvE", 0) == 1;

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
        cp.pieceName = randomName;

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

    // Special spawn function for tutorial
    public void SetupTutorialBoard(List<PieceSetup> playerPieces, List<PieceSetup> enemyPieces)
    {
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (chessPieces[x, y] != null) Destroy(chessPieces[x, y].gameObject);
                if (prisonerPieces[x, y] != null) Destroy(prisonerPieces[x, y].gameObject);

                chessPieces[x, y] = null;
                prisonerPieces[x, y] = null;
                UpdateTileVisuals(x, y);
            }
        }

        whiteJailCount = 0;
        blackJailCount = 0;
        globalChargeCounter = 0;
        chargeSequenceCounter = 0;

        void SpawnFromSetup(List<PieceSetup> setups)
        {
            foreach (var setup in setups)
            {
                ChessPiece cp = SpawnSinglePiece(setup.pieceType, setup.team);
                cp.orderCharge = setup.customChargeOrder;
                cp.chargeSequence = setup.customChargeOrder;
                if (setup.customChargeOrder > chargeSequenceCounter) chargeSequenceCounter = setup.customChargeOrder;
                cp.SaveOriginalPosition(setup.startPos.x, setup.startPos.y);
                int homeRow = ChessPiece.GetHomeRow(setup.team);
                if (setup.startPos.y != homeRow || setup.customChargeOrder > 0)
                {
                    cp.hasLeftHomeRow = setup.startPos.y != homeRow;
                }
                if (setup.customChargeOrder > globalChargeCounter)
                {
                    globalChargeCounter = setup.customChargeOrder;
                }

                if (setup.isPrisoner)
                {
                    cp.isJailed = true;
                    int jailX = 3;
                    int jailY;

                    if (cp.team == 0)
                    {
                        whiteJailCount++;
                        cp.jailSequenceNumber = whiteJailCount;
                        jailY = 15 - whiteJailCount;
                    }
                    else
                    {
                        blackJailCount++;
                        cp.jailSequenceNumber = blackJailCount;
                        jailY = blackJailCount;
                    }

                    cp.currentX = jailX;
                    cp.currentY = jailY;
                    prisonerPieces[jailX, jailY] = cp;
                    cp.ApplyPrisonerIdleState(GetTileCenter(jailX, jailY));
                    UpdateTileVisuals(jailX, jailY);
                }
                else
                {
                    cp.currentX = setup.startPos.x;
                    cp.currentY = setup.startPos.y;
                    chessPieces[setup.startPos.x, setup.startPos.y] = cp;
                    cp.SetPosition(GetTileCenter(setup.startPos.x, setup.startPos.y), true);
                }
            }
        }

        SpawnFromSetup(playerPieces);
        SpawnFromSetup(enemyPieces);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);
        }

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.maxPiecesPerTeam = Mathf.Max(playerPieces.Count, enemyPieces.Count);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTeamStats(MatchManager.Instance.maxPiecesPerTeam, whiteJailCount, blackJailCount);
        }
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

    public Vector2Int WorldPositionToTileIndex(Vector3 worldPosition)
    {
        Vector3 local = worldPosition + bounds;
        int x = Mathf.FloorToInt(local.x / tileSize);
        int y = Mathf.FloorToInt(local.z / tileSize);

        if (x < 0 || x >= TILE_WIDTH || y < 0 || y >= TILE_HEIGHT)
            return -Vector2Int.one;

        return new Vector2Int(x, y);
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

    public List<ChessPiece> CollectAllPieces()
    {
        List<ChessPiece> pieces = new List<ChessPiece>();
        HashSet<ChessPiece> seen = new HashSet<ChessPiece>();

        void AddFrom(ChessPiece[,] grid)
        {
            if (grid == null) return;
            for (int x = 0; x < TILE_WIDTH; x++)
            {
                for (int y = 0; y < TILE_HEIGHT; y++)
                {
                    ChessPiece piece = grid[x, y];
                    if (piece != null && seen.Add(piece))
                    {
                        pieces.Add(piece);
                    }
                }
            }
        }

        AddFrom(chessPieces);
        AddFrom(prisonerPieces);
        return pieces;
    }

    private void UpdateOrderCharge(ChessPiece resettingPiece)
    {
        if (resettingPiece == null) return;

        List<ChessPiece> others = new List<ChessPiece>();
        List<ChessPiece> allPieces = CollectAllPieces();
        for (int i = 0; i < allPieces.Count; i++)
        {
            ChessPiece piece = allPieces[i];
            if (piece != resettingPiece && piece.orderCharge > 0)
            {
                others.Add(piece);
            }
        }

        others.Sort((a, b) => a.orderCharge.CompareTo(b.orderCharge));
        for (int i = 0; i < others.Count; i++)
        {
            others[i].orderCharge = i + 1;
        }

        resettingPiece.orderCharge = others.Count + 1;
        globalChargeCounter = resettingPiece.orderCharge;
        resettingPiece.chargeSequence = ++chargeSequenceCounter;
    }

    private void OnPieceLandedOnHomeRow(ChessPiece cp)
    {
        if (cp == null) return;
        UpdateOrderCharge(cp);
        cp.PlayRechargeRankVfx();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ReChargeSFX");
    }

		public bool MoveTo(ChessPiece cp, int x, int y, List<Vector2Int> availableMove)
			{
				if (!ContainsValidMove(availableMove, new Vector2Int(x, y))) return false;

        ChessPiece occupant = chessPieces[x, y];
        if (occupant != null)
        {
            if (occupant.team == cp.team) return false;
            if (!cp.CanCapture(occupant)) return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        Vector3 startPos = GetTileCenter(previousPosition.x, previousPosition.y);
        Vector3 endPos = GetTileCenter(x, y);

        int baseRow = ChessPiece.GetHomeRow(cp.team);
        bool wasInBase = (previousPosition.y == baseRow);
        bool isMovingOut = (y != baseRow);

        bool isMovingIn = (!wasInBase && y == baseRow);

        if (wasInBase && isMovingOut)
        {
            cp.hasLeftHomeRow = true;
            // Bidak yang belum pernah charge (awal permainan) dapat urutan saat pertama keluar
            if (cp.orderCharge == 0) OnPieceLandedOnHomeRow(cp);
        }
        else if (isMovingIn)
        {
            // Urutan charge terupdate setiap hinggap di petak charge
            OnPieceLandedOnHomeRow(cp);
        }

        cp.MarkMovedThisTurn();

        // Shake kecil saat bidak hinggap menyentuh kawannya yang tawanan
        bool releasedPrisoner = false;
        if (prisonerPieces[x, y] != null)
        {
            ChessPiece prisoner = prisonerPieces[x, y];

            if (prisoner.team == cp.team)
            {
                ReleaseChain(prisoner.team, prisoner.jailSequenceNumber);
                releasedPrisoner = true;
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
                CaptureConditionType captureType = CaptureClassifier.Classify(cp, ocp);

                ocp.isJailed = true;
                ocp.idleOwnerTurns = 0;
                ocp.movedThisTurn = false;

                int jailX = JAIL_COLUMN_X;
                int jailY;
                Vector3 capturePos = GetTileCenter(x, y);

                int sequence = GetNextJailSequence(ocp.team);
                ocp.jailSequenceNumber = sequence;
                if (ocp.team == 0)
                {
                    whiteJailCount = sequence;
                    jailY = 15 - sequence;
                }
                else
                {
                    blackJailCount = sequence;
                    jailY = sequence;
                }

                // State logis penjara diset sekarang; visualnya (SFX, VFX, lari ke penjara, shake)
                // baru dimainkan setelah bidak penangkap hinggap di tile musuh.
                ocp.currentX = jailX;
                ocp.currentY = jailY;
                prisonerPieces[jailX, jailY] = ocp;
                Vector3 jailPos = GetTileCenter(jailX, jailY);

                if (UIManager.Instance != null)
                {
                    int maxPieces = (MatchManager.Instance != null) ? MatchManager.Instance.maxPiecesPerTeam : 8;
                    UIManager.Instance.UpdateTeamStats(maxPieces, whiteJailCount, blackJailCount);
                }

                chessPieces[x, y] = cp;
                chessPieces[previousPosition.x, previousPosition.y] = null;

                cp.currentX = x;
                cp.currentY = y;

                int captorTeam = cp.team;
                BeginMoveDestinationVfx(cp, x, y);
                cp.MoveWithAnimation(startPos, endPos, 0.15f, 6f, false, () =>
                {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("NangkapPionSFX");

                    if (GameVfxManager.Instance != null)
                        GameVfxManager.Instance.PlayCaptureVfx(captureType, capturePos, captorTeam);

                    // Kalau tawanan ini sudah keburu dibebaskan sebelum penangkap hinggap, jangan dilarikan ke penjara.
                    if (ocp != null && ocp.isJailed)
                    {
                        ocp.AnimateCaptureToPrison(capturePos, jailPos, 8f);
                        UpdateTileVisuals(jailX, jailY);
                    }

                    // TRIGGER SHAKE: tepat saat penangkap hinggap di tile musuh
                    TriggerCameraShake(1f);
                });

                UpdateTileVisuals(x, y);
                UpdateTileVisuals(previousPosition.x, previousPosition.y);

                if (matchManager != null)
                {
                    if (TutorialManager.Instance == null || !TutorialManager.Instance.isTutorialActive || TutorialManager.Instance.IsFreePlayStage())
                    {
                        matchManager.CheckWinConditions(cp, x, y);
                    }
                }

                if (UIManager.Instance != null)
                {
                    UIManager.Instance.UpdateChargeList(chessPieces);
                }

                return true;
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        cp.currentX = x;
        cp.currentY = y;

        BeginMoveDestinationVfx(cp, x, y);
        if (releasedPrisoner)
        {
            // Shake kecil saat hinggap menyentuh kawan yang tawanan
            cp.MoveWithAnimation(startPos, endPos, 0.15f, 6f, false, () => TriggerCameraShake(releaseShakeMultiplier));
        }
        else
        {
            cp.MoveWithAnimation(startPos, endPos, 0.15f, 6f);
        }

        UpdateTileVisuals(x, y);
        UpdateTileVisuals(previousPosition.x, previousPosition.y);

        // Check win condition
        if (matchManager != null)
        {
            if (TutorialManager.Instance == null || !TutorialManager.Instance.isTutorialActive || TutorialManager.Instance.IsFreePlayStage())
            {
                matchManager.CheckWinConditions(cp, x, y);
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);
        }

        return true;
    }

    // Choose random move
    public void ExecuteRandomMove(int team, bool isFromTimeout = false)
    {
        List<ChessPiece> movablePieces = new List<ChessPiece>();
        Dictionary<ChessPiece, List<Vector2Int>> pieceMoves = new Dictionary<ChessPiece, List<Vector2Int>>();

        if (isFromTimeout && TutorialManager.Instance != null && TutorialManager.Instance.isTutorialActive)
        {
            int currentIndex = TutorialManager.Instance.currentStageIndex;
            TutorialStepData currentStage = TutorialManager.Instance.tutorialStages[currentIndex];

            if (!currentStage.enableTimer)
            {
                return;
            }
        }

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

            MoveTo(randomPiece, randomDest.x, randomDest.y, availableMoves);

            // Change turn after random move executed
            if (turnManager != null)
            {
                turnManager.EndTurn(isFromTimeout);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyAutoMoveExecuted();
            }
        }
        else
        {
            // If there is no available move, skip the turn completely
            if (turnManager != null)
            {
                turnManager.EndTurn(isFromTimeout);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyAutoMoveExecuted();
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

        if (AudioManager.Instance != null)
        {
            // Suara rantai putus segera diputar
            AudioManager.Instance.PlaySFX("RescuePionSFX");
            
            // Suara sorak tawanan diberikan jeda sedikit agar terdengar lebih jelas
            StartCoroutine(PlayDelayedSFX("ChainBreak1SFX_Yay", 0.15f));
        }

        GameVfxManager vfxManager = GameVfxManager.Instance;
        if (vfxManager != null)
        {
            foreach (ChessPiece piece in freedPieces)
            {
                Vector2Int jailPos = jailPositions[piece];
                vfxManager.PlayPrisonerReleaseVfx(GetTileCenter(jailPos.x, jailPos.y), piece.team);
            }
        }

        foreach (ChessPiece piece in freedPieces)
        {
            piece.isJailed = false;
            piece.jailSequenceNumber = 0;
            piece.isGhost = true;
            piece.rechargeCountdown = 4;
            piece.idleOwnerTurns = 0;
            piece.movedThisTurn = false;
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

        RepackJailColumn(teamToFree);

        if (UIManager.Instance != null)
        {
            int maxPieces = (MatchManager.Instance != null) ? MatchManager.Instance.maxPiecesPerTeam : 8;
            UIManager.Instance.UpdateTeamStats(maxPieces, whiteJailCount, blackJailCount);
        }
    }

    // Helper untuk memberikan jeda antar suara agar tidak distorsi
    private IEnumerator PlayDelayedSFX(string sfxName, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sfxName);
    }

    private const int JAIL_COLUMN_X = 3;

    // Compact remaining prisoners so freed slots are reused (seq 1, 2, 3… with no gaps)
    private void RepackJailColumn(int team)
    {
        List<ChessPiece> remaining = new List<ChessPiece>();

        for (int y = 0; y < TILE_HEIGHT; y++)
        {
            ChessPiece piece = prisonerPieces[JAIL_COLUMN_X, y];
            if (piece != null && piece.team == team && piece.isJailed)
            {
                remaining.Add(piece);
            }
        }

        remaining.Sort((a, b) => a.jailSequenceNumber.CompareTo(b.jailSequenceNumber));

        for (int y = 0; y < TILE_HEIGHT; y++)
        {
            if (prisonerPieces[JAIL_COLUMN_X, y] != null && prisonerPieces[JAIL_COLUMN_X, y].team == team)
            {
                prisonerPieces[JAIL_COLUMN_X, y] = null;
                UpdateTileVisuals(JAIL_COLUMN_X, y);
            }
        }

        for (int i = 0; i < remaining.Count; i++)
        {
            int sequence = i + 1;
            int jailY = team == 0 ? 15 - sequence : sequence;
            ChessPiece piece = remaining[i];

            piece.jailSequenceNumber = sequence;
            piece.currentX = JAIL_COLUMN_X;
            piece.currentY = jailY;
            prisonerPieces[JAIL_COLUMN_X, jailY] = piece;
            piece.ApplyPrisonerIdleState(GetTileCenter(JAIL_COLUMN_X, jailY));
            UpdateTileVisuals(JAIL_COLUMN_X, jailY);
        }

        if (team == 0) whiteJailCount = remaining.Count;
        else blackJailCount = remaining.Count;
    }

    [Header("Camera Shake")]
    [Tooltip("Pengali kekuatan shake saat membebaskan tawanan (1 = sama dengan shake tangkap).")]
    public float releaseShakeMultiplier = 0.4f;

    private void TriggerCameraShake(float multiplier)
    {
        if (Camera.main == null) return;
        CameraController cam = Camera.main.GetComponent<CameraController>();
        if (cam != null) cam.TriggerShake(multiplier);
    }

    private int GetNextJailSequence(int team)
    {
        int count = 0;
        for (int y = 0; y < TILE_HEIGHT; y++)
        {
            ChessPiece piece = prisonerPieces[JAIL_COLUMN_X, y];
            if (piece != null && piece.team == team && piece.isJailed)
            {
                count++;
            }
        }
        return count + 1;
    }

    // Helper function to assign final position and animate the release of a piece from jail
    private void AssignFinalPosition(ChessPiece piece, int targetX, int targetY, Vector2Int jailPos)
    {
        piece.currentX = targetX;
        piece.currentY = targetY;
        chessPieces[targetX, targetY] = piece;

        // Tahanan yang dibebaskan hinggap di petak charge: urutan charge terupdate
        if (targetY == ChessPiece.GetHomeRow(piece.team)) UpdateOrderCharge(piece);

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

            if (!active.IsAnimating) active.SetPosition(activePos, true);
            if (!prisoner.IsAnimating)
            {
                prisoner.SetPosition(prisonerPos, true);
                prisoner.transform.rotation = Quaternion.LookRotation(ChessPiece.PrisonerFacing);
            }
        }
        else if (active != null)
        {
            active.SetScale(Vector3.one, false);
            if (!active.IsAnimating) active.SetPosition(center, true);
        }
        else if (prisoner != null)
        {
            prisoner.SetScale(Vector3.one, false);
            if (!prisoner.IsAnimating)
            {
                prisoner.SetPosition(center, true);
                prisoner.transform.rotation = Quaternion.LookRotation(ChessPiece.PrisonerFacing);
            }
        }
    }

    private void BeginMoveDestinationVfx(ChessPiece cp, int destX, int destY)
    {
        if (generateBoard == null || cp == null) return;
        generateBoard.PlayDestinationTileFlashWhileMoving(new Vector2Int(destX, destY), cp);
    }

    public void ProcessEndOfOwnerTurn(int team)
    {
        if (isGameOver) return;

        List<ChessPiece> teamPieces = new List<ChessPiece>();
        List<ChessPiece> allPieces = CollectAllPieces();
        for (int i = 0; i < allPieces.Count; i++)
        {
            if (allPieces[i].team == team) teamPieces.Add(allPieces[i]);
        }

        for (int i = 0; i < teamPieces.Count; i++)
        {
            ChessPiece piece = teamPieces[i];
            if (piece.IsExemptFromAutoReturn())
            {
                if (piece.IsOnHomeRow() || piece.isJailed || piece.isGhost)
                {
                    piece.idleOwnerTurns = 0;
                }
                piece.movedThisTurn = false;
                continue;
            }

            if (piece.movedThisTurn)
            {
                piece.idleOwnerTurns = 0;
            }
            else if (piece.idleOwnerTurns < ChessPiece.AutoReturnOwnerTurns)
            {
                piece.idleOwnerTurns++;
            }

            piece.movedThisTurn = false;
        }

        for (int i = 0; i < teamPieces.Count; i++)
        {
            ChessPiece piece = teamPieces[i];
            if (piece.IsExemptFromAutoReturn()) continue;
            if (piece.idleOwnerTurns >= ChessPiece.AutoReturnOwnerTurns)
            {
                TryAutoReturnHome(piece);
            }
        }
    }

    private bool BlocksHomeReturn(ChessPiece occupant, ChessPiece mover)
    {
        if (occupant == null || occupant == mover) return false;
        if (occupant.isGhost || occupant.isJailed) return false;
        return true;
    }

    private bool CanWalkHomeReturnTile(int x, int y, ChessPiece mover)
    {
        if (x < 0 || x >= TILE_WIDTH || y < 0 || y >= TILE_HEIGHT) return false;
        return !BlocksHomeReturn(chessPieces[x, y], mover);
    }

    private bool IsHomeReturnDestinationEmpty(int x, int y, ChessPiece mover)
    {
        if (x < 0 || x >= TILE_WIDTH || y < 0 || y >= TILE_HEIGHT) return false;
        ChessPiece occupant = chessPieces[x, y];
        return occupant == null || occupant == mover;
    }

    private Vector2Int FindHomeReturnDestination(ChessPiece piece)
    {
        int homeRow = ChessPiece.GetHomeRow(piece.team);
        int preferredX = piece.originalX;

        if (IsHomeReturnDestinationEmpty(preferredX, homeRow, piece))
        {
            return new Vector2Int(preferredX, homeRow);
        }

        int bestX = -1;
        int bestDist = int.MaxValue;
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            if (!IsHomeReturnDestinationEmpty(x, homeRow, piece)) continue;
            int dist = Mathf.Abs(x - preferredX);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestX = x;
            }
        }

        if (bestX < 0) return new Vector2Int(-1, -1);
        return new Vector2Int(bestX, homeRow);
    }

    private List<Vector2Int> FindHomeReturnPath(ChessPiece piece, Vector2Int dest)
    {
        Vector2Int start = new Vector2Int(piece.currentX, piece.currentY);
        if (start == dest) return new List<Vector2Int> { start };

        Vector2Int[] dirs = {
            new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 0), new Vector2Int(-1, 0)
        };

        bool[,] visited = new bool[TILE_WIDTH, TILE_HEIGHT];
        Vector2Int[,] parent = new Vector2Int[TILE_WIDTH, TILE_HEIGHT];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        visited[start.x, start.y] = true;
        parent[start.x, start.y] = new Vector2Int(-1, -1);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == dest) break;

            for (int i = 0; i < dirs.Length; i++)
            {
                int nx = current.x + dirs[i].x;
                int ny = current.y + dirs[i].y;
                if (nx < 0 || nx >= TILE_WIDTH || ny < 0 || ny >= TILE_HEIGHT) continue;
                if (visited[nx, ny]) continue;

                if (nx == dest.x && ny == dest.y)
                {
                    if (!IsHomeReturnDestinationEmpty(nx, ny, piece)) continue;
                }
                else if (!CanWalkHomeReturnTile(nx, ny, piece))
                {
                    continue;
                }

                visited[nx, ny] = true;
                parent[nx, ny] = current;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }

        if (!visited[dest.x, dest.y]) return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int walk = dest;
        while (walk.x >= 0)
        {
            path.Add(walk);
            walk = parent[walk.x, walk.y];
        }
        path.Reverse();
        return path;
    }

    private void TryAutoReturnHome(ChessPiece piece)
    {
        if (piece == null || piece.IsExemptFromAutoReturn()) return;

        Vector2Int dest = FindHomeReturnDestination(piece);
        if (dest.x < 0) return;

        List<Vector2Int> path = FindHomeReturnPath(piece, dest);
        if (path == null || path.Count == 0) return;

        Vector2Int previous = new Vector2Int(piece.currentX, piece.currentY);
        chessPieces[previous.x, previous.y] = null;
        chessPieces[dest.x, dest.y] = piece;
        piece.currentX = dest.x;
        piece.currentY = dest.y;
        piece.idleOwnerTurns = 0;
        if (dest.y == ChessPiece.GetHomeRow(piece.team)) OnPieceLandedOnHomeRow(piece);

        List<Vector3> worldPath = new List<Vector3>(path.Count);
        for (int i = 0; i < path.Count; i++)
        {
            worldPath.Add(GetTileCenter(path[i].x, path[i].y));
        }

        piece.AnimateHomeReturn(worldPath);
        UpdateTileVisuals(previous.x, previous.y);
        UpdateTileVisuals(dest.x, dest.y);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateChargeList(chessPieces);
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
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= ProcessTurnLogic;
            turnManager.OnOwnerTurnEnding -= ProcessEndOfOwnerTurn;
        }
    }
}