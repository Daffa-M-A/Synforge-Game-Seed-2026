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

    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMove = new List<Vector2Int>();

    private void Update()
    {
        if (boardManager.isGameOver) return;

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
            }
        }
        else if (currentHover != -Vector2Int.one)
        {
            generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove, boardManager.chessPieces, team);
            currentHover = -Vector2Int.one;
        }
    }

    private void HandleClick(bool hasTileUnderMouse, Vector2Int hoveredTile)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

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
                            currentlyDragging.SetPosition(boardManager.GetTileCenter(previousPosition.x, previousPosition.y));
                        }
                    }
                }
                else 
                {
                    // Kembalikan bidak ke posisi asal jika dilepas di luar area valid
                    currentlyDragging.SetPosition(boardManager.GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
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
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffSet);
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