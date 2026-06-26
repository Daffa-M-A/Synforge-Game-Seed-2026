using System.Xml.Serialization;
using UnityEngine;
using System.Collections.Generic;

public enum  ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;
    public int orderCharge = 0;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        return r;
    }

    // Check  for available moves in a specific direction
    protected List<Vector2Int> GetMovesInDirection(ref ChessPiece[,] board, int tileCountX, int tileCountY, Vector2Int direction)
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        int x = currentX + direction.x;
        int y = currentY + direction.y;

        // Loop through the board in the specified direction until we hit the edge of the board or another piece
        while (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
        {
            if (board[x, y] == null)
            {
                moves.Add(new Vector2Int(x, y));
            }
            else
            {
                if (board[x, y].team != team)
                {
                    if (CanCapture(board[x, y]))
                    {
                        moves.Add(new Vector2Int(x, y)); 
                    }
                }
                break; 
            }
            x += direction.x;
            y += direction.y;
        }
        return moves;
    }

    // Check if order charge allow chess piece to kill
    public virtual bool CanCapture(ChessPiece enemy)
    {
        int baseRow = (team == 0) ? 0 : 15;

        // If in base, when they come out give highest order charge
        if (this.currentY == baseRow)
        {
            return true;
        }

        // If in home territory, the one with the home can kill the opponent even if the opponent have higher order charge
        bool isEnemyInMyTerritory = false;

        if (team == 0 && enemy.currentY >= 1 && enemy.currentY <= 7)
        {
            isEnemyInMyTerritory = true;
        }
        else if (team == 1 && enemy.currentY >= 8 && enemy.currentY <= 14)
        {
            isEnemyInMyTerritory = true;
        }

        if (isEnemyInMyTerritory)
        {
            return true;
        }

        // Normal condition when you have higher order charge
        return this.orderCharge > enemy.orderCharge;
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if(force)
        {
            transform.position = desiredPosition;
        }
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
        {
            transform.localScale = desiredScale;
        }
    }
}
