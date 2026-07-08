using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    [Header("Art & Visual")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private Material fortressMaterial;

    [Header("Move Destination VFX")]
    [SerializeField] private float destinationFlashInterval = 0.12f;

    private GameObject[,] tiles;
    private Coroutine destinationFlashRoutine;
    private Vector2Int flashingTile = -Vector2Int.one;
    private int tileLayer = -1;
    private int hoverLayer = -1;
    private bool isDestinationFlashActive;

    // Create the tiles for board
    public void GenerateAllTiles(float tileSize, int tileWidth, int tileHeight, float yOffSet, Vector3 bounds)
    {
        tiles = new GameObject[tileWidth, tileHeight];
        for (int x = 0; x < tileWidth; x++)
        {
            for (int y = 0; y < tileHeight; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y, yOffSet, bounds);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y, float yOffSet, Vector3 bounds)
    {
        GameObject tileObject = new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer meshRenderer = tileObject.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffSet, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffSet, (y + 1) * tileSize) - bounds;

        if ((x == 3 && y == 0) || (x == 3 && y == 15))
        {
            meshRenderer.material = fortressMaterial;
        }
        else
        {
            meshRenderer.material = tileMaterial;
        }

        int[] tris = new int[6] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    // changing layer of tiles depending on hover state and available moves
    public void UpdateHoverState(Vector2Int previousHover, Vector2Int currentHover, List<Vector2Int> availableMoves, ChessPiece[,] board = null, int currentTeam = -1)
    {
        if (previousHover != -Vector2Int.one && IsInsideBoard(previousHover) && !IsFlashingTile(previousHover))
        {
            if (tiles[previousHover.x, previousHover.y] != null)
            {
                RestoreTileLayer(previousHover, availableMoves, board, currentTeam);
            }
        }

        if (currentHover != -Vector2Int.one && IsInsideBoard(currentHover) && !IsFlashingTile(currentHover))
        {
            if (tiles[currentHover.x, currentHover.y] != null)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
    }

    private void RestoreTileLayer(Vector2Int tilePos, List<Vector2Int> availableMoves, ChessPiece[,] board, int currentTeam)
    {
        if (IsFlashingTile(tilePos)) return;

        if (ContainsValidMove(availableMoves, tilePos))
        {
            if (board != null && board[tilePos.x, tilePos.y] != null && board[tilePos.x, tilePos.y].team != currentTeam)
            {
                tiles[tilePos.x, tilePos.y].layer = LayerMask.NameToLayer("Enemy Highlight");
            }
            else
            {
                tiles[tilePos.x, tilePos.y].layer = LayerMask.NameToLayer("Highlight");
            }
        }
        else
        {
            tiles[tilePos.x, tilePos.y].layer = LayerMask.NameToLayer("Tile");
        }
    }

    private bool IsInsideBoard(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < tiles.GetLength(0) && pos.y >= 0 && pos.y < tiles.GetLength(1);
    }

	public void HighlightTiles(List<Vector2Int> availableMoves, ChessPiece[,] board, int currentTeam)
		{
			for (int i = 0; i < availableMoves.Count; i++)
			{
				int x = availableMoves[i].x;
				int y = availableMoves[i].y;

                if (IsFlashingTile(new Vector2Int(x, y))) continue;

				// Logika Deteksi Musuh: Jika ada bidak di petak tujuan dan timnya berbeda
				if (board[x, y] != null && board[x, y].team != currentTeam)
				{
					// Pindahkan ke layer Enemy Highlight agar Renderer Feature menimpanya dengan warna MERAH
					tiles[x, y].layer = LayerMask.NameToLayer("Enemy Highlight");
				}
				else
				{
					// Pindahkan ke layer Highlight agar Renderer Feature menimpanya dengan warna HIJAU
					tiles[x, y].layer = LayerMask.NameToLayer("Highlight");
				}
			}
		}

    public void RemoveHighlightTiles(List<Vector2Int> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            Vector2Int pos = availableMoves[i];
            if (IsFlashingTile(pos)) continue;
            tiles[pos.x, pos.y].layer = LayerMask.NameToLayer("Tile");
        }
    }

    public Vector2Int LookUpTileIndex(GameObject hitInfo, int tileWidth, int tileHeight)
    {
        for (int x = 0; x < tileWidth; x++)
        {
            for (int y = 0; y < tileHeight; y++)
            {
                if (tiles[x, y] == hitInfo) return new Vector2Int(x, y);
            }
        }
        return -Vector2Int.one;
    }

    private bool ContainsValidMove(List<Vector2Int> moves, Vector2Int pos)
    {
        if (moves == null) return false;
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y) return true;
        }
        return false;
    }

    public Material GetTileMaterial(int x, int y)
    {
        if (x >= 0 && x < tiles.GetLength(0) && y >= 0 && y < tiles.GetLength(1))
        {
            if (tiles[x, y] != null)
            {
                return tiles[x, y].GetComponent<MeshRenderer>().material;
            }
        }
        return null;
    }

    
    public void SetTileMaterial(int x, int y, Material newMaterial)
    {
        if (x >= 0 && x < tiles.GetLength(0) && y >= 0 && y < tiles.GetLength(1))
        {
            if (tiles[x, y] != null && newMaterial != null)
            {
                tiles[x, y].GetComponent<MeshRenderer>().material = newMaterial;
            }
        }
    }

    public void PlayDestinationTileFlashWhileMoving(Vector2Int tilePos, ChessPiece piece)
    {
        EnsureLayerIds();
        StopDestinationTileFlash();

        if (!IsInsideBoard(tilePos) || piece == null) return;

        destinationFlashRoutine = StartCoroutine(DestinationFlashCoroutine(tilePos, piece));
    }

    public void StopDestinationTileFlash()
    {
        if (destinationFlashRoutine != null)
        {
            StopCoroutine(destinationFlashRoutine);
            destinationFlashRoutine = null;
        }

        RestoreFlashingTileLayer();
    }

    private IEnumerator DestinationFlashCoroutine(Vector2Int tilePos, ChessPiece piece)
    {
        flashingTile = tilePos;
        isDestinationFlashActive = true;
        bool showHighlight = true;

        SetFlashingTileLayer(showHighlight);

        const float animationStartTimeout = 0.5f;
        float waitedForAnimation = 0f;
        while (piece != null && !piece.IsAnimating && waitedForAnimation < animationStartTimeout)
        {
            waitedForAnimation += Time.deltaTime;
            yield return null;
        }

        while (piece != null && (piece.IsAnimating || !piece.IsInIdleAnimation()))
        {
            showHighlight = !showHighlight;
            SetFlashingTileLayer(showHighlight);
            yield return new WaitForSeconds(destinationFlashInterval);
        }

        StopDestinationTileFlash();
    }

    private void SetFlashingTileLayer(bool showHighlight)
    {
        if (!IsInsideBoard(flashingTile) || tiles[flashingTile.x, flashingTile.y] == null) return;

        EnsureLayerIds();
        tiles[flashingTile.x, flashingTile.y].layer = showHighlight ? hoverLayer : tileLayer;
    }

    private bool IsFlashingTile(Vector2Int tilePos)
    {
        return isDestinationFlashActive && tilePos == flashingTile;
    }

    private void RestoreFlashingTileLayer()
    {
        if (flashingTile != -Vector2Int.one && IsInsideBoard(flashingTile) && tiles[flashingTile.x, flashingTile.y] != null)
        {
            EnsureLayerIds();
            tiles[flashingTile.x, flashingTile.y].layer = tileLayer;
        }

        flashingTile = -Vector2Int.one;
        isDestinationFlashActive = false;
    }

    private void EnsureLayerIds()
    {
        if (tileLayer < 0) tileLayer = LayerMask.NameToLayer("Tile");
        if (hoverLayer < 0) hoverLayer = LayerMask.NameToLayer("Hover");
    }
}