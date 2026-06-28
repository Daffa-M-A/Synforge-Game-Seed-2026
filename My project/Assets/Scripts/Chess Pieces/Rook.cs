using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        if (isJailed || isGhost) return r;

        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.up)); // Up
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.down)); // Down
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.left)); // Left
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.right)); // Right

        r.AddRange(GetAdjacentCaptures(ref board, tileCountX, tileCountY)); // Adjacent kill
        return r;
    }
}