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
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        bool isRaycastHit = Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight"));

        HandleHover(isRaycastHit, info);
        HandleClick(isRaycastHit, info);
        HandleDrag(ray);
    }

    private void HandleHover(bool isRaycastHit, RaycastHit info)
    {
        if (isRaycastHit)
        {
            Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);

            if (currentHover != hitPosition)
            {
                generateBoard.UpdateHoverState(currentHover, hitPosition, availableMove);
                currentHover = hitPosition;
            }
        }
        else if (currentHover != -Vector2Int.one)
        {
            generateBoard.UpdateHoverState(currentHover, -Vector2Int.one, availableMove);
            currentHover = -Vector2Int.one;
        }
    }

    private void HandleClick(bool isRaycastHit, RaycastHit info)
    {
        if (Input.GetMouseButtonDown(0) && isRaycastHit)
        {
            Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
            ChessPiece hitPiece = boardManager.chessPieces[hitPosition.x, hitPosition.y];

            if (hitPiece == null) return;

            bool isPieceWhite = hitPiece.team == 0;
            if (isPieceWhite != turnManager.IsWhiteTurn) return;

            currentlyDragging = hitPiece;
            availableMove = currentlyDragging.GetAvailableMoves(ref boardManager.chessPieces, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
            generateBoard.HighlightTiles(availableMove);
        }

        if (Input.GetMouseButtonUp(0) && currentlyDragging != null)
        {
            if (isRaycastHit) 
            {
                Vector2Int hitPosition = generateBoard.LookUpTileIndex(info.transform.gameObject, ChessBoardManager.TILE_WIDTH, ChessBoardManager.TILE_HEIGHT);
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = boardManager.MoveTo(currentlyDragging, hitPosition.x, hitPosition.y, availableMove);

                if (validMove)
                {
                    turnManager.EndTurn();
                }
                else
                {
                    currentlyDragging.SetPosition(boardManager.GetTileCenter(previousPosition.x, previousPosition.y));
                }
            }
            else 
            {
                currentlyDragging.SetPosition(boardManager.GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
            }

            currentlyDragging = null;
            generateBoard.RemoveHighlightTiles(availableMove);
            availableMove.Clear();
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
}