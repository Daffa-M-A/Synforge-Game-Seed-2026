using UnityEngine;
using System.Collections.Generic;

public class Knight : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = base.GetAvailableMoves(ref board, tileCountX, tileCountY);

        if (isJailed || isGhost) return r;

        // List of possible knight moves
        Vector2Int[] jumpOffsets = {
            new Vector2Int(1, 2), new Vector2Int(2, 1),
            new Vector2Int(-1, 2), new Vector2Int(-2, 1),
            new Vector2Int(1, -2), new Vector2Int(2, -1),
            new Vector2Int(-1, -2), new Vector2Int(-2, -1)
        };

        // Check for each possible move
        foreach (Vector2Int offset in jumpOffsets)
        {
            int x = currentX + offset.x;
            int y = currentY + offset.y;

            if (x >= 0 && x < tileCountX && y >= 0 && y < tileCountY)
            {
                if (board[x, y] == null)
                {
                    r.Add(new Vector2Int(x, y));
                }
                else if (board[x, y].team != team)
                {
                    if (CanCapture(board[x, y]))
                    {
                        r.Add(new Vector2Int(x, y));
                    }
                }
            }
        }


        return r;
    }
}