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

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        bool isRaycastHit = Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Enemy Highlight"));

        HandleHover(isRaycastHit, info);
        HandleClick(isRaycastHit, info);
        HandleDrag(ray);
    }

    private void HandleHover(bool isRaycastHit, RaycastHit info)
    {
        if (generateBoard == null || boardManager == null) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            if (currentHover != -Vector2Int.one)
            {
                generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove, boardManager.chessPieces, -1);
                currentHover = -Vector2Int.one;
            }
            return; 
        }

        int currentTeam = currentlyDragging != null ? currentlyDragging.team : -1;

        if (isRaycastHit)
        {
            if (info.collider == null || info.transform == null) return;

            Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);

            if (currentHover != hitPosition)
            {
                if (AreHighlightsEnabled())
                {
                    generateBoard.UpdateHoverState(currentHover, hitPosition, availableMove, boardManager.chessPieces, currentTeam);
                }
                currentHover = hitPosition;
            }
        }
        else if (currentHover != -Vector2Int.one)
        {
            generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove, boardManager.chessPieces, currentTeam);
            currentHover = -Vector2Int.one;
        }
    }

    private void HandleClick(bool isRaycastHit, RaycastHit info)
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            if (isRaycastHit)
            {
                if (info.transform == null) return;
                Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);

                if (hitPosition == -Vector2Int.one) return;

                ChessPiece hitPiece = boardManager.chessPieces[hitPosition.x, hitPosition.y];
                if (hitPiece == null || hitPiece.isGhost || hitPiece.isJailed) return;

                bool isPieceWhite = hitPiece.team == 0;
                if (isPieceWhite != turnManager.isWhiteTurn) return;

                currentlyDragging = hitPiece;
                availableMove = currentlyDragging.GetAvailableMoves(ref boardManager.chessPieces, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
                if (AreHighlightsEnabled())
                {
                    generateBoard.HighlightTiles(availableMove, boardManager.chessPieces, currentlyDragging.team);
                }

                if (UIManager.Instance != null) UIManager.Instance.ShowPieceInfo(currentlyDragging);
            }
        }

        if (Input.GetMouseButtonUp(0) && currentlyDragging != null)
        {
            if (isRaycastHit && info.transform != null)
            {
                Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
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
            else currentlyDragging.SetPosition(boardManager.GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));

            currentlyDragging = null;
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