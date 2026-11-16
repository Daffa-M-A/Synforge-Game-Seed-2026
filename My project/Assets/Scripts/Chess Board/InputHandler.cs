using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public ChessBoardManager boardManager;
    public GenerateBoard generateBoard;
    public TurnManager turnManager;

    [Header("Settings")]
    public float dragOffSet = 1.25f;

    [Header("Threat Glow (hover saat drag)")]
    [Tooltip("Warna glow musuh yang bisa memakan bidak kita di tile yang sedang di-hover.")]
    public Color threatGlowColor = new Color(1f, 0.15f, 0.1f, 1f);
    [Tooltip("Kekuatan emission glow.")]
    public float threatGlowIntensity = 1.5f;

    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMove = new List<Vector2Int>();
    private readonly List<ChessPiece> glowingThreats = new List<ChessPiece>();

    private void Update()
    {
        if (boardManager.isGameOver) return;

        if (UIManager.Instance != null && UIManager.Instance.IsPaused) return;

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        bool hasTileUnderMouse = TryGetTileUnderMouse(ray, out Vector2Int hoveredTile);

        HandleHover(hasTileUnderMouse, hoveredTile);
        HandleClick(hasTileUnderMouse, hoveredTile);
        HandleDrag(ray);
    }

    private bool TryGetTileUnderMouse(Ray ray, out Vector2Int tileIndex)
    {
        tileIndex = -Vector2Int.one;
        if (boardManager == null) return false;

        Plane boardPlane = new Plane(Vector3.up, Vector3.up * boardManager.yOffSet);
        if (!boardPlane.Raycast(ray, out float distance)) return false;

        tileIndex = boardManager.WorldPositionToTileIndex(ray.GetPoint(distance));
        return tileIndex != -Vector2Int.one;
    }

    private void HandleHover(bool hasTileUnderMouse, Vector2Int hoveredTile)
    {
        if (generateBoard == null || boardManager == null) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            if (currentHover != -Vector2Int.one)
            {
                int currentTeam = currentlyDragging != null ? currentlyDragging.team : -1;
                generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove, boardManager.chessPieces, currentTeam);
                currentHover = -Vector2Int.one;
                ClearThreatGlow();
            }
            return;
        }

        int team = currentlyDragging != null ? currentlyDragging.team : -1;

        if (hasTileUnderMouse)
        {
            if (currentHover != hoveredTile)
            {
                generateBoard.UpdateHoverState(currentHover, hoveredTile, availableMove, boardManager.chessPieces, team);
                currentHover = hoveredTile;
                UpdateThreatGlow(hoveredTile);
            }
        }
        else if (currentHover != -Vector2Int.one)
        {
            generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove, boardManager.chessPieces, team);
            currentHover = -Vector2Int.one;
            ClearThreatGlow();
        }
    }

    // ---- Threat glow ----
    // Saat drag dan hover di tile yang legal, simulasikan bidak berada di tile itu,
    // lalu nyalakan glow merah pada semua musuh yang bisa memakannya di sana.
    private void UpdateThreatGlow(Vector2Int tile)
    {
        List<ChessPiece> threats = null;

        if (currentlyDragging != null && tile != -Vector2Int.one && boardManager.ContainsValidMove(availableMove, tile))
        {
            threats = FindThreatsAt(currentlyDragging, tile);
        }

        // Matikan yang tidak lagi mengancam
        for (int i = glowingThreats.Count - 1; i >= 0; i--)
        {
            ChessPiece p = glowingThreats[i];
            if (p == null || threats == null || !threats.Contains(p))
            {
                if (p != null) p.SetThreatGlow(false, threatGlowColor);
                glowingThreats.RemoveAt(i);
            }
        }

        if (threats == null) return;

        // Nyalakan yang baru
        for (int i = 0; i < threats.Count; i++)
        {
            ChessPiece p = threats[i];
            if (!glowingThreats.Contains(p))
            {
                p.SetThreatGlow(true, threatGlowColor, threatGlowIntensity);
                glowingThreats.Add(p);
            }
        }
    }

    private void ClearThreatGlow()
    {
        for (int i = 0; i < glowingThreats.Count; i++)
        {
            if (glowingThreats[i] != null) glowingThreats[i].SetThreatGlow(false, threatGlowColor);
        }
        glowingThreats.Clear();
    }

    private List<ChessPiece> FindThreatsAt(ChessPiece mover, Vector2Int dest)
    {
        List<ChessPiece> threats = new List<ChessPiece>();
        ChessPiece[,] board = boardManager.chessPieces;
        if (board == null) return threats;

        int w = ChessBoardManager.TILE_WIDTH;
        int h = ChessBoardManager.TILE_HEIGHT;

        // Simulasi: pindahkan bidak ke tile tujuan (bidak musuh di tile itu dianggap sudah dimakan).
        int origX = mover.currentX, origY = mover.currentY;
        ChessPiece displaced = board[dest.x, dest.y];

        board[origX, origY] = null;
        board[dest.x, dest.y] = mover;
        mover.currentX = dest.x;
        mover.currentY = dest.y;

        try
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    ChessPiece enemy = board[x, y];
                    if (enemy == null || enemy == mover || enemy.team == mover.team) continue;
                    if (enemy.isJailed || enemy.isGhost) continue;

                    List<Vector2Int> enemyMoves = enemy.GetAvailableMoves(ref board, w, h);
                    if (enemyMoves.Contains(dest)) threats.Add(enemy);
                }
            }
        }
        finally
        {
            // Kembalikan papan ke keadaan semula
            mover.currentX = origX;
            mover.currentY = origY;
            board[dest.x, dest.y] = displaced;
            board[origX, origY] = mover;
        }

        return threats;
    }

    private void HandleClick(bool hasTileUnderMouse, Vector2Int hoveredTile)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            if (boardManager.isPvEMode && !turnManager.isWhiteTurn) return;

            if (hasTileUnderMouse)
            {
                Vector2Int hitPosition = hoveredTile;

                if (hitPosition == -Vector2Int.one) return;

				ChessPiece hitPiece = boardManager.chessPieces[hitPosition.x, hitPosition.y];
                if (hitPiece == null || hitPiece.isGhost || hitPiece.isJailed) return;

                // Hitung gerakan yang tersedia untuk bidak yang diklik (kawan maupun lawan)
                availableMove = hitPiece.GetAvailableMoves(ref boardManager.chessPieces, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
                
                // Tampilkan highlight jika fitur highlight aktif
                if (AreHighlightsEnabled())
                {
                    generateBoard.HighlightTiles(availableMove, boardManager.chessPieces, hitPiece.team);
                }

                if (currentHover != -Vector2Int.one)
                {
                    generateBoard.UpdateHoverState(-Vector2Int.one, currentHover, availableMove, boardManager.chessPieces, hitPiece.team);
                }

                // Tampilkan info bidak di UI
                if (UIManager.Instance != null) UIManager.Instance.ShowPieceInfo(hitPiece);

                // LOGIKA INTERAKSI: Hanya izinkan Dragging jika itu bidak kita
                bool isPieceWhite = hitPiece.team == 0;
                if (isPieceWhite == turnManager.isWhiteTurn) 
                {
                    currentlyDragging = hitPiece;
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("PickUpPionSFX");
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            // Glow ancaman harus mati sebelum bidak bergerak/flash lain dimulai
            ClearThreatGlow();

            // LOGIKA PERGERAKAN: Hanya dijalankan jika kita sedang men-drag bidak (Bidak Kawan)
            if (currentlyDragging != null)
            {
                if (hasTileUnderMouse)
                {
                    Vector2Int hitPosition = hoveredTile;
                    
                    if (hitPosition != -Vector2Int.one)
                    {
                        Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                        bool isMoveAllowed = true;
                        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorialActive)
                        {
                            isMoveAllowed = TutorialManager.Instance.ValidateMove(currentlyDragging, hitPosition);
                        }

                        bool validMove = isMoveAllowed && boardManager.MoveTo(currentlyDragging, hitPosition.x, hitPosition.y, availableMove);

                        if (validMove)
                        {
                            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("DropPion");
                            turnManager.EndTurn();

                            if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorialActive)
                            {
                                if (TutorialManager.Instance.ShouldSkipEnemyTurn())
                                {
                                    turnManager.EndTurn();
                                }
                            }
                        }
                        else
                        {
                            currentlyDragging.SetPosition(boardManager.GetTileCenter(previousPosition.x, previousPosition.y), true);
                        }
                    }
                }
                else 
                {
                    // Kembalikan bidak ke posisi asal jika dilepas di luar area valid
                    currentlyDragging.SetPosition(boardManager.GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY), true);
                }

                currentlyDragging = null;
            }

            // LOGIKA PEMBERSIHAN VISUAL: Dijalankan untuk semua bidak (Kawan maupun Lawan)
            generateBoard.RemoveHighlightTiles(availableMove);
            availableMove.Clear();

            if (UIManager.Instance != null) UIManager.Instance.HidePieceInfo();
        }
    }

    private void HandleDrag(Ray ray)
    {
        if (currentlyDragging == null) return;

        Plane horizonPlane = new Plane(Vector3.up, Vector3.up * boardManager.yOffSet);
        if (horizonPlane.Raycast(ray, out float distance))
        {
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffSet, true);
        }
    }

    private bool AreHighlightsEnabled()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.isTutorialActive)
        {
            TutorialStepData stage = TutorialManager.Instance.tutorialStages[TutorialManager.Instance.currentStageIndex];
            return !stage.disableHighlights;
        }
        return true;
    }
}