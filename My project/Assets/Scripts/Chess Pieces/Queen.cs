using UnityEngine;
using System.Collections.Generic;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = base.GetAvailableMoves(ref board, tileCountX, tileCountY);

        if (isJailed || isGhost) return r;

        // Rook movements
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.up));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.down));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.left));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, Vector2Int.right));

        // Bishop movements
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(1, 1)));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(1, -1)));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(-1, 1)));
        r.AddRange(GetMovesInDirection(ref board, tileCountX, tileCountY, new Vector2Int(-1, -1)));

        return r;
    }
}