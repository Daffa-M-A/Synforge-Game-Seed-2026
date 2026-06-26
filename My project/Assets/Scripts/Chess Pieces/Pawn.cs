using UnityEngine;
using System.Collections.Generic;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        int direction = (team == 0) ? 1 : -1;

        int nextY = currentY + direction;

        // One tile move
        if (nextY >= 0 && nextY < tileCountY)
        {
            if (board[currentX, nextY] == null)
            {
                r.Add(new Vector2Int(currentX, nextY));

                // Two tile move
                if ((team == 0 && currentY == 1) || (team == 1 && currentY == 13))
                {
                    int doubleNextY = currentY + (direction * 2);
                    if (board[currentX, doubleNextY] == null)
                    {
                        r.Add(new Vector2Int(currentX, doubleNextY));
                    }
                }
            }
        }

        // Kill moves
        if (currentX + 1 < tileCountX && nextY >= 0 && nextY < tileCountY)
        {
            if (board[currentX + 1, nextY] != null && board[currentX + 1, nextY].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, nextY));
            }
        }

        if (currentX - 1 >= 0 && nextY >= 0 && nextY < tileCountY)
        {
            if (board[currentX - 1, nextY] != null && board[currentX - 1, nextY].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, nextY));
            }
        }

        return r;
    }
}