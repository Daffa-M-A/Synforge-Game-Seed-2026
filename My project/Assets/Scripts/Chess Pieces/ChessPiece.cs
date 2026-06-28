using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public enum ChessPieceType
{
    None = 0, Pawn = 1, Rook = 2, Knight = 3, Bishop = 4, Queen = 5, King = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    public int orderCharge = 0;

    protected Vector3 desiredPosition;
    protected Vector3 desiredScale = Vector3.one;

    [Header("Jail & Ghost System Data")]
    public int originalX;
    public int originalY;
    public int jailSequenceNumber = 0;
    public bool isJailed = false;
    public int rechargeCountdown = 0;
    public bool isGhost = false;

    private void Awake()
    {
        desiredScale = transform.localScale;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    // Used for returning the piece after released from jail
    public void SaveOriginalPosition(int x, int y)
    {
        originalX = x;
        originalY = y;
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        if (isJailed || isGhost) return r;

        return r;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force) transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force) transform.localScale = desiredScale;
    }

    // Check if the order chagre allow chess piece to kill
    public virtual bool CanCapture(ChessPiece enemy)
    {
        if (this.isGhost || this.isJailed) return false;

        if (enemy.isGhost || enemy.isJailed) return false;

        int myBaseRow = (team == 0) ? 0 : 15;
        int enemyBaseRow = (team == 0) ? 15 : 0;

        // Will always kill inside the area
        if (this.currentY == myBaseRow) return true;

        // Define area
        bool amIInMyTerritory = (team == 0 && this.currentY <= 7) || (team == 1 && this.currentY >= 8);
        bool isEnemyInMyTerritory = (team == 0 && enemy.currentY <= 7) || (team == 1 && enemy.currentY >= 8);
        bool isEnemyInTheirTerritory = (team == 0 && enemy.currentY >= 8) || (team == 1 && enemy.currentY <= 7);

        // Check if enemy piece is inside his base
        bool isEnemyInTheirBase = (enemy.currentY == enemyBaseRow);

        // Block if enemy piece is inside the base row
        if (amIInMyTerritory && isEnemyInTheirBase) return false;

        // Enemy in our terriroty
        if (isEnemyInMyTerritory) return true;

        // Rule 2: Can use sacrifice to kill the enemy piece
        if (amIInMyTerritory && isEnemyInTheirTerritory) return true;

        // Default (check if the order charge allow to kill)
        return this.orderCharge > enemy.orderCharge;
    }

    // Rules 4: Chess piece can eat if there is an enemy piece adjacent to the piece
    protected List<Vector2Int> GetAdjacentCaptures(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> captures = new List<Vector2Int>();
        Vector2Int[] offsets = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int offset in offsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                ChessPiece target = board[x, y];
                // Only allow the move if there is an enemy piece
                if (target != null && target.team != team)
                {
                    if (CanCapture(target))
                    {
                        captures.Add(new Vector2Int(x, y));
                    }
                }
            }
        }
        return captures;
    }

    // Release if team chess piece is adjacent to the player chess piece
    protected List<Vector2Int> GetAdjacentRelease(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> libs = new List<Vector2Int>();
        Vector2Int[] offsets = {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (Vector2Int offset in offsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                ChessPiece target = board[x, y];
                if (target != null && target.team == team && target.isJailed)
                {
                    libs.Add(new Vector2Int(x, y));
                }
            }
        }
        return libs;
    }

    // Check for available tiles in specific direction
    protected List<Vector2Int> GetMovesInDirection(ref ChessPiece[,] board, int tileCountX, int tileCountY, Vector2Int direction)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int x = currentX + direction.x;
        int y = currentY + direction.y;
        int currentDistance = 1; // Penghitung jarak

        // Check if chess piece is still inside the first tile on their own area
        int myBaseRow = (team == 0) ? 0 : 15;
        int maxDistance = (this.currentY == myBaseRow) ? 7 : 16;

        // Rule 1: Stop when player move from the first tile to only 7 tiles
        while (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY && currentDistance <= maxDistance)
        {
            ChessPiece target = board[x, y];

            if (target == null || target.isGhost || target.isJailed)
            {
                moves.Add(new Vector2Int(x, y));
            }
            else
            {
                if (target.team != team)
                {
                    if (CanCapture(target))
                    {
                        moves.Add(new Vector2Int(x, y));
                    }
                }
                break; 
            }
            x += direction.x;
            y += direction.y;
            currentDistance++;
        }
        return moves;
    }

    // Countdown for ghost
    public void TickRechargeCountdown()
    {
        if (!isGhost) return;

        rechargeCountdown--;

        if (rechargeCountdown <= 0)
        {
            isGhost = false;
            rechargeCountdown = 0;
        }
    }
}