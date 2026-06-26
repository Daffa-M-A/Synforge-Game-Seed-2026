using System.Collections.Generic;
using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    [Header("Art & Visual")]
    [SerializeField] private Material tileMaterial;

    private GameObject[,] tiles;

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
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[1] = new Vector3(x * tileSize, yOffSet, (y + 1) * tileSize) - bounds;
        vertices[2] = new Vector3((x + 1) * tileSize, yOffSet, y * tileSize) - bounds;
        vertices[3] = new Vector3((x + 1) * tileSize, yOffSet, (y + 1) * tileSize) - bounds;

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
    public void UpdateHoverState(Vector2Int previousHover, Vector2Int currentHover, List<Vector2Int> availableMoves)
    {
        if (previousHover != -Vector2Int.one)
        {
            bool isValid = ContainsValidMove(availableMoves, previousHover);
            tiles[previousHover.x, previousHover.y].layer = isValid ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
        }

        if (currentHover != -Vector2Int.one)
        {
            tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Hover");
        }
    }

    public void HighlightTiles(List<Vector2Int> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
    }

    public void RemoveHighlightTiles(List<Vector2Int> availableMoves)
    {
        for (int i = 0; i < availableMoves.Count; i++)
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
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
}