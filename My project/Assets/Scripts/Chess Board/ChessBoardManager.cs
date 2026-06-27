using System.Collections.Generic;
using UnityEngine;

public class ChessBoardManager : MonoBehaviour
{
    [Header("Dependencies")]
    public GenerateBoard generateBoard;

    [Header("Board Settings")]
    public float tileSize = 1.0f;
    public float yOffSet = 0.2f;
    public Vector3 boardCenter = Vector3.zero;
    public float deathSize = 0.5f;
    public float deathSpacing = 0.3f;

    [Header("Prefabs & Materials")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    // Constants & Logic Data
    public const int TILE_WIDTH = 8;
    public const int TILE_HEIGHT = 16;
    public int globalChargeCounter = 0;
    public bool isGameOver = false;
    public Vector3 bounds { get; private set; }

    // Public variable that will be used for ref in GetAvailableMoves() method of each chess piece
    public ChessPiece[,] chessPieces;

    private List<ChessPiece> deathWhite = new List<ChessPiece>();
    private List<ChessPiece> deathBlack = new List<ChessPiece>();

    private void Awake()
    {
        yOffSet += transform.position.y;
        bounds = new Vector3((TILE_WIDTH / 2f) * tileSize, 0, (TILE_HEIGHT / 2f) * tileSize) + boardCenter;

        if (generateBoard != null)
            generateBoard.GenerateAllTiles(tileSize, TILE_WIDTH, TILE_HEIGHT, yOffSet, bounds);

        SpawnAllPieces();
        PositionAllPieces();
    }

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
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_WIDTH; x++)
        {
            for (int y = 0; y < TILE_HEIGHT; y++)
            {
                if (chessPieces[x, y] != null) PositionSinglePiece(x, y, true);
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

        int baseRow = (cp.team == 0) ? 0 : 15;
        bool wasInBase = (previousPosition.y == baseRow);
        bool isMovingOut = (y != baseRow);

        if (wasInBase && isMovingOut)
        {
            UpdateOrderCharge(cp);
        }

        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (ocp.team == cp.team) return false;

            if (ocp.team == 0)
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
        chessPieces[previousPosition.x, previousPosition.y] = null;
        PositionSinglePiece(x, y);

        // Check win condition
        if (cp.team == 0 && x == 3 && y == 15)
        {
            isGameOver = true;
            Debug.Log("PUTIH MENANG! Fortress Hitam Berhasil Direbut!");
        }
        else if (cp.team == 1 && x == 3 && y == 0)
        {
            isGameOver = true;
            Debug.Log("HITAM MENANG! Fortress Putih Berhasil Direbut!");
        }

        return true;
    }
}