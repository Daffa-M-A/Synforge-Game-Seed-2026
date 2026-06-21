using System;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Art & Visual")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffSet = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;

    //Logic
    private const int TILE_WIDTH = 8;
    private const int TILE_HEIGHT = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_WIDTH, TILE_HEIGHT);
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            //Get the tile index that was hit
            Vector2Int hitPosition = LookUpTileIndex(info.transform.gameObject);

            //If we are hovering over a tile after not hovering over any tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }

            //if we already hovering over a tile, change the previous one
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if(currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
        }
    }

    //Generate all the tiles for the chessboard
    private void GenerateAllTiles(float tileSize, int tileWidth, int tileHeight)
    {
        yOffSet += transform.position.y;
        bounds = new Vector3((tileWidth / 2) * tileSize, 0, (tileWidth / 2) * tileSize) + boardCenter;

        tiles = new GameObject[tileWidth, tileHeight];
        for (int x = 0; x < tileHeight; x++)
        {
            for (int y = 0; y < tileWidth; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
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

        int[] tris = new int[6] {0, 1, 2, 1, 3, 2};

        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    //Operations
    private Vector2Int LookUpTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }

        return -Vector2Int.one; //Invalid tile index
    }
}