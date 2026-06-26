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
                    moves.Add(new Vector2Int(x, y)); 
                }
                break; 
            }

            x += direction.x;
            y += direction.y;
        }

        return moves;
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
