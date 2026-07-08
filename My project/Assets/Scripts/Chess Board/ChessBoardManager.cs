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

            int maxPieces = (matchManager != null) ? matchManager.maxPiecesPerTeam : 8;
            UIManager.Instance.UpdateTeamStats(maxPieces, whiteJailCount, blackJailCount);   // <-- baru
        }
    }

    private void Awake()
    {
        Instance = this;
        GameVfxManager.EnsureInstance();
        
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

        void SpawnFromSetup(List<PieceSetup> setups)
        {
            foreach (var setup in setups)
            {
                ChessPiece cp = SpawnSinglePiece(setup.pieceType, setup.team);
                cp.orderCharge = setup.customChargeOrder;
                cp.SaveOriginalPosition(setup.startPos.x, setup.startPos.y);

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

        Vector3 startPos = GetTileCenter(previousPosition.x, previousPosition.y);
        Vector3 endPos = GetTileCenter(x, y);

        int baseRow = (cp.team == 0) ? 0 : 15;
        bool wasInBase = (previousPosition.y == baseRow);
        bool isMovingOut = (y != baseRow);

        if (wasInBase && isMovingOut)
        {
            UpdateOrderCharge(cp);
            cp.PlayRechargeRankVfx();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("ReChargeSFX");            
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
                CaptureConditionType captureType = CaptureClassifier.Classify(cp, ocp);
                bool isSacrificeCapture = captureType == CaptureConditionType.Sacrifice;

                ocp.isJailed = true;

                if (AudioManager.Instance != null)
                {
                    if (isSacrificeCapture)
                    {
                        AudioManager.Instance.PlaySFX("NangkapSacrificePionSFX");
                    }
                    else
                    {
                        AudioManager.Instance.PlaySFX("NangkapPionSFX");
                    }
                }

                int jailX = JAIL_COLUMN_X;
                int jailY;
                Vector3 capturePos = GetTileCenter(x, y);

                if (GameVfxManager.Instance != null && !isSacrificeCapture)
                {
                    GameVfxManager.Instance.PlayCaptureVfx(captureType, capturePos, cp.team);
                }

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

                ocp.currentX = jailX;
                ocp.currentY = jailY;
                prisonerPieces[jailX, jailY] = ocp;
                ocp.AnimateCaptureToPrison(capturePos, GetTileCenter(jailX, jailY), 8f, isSacrificeCapture);
                UpdateTileVisuals(jailX, jailY);
				
				// TRIGGER SHAKE: Hanya saat bidak benar-benar menangkap musuh
                CameraController cam = Camera.main.GetComponent<CameraController>();
                if (cam != null) cam.TriggerShake();

                if (UIManager.Instance != null)
                {
                    int maxPieces = (MatchManager.Instance != null) ? MatchManager.Instance.maxPiecesPerTeam : 8;
                    UIManager.Instance.UpdateTeamStats(maxPieces, whiteJailCount, blackJailCount);
                }

                chessPieces[x, y] = cp;
                chessPieces[previousPosition.x, previousPosition.y] = null;

                cp.currentX = x;
                cp.currentY = y;

                BeginMoveDestinationVfx(cp, x, y);
                cp.MoveWithAnimation(startPos, endPos, 0.15f, 6f, isSacrificeCapture);

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
        cp.MoveWithAnimation(startPos, endPos, 0.15f, 6f);

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
                turnManager.EndTurn();
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
                turnManager.EndTurn();
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