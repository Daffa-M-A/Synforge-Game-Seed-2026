using UnityEngine;
using System.Collections.Generic;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(1, 1)));   // Top Right
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(1, -1)));  // Bottom Right
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(-1, 1)));  // Top Left
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(-1, -1))); // Bottom Left

        return r;
    }
}