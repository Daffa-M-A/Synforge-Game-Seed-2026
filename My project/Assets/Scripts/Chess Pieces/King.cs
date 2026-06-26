using UnityEngine;
using System.Collections.Generic;

public class King : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        Vector2Int[] moveOffsets = {
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(1, 1),   // Top Right
            new Vector2Int(1, -1),  // Bottom Right
            new Vector2Int(-1, 1),  // Top Left
            new Vector2Int(-1, -1)  // Bottom Left
        };

        foreach (Vector2Int offset in moveOffsets)
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