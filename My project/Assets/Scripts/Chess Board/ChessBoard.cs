using System;
using System.Collections.Generic;
using UnityEngine;

public class ChessBoard : MonoBehaviour
{
    [Header("Art & Visual")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffSet = 0.2f;
    [SerializeField] private Vector3 boardCenter = Vector3.zero;
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.3f;
    [SerializeField] private float dragOffSet = 1.25f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    //Logic
    private ChessPiece[,] chessPieces;
    private ChessPiece curentlyDragging;
    private List<ChessPiece> deathWhite = new List<ChessPiece>();
    private List<ChessPiece> deathBlack = new List<ChessPiece>();
    private const int TILE_WIDTH = 8;
    private const int TILE_HEIGHT = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover;
    private Vector3 bounds;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_WIDTH, TILE_HEIGHT);
        SpawnAllPieces();
        PositionAllPieces();
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

            //If we press down on the mouse 1 button
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    curentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                }
            }

            //If we are releasing the mouse 1 button
            if (curentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosisition = new Vector2Int(curentlyDragging.currentX, curentlyDragging.currentY);

                bool validMove = MoveTo(curentlyDragging, hitPosition.x, hitPosition.y);
                if (!validMove)
                {
                    curentlyDragging.SetPosition(GeTileCenter(previousPosisition.x, previousPosisition.y));
                    curentlyDragging = null;
                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }

            if (curentlyDragging && Input.GetMouseButtonUp(0))
            {
                curentlyDragging.SetPosition(GeTileCenter(curentlyDragging.currentX, curentlyDragging.currentY));
                curentlyDragging = null;
            }
        }

        //If we are dragging a piece
        if(curentlyDragging)
        {
            Plane horizonPlane = new Plane(Vector3.up, Vector3.up * yOffSet);
            float distance = 0;
            if(horizonPlane.Raycast(ray, out distance))
            {
                curentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffSet);
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

    //Spawning chess pieces on the board
    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_WIDTH, TILE_HEIGHT];

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
        /*
        for (int i = 0; i < TILE_WIDTH; i++)
        {
            chessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }
        */

        //Black Team
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        /*
        for (int i = 0; i < TILE_WIDTH; i++)
        {
            chessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
        */
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();

        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }

    //Posistioning pieces on the board
    private void PositionAllPieces()
    { 
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x,y].SetPosition(GeTileCenter(x, y), force);
    }

    private Vector3 GeTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffSet, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    //Operations
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosistion = new Vector2Int(cp.currentX, cp.currentY);

        //Is there another piece on the target position?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];

            if(ocp.team == cp.team)
            {
                return false;
            }

            //If its the enemy team
            if(ocp.team == 0)
            {
                deathWhite.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffSet, -1 * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing) * deathWhite.Count);
            }
            else
            {
                deathBlack.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(-1 * tileSize, yOffSet, 8 * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing) * deathBlack.Count);
            }
        }

        chessPieces[x, y] = cp;
        chessPieces[previousPosistion.x, previousPosistion.y] = null;

        PositionSinglePiece(x, y);

        return true;
    }

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