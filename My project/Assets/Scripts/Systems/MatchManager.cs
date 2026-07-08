using UnityEngine;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance;

    [Header("Dependencies")]
    public ChessBoardManager boardManager;
    public TimerManager timerManager;

    [Header("Match Settings")]
    public int maxPiecesPerTeam = 8;

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void CheckWinConditions(ChessPiece lastMovedPiece, int x, int y)
    {
        if (boardManager.isGameOver) return;

        // Win from catching enemy fortress
        if (lastMovedPiece.team == 0 && x == 3 && y == 15)
        {
            DeclareWinner(0, fromGlobalTimer: false); // White win
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.isTutorialActive = false;
            }
            return;
        }
        else if (lastMovedPiece.team == 1 && x == 3 && y == 0)
        {
            DeclareWinner(1, fromGlobalTimer: false); // Black win
            return;
        }

        // Win from catching every enemy piece
        if (boardManager.whiteJailCount >= maxPiecesPerTeam)
        {
            DeclareWinner(1, fromGlobalTimer: false); // Black win
            return;
        }
        else if (boardManager.blackJailCount >= maxPiecesPerTeam)
        {
            DeclareWinner(0, fromGlobalTimer: false); // White win
            return;
        }
    }

    public void ExecuteTiebreaker()
    {
        if (boardManager.isGameOver) return;

        int poinPutih = boardManager.blackJailCount;
        int poinHitam = boardManager.whiteJailCount;

        if (poinPutih > poinHitam)
        {
            DeclareWinner(0, fromGlobalTimer: true); // White win
        }
        else if (poinHitam > poinPutih)
        {
            DeclareWinner(1, fromGlobalTimer: true); // Black win
        }
        else
        {
            DeclareWinner(-1, fromGlobalTimer: true); // Draw
        }
    }

    private void DeclareWinner(int winningTeam, bool fromGlobalTimer)
    {
        boardManager.isGameOver = true;

        if (AudioManager.Instance != null)
            AudioManager.Instance.NotifyGameOver(winningTeam, fromGlobalTimer);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverUI(winningTeam);
        }
    }
}