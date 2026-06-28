using UnityEngine;
using System.Collections.Generic;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();
        int direction = (team == 0) ? 1 : -1;

        int nextY = currentY + direction;

        if (isJailed || isGhost) return r;

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
            ChessPiece targetRight = board[currentX + 1, nextY];
            if (targetRight != null && targetRight.team != team)
            {
                if (CanCapture(targetRight))
                {
                    r.Add(new Vector2Int(currentX + 1, nextY));
                }
            }
        }

        if (currentX - 1 >= 0 && nextY >= 0 && nextY < tileCountY)
        {
            ChessPiece targetLeft = board[currentX - 1, nextY];
            if (targetLeft != null && targetLeft.team != team)
            {
                if (CanCapture(targetLeft))
                {
                    r.Add(new Vector2Int(currentX - 1, nextY));
                }
            }
        }

        r.AddRange(GetAdjacentCaptures(ref board, tileCountX, tileCountY)); // Adjacent kill

        return r;
    }
}