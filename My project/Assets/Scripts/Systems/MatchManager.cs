using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [Header("Dependencies")]
    public ChessBoardManager boardManager;
    public TimerManager timerManager;

    [Header("Match Settings")]
    public int maxPiecesPerTeam = 8;

    public void CheckWinConditions(ChessPiece lastMovedPiece, int x, int y)
    {
        if (boardManager.isGameOver) return;

        // Win from catching enemy fortress
        if (lastMovedPiece.team == 0 && x == 3 && y == 15)
        {
            DeclareWinner(0, "Tim Putih berhasil menduduki Fortress Hitam!");
            return;
        }
        else if (lastMovedPiece.team == 1 && x == 3 && y == 0)
        {
            DeclareWinner(1, "Tim Hitam berhasil menduduki Fortress Putih!");
            return;
        }

        // Win from catching every enemy piece
        if (boardManager.whiteJailCount >= maxPiecesPerTeam)
        {
            DeclareWinner(1, "Tim Hitam Menang! Seluruh pasukan Putih telah dipenjara.");
            return;
        }
        else if (boardManager.blackJailCount >= maxPiecesPerTeam)
        {
            DeclareWinner(0, "Tim Putih Menang! Seluruh pasukan Hitam telah dipenjara.");
            return;
        }
    }

    public void ExecuteTiebreaker()
    {
        if (boardManager.isGameOver) return;

        Debug.Log("Tidebreaker");

        int poinPutih = boardManager.blackJailCount;
        int poinHitam = boardManager.whiteJailCount;

        if (poinPutih > poinHitam)
        {
            DeclareWinner(0, $"White win (Tangkapan: {poinPutih} vs {poinHitam})");
        }
        else if (poinHitam > poinPutih)
        {
            DeclareWinner(1, $"Black win (Tangkapan: {poinHitam} vs {poinPutih})");
        }
        else
        {
            DeclareWinner(-1, $"Draw (Tangkapan Imbang: {poinPutih} - {poinHitam})");
        }
    }

    private void DeclareWinner(int winningTeam, string reason)
    {
        boardManager.isGameOver = true;

        // Add panel here
    }
}