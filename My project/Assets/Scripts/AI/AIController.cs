using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    [Header("Dependencies")]
    public ChessBoardManager boardManager;
    public TurnManager turnManager;

    [Header("AI Settings")]
    public int aiTeam = 1; // 1 = Tim Hitam (Tim B)
    public float thinkingDelay = 0.8f; // Jeda agar terasa natural
    public float randomnessNoise = 15f; // Sedikit variasi agar tidak monoton

    [Header("Heuristic Weights (Bobot Skor)")]
    public float winFortressScore = 10000f;
    public float freePrisonerBaseScore = 500f;
    public float capturePieceBaseScore = 300f;
    public float dangerPenaltyScore = -250f;
    public float advanceForwardMultiplier = 12f;
    public float getChargeBonus = 25f;

    private struct EvaluatedMove
    {
        public ChessPiece piece;
        public Vector2Int targetPos;
        public List<Vector2Int> allValidMovesForPiece;
        public float score;
    }

    private void Start()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged += HandleTurnChanged;
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= HandleTurnChanged;
        }
    }

    private void HandleTurnChanged(bool isWhiteTurn)
    {
        if (boardManager == null || boardManager.isGameOver) return;
        if (!boardManager.isPvEMode) return;

        // Jika bukan giliran Putih (artinya giliran Hitam / AI)
        if (!isWhiteTurn && aiTeam == 1)
        {
            StartCoroutine(ExecuteAITurnRoutine());
        }
    }

    private IEnumerator ExecuteAITurnRoutine()
    {
        // Tunggu sebentar agar animasi giliran sebelumnya selesai & AI terlihat berpikir
        yield return new WaitForSeconds(thinkingDelay);

        if (boardManager.isGameOver) yield break;

        EvaluatedMove bestMove = FindBestMove();

        if (bestMove.piece != null)
        {
            bool moved = boardManager.MoveTo(
                bestMove.piece,
                bestMove.targetPos.x,
                bestMove.targetPos.y,
                bestMove.allValidMovesForPiece
            );

            if (moved && turnManager != null && !boardManager.isGameOver)
            {
                turnManager.EndTurn();
            }
        }
        else
        {
            // Jika AI benar-benar tidak punya langkah legal, lewati giliran
            if (turnManager != null && !boardManager.isGameOver)
            {
                turnManager.EndTurn();
            }
        }
    }

    private EvaluatedMove FindBestMove()
    {
        List<EvaluatedMove> candidateMoves = new List<EvaluatedMove>();
        ChessPiece[,] board = boardManager.chessPieces;

        // 1. Kumpulkan semua peta bahaya (petak yang bisa diserang pemain Putih)
        HashSet<Vector2Int> playerAttackMap = GetAllEnemyAttackTiles(board, 0);

        // 2. Evaluasi setiap bidak AI dan setiap langkah legalnya
        for (int x = 0; x < ChessBoardManager.TILE_WIDTH; x++)
        {
            for (int y = 0; y < ChessBoardManager.TILE_HEIGHT; y++)
            {
                ChessPiece cp = board[x, y];
                if (cp != null && cp.team == aiTeam && !cp.isJailed && !cp.isGhost)
                {
                    List<Vector2Int> validMoves = cp.GetAvailableMoves(
                        ref board,
                        ChessBoardManager.TILE_WIDTH,
                        ChessBoardManager.TILE_HEIGHT
                    );

                    foreach (Vector2Int target in validMoves)
                    {
                        float moveScore = EvaluateSingleMove(cp, target, playerAttackMap);
                        candidateMoves.Add(new EvaluatedMove
                        {
                            piece = cp,
                            targetPos = target,
                            allValidMovesForPiece = validMoves,
                            score = moveScore
                        });
                    }
                }
            }
        }

        // 3. Pilih langkah dengan skor tertinggi
        EvaluatedMove best = new EvaluatedMove { score = float.MinValue };
        foreach (EvaluatedMove move in candidateMoves)
        {
            if (move.score > best.score)
            {
                best = move;
            }
        }

        return best;
    }

    private float EvaluateSingleMove(ChessPiece piece, Vector2Int target, HashSet<Vector2Int> enemyDangerTiles)
    {
        float score = 0f;
        ChessPiece[,] board = boardManager.chessPieces;
        ChessPiece[,] prisoners = boardManager.prisonerPieces;

        // A. CEK MENANG DI BENTENG LAWAN (Tim Putih punya benteng di X:3, Y:0)
        if (target.x == 3 && target.y == 0)
        {
            // Pastikan bidak memenuhi syarat menang (misal punya orderCharge > 0)
            if (piece.orderCharge > 0)
            {
                score += winFortressScore;
            }
        }

        // B. CEK MEMBEBASKAN TEMAN DI PENJARA
        if (prisoners[target.x, target.y] != null && prisoners[target.x, target.y].team == aiTeam)
        {
            score += freePrisonerBaseScore;
        }

        // C. CEK MENANGKAP BIDAK LAWAN
        ChessPiece targetOccupant = board[target.x, target.y];
        if (targetOccupant != null && targetOccupant.team != aiTeam)
        {
            score += capturePieceBaseScore + GetPieceValue(targetOccupant.type);
        }

        // D. KEMAJUAN KE ARAH MARKAS LAWAN (AI bergerak dari Y=15 menuju Y=0)
        int distanceMovedForward = piece.currentY - target.y;
        score += distanceMovedForward * advanceForwardMultiplier;

        // E. BONUS MENDAPATKAN ORDER CHARGE (Keluar dari baris awal Y=15)
        if (piece.currentY == 15 && target.y != 15 && piece.orderCharge == 0)
        {
            score += getChargeBonus;
        }

        // F. PENALTI BAHAYA (1-Ply Lookahead: Apakah petak tujuan diincar lawan?)
        if (enemyDangerTiles.Contains(target))
        {
            score += dangerPenaltyScore - GetPieceValue(piece.type);
        }

        // G. SEDIKIT VARIASI ACAK
        score += Random.Range(-randomnessNoise, randomnessNoise);

        return score;
    }

    private HashSet<Vector2Int> GetAllEnemyAttackTiles(ChessPiece[,] board, int enemyTeam)
    {
        HashSet<Vector2Int> dangerTiles = new HashSet<Vector2Int>();

        for (int x = 0; x < ChessBoardManager.TILE_WIDTH; x++)
        {
            for (int y = 0; y < ChessBoardManager.TILE_HEIGHT; y++)
            {
                ChessPiece enemy = board[x, y];
                if (enemy != null && enemy.team == enemyTeam && !enemy.isJailed && !enemy.isGhost)
                {
                    List<Vector2Int> enemyMoves = enemy.GetAvailableMoves(
                        ref board,
                        ChessBoardManager.TILE_WIDTH,
                        ChessBoardManager.TILE_HEIGHT
                    );

                    foreach (Vector2Int move in enemyMoves)
                    {
                        dangerTiles.Add(move);
                    }
                }
            }
        }

        return dangerTiles;
    }

    private float GetPieceValue(ChessPieceType type)
    {
        switch (type)
        {
            case ChessPieceType.Queen: return 90f; // Captain
            case ChessPieceType.Rook: return 50f; // Sprinter
            case ChessPieceType.Bishop: return 35f; // Flanker
            case ChessPieceType.Knight: return 30f; // Scout
            case ChessPieceType.King: return 40f; // Guardian
            default: return 10f; // Runner / Pawn
        }
    }
}