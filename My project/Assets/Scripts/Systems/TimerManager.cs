using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("Dependencies")]
    public ChessBoardManager boardManager;
    public TurnManager turnManager;
    public MatchManager matchManager;

    [Header("Timer Settings")]
    public float globalTimerDuration = 900f;
    public float playerTimerDuration = 30f;

    private float currentGlobalTimer;
    private float currentPlayerTimer;

    private float nextGlobalLogTime;
    private float nextPlayerLogTime;

    private bool isTimerActive = false;

    private void Start()
    {
        currentGlobalTimer = globalTimerDuration;
        currentPlayerTimer = playerTimerDuration;

        nextGlobalLogTime = currentGlobalTimer - 60f;
        nextPlayerLogTime = currentPlayerTimer - 10f;

        isTimerActive = true;

        if (turnManager != null) turnManager.OnTurnChanged += ResetPlayerTimer;
    }

    private void Update()
    {
        if (!isTimerActive || boardManager == null || boardManager.isGameOver) return;

        // Logic for global timer
        currentGlobalTimer -= Time.deltaTime;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnTimerDisplay(currentPlayerTimer);
            UIManager.Instance.UpdateGlobalTimerDisplay(currentGlobalTimer);
        }

        if (currentGlobalTimer <= nextGlobalLogTime && currentGlobalTimer > 0)
        {
            Debug.Log($"[Global Timer] Sisa waktu: {Mathf.CeilToInt(currentGlobalTimer / 60f)} Menit");
            nextGlobalLogTime -= 60f;
        }

        // Tidebreaker
        if (currentGlobalTimer <= 0)
        {
            currentGlobalTimer = 0;
            isTimerActive = false;

            if (matchManager != null)
                matchManager.ExecuteTiebreaker(); 

            return;
        }

        // Logic for player timer
        currentPlayerTimer -= Time.deltaTime;

        if (currentPlayerTimer <= nextPlayerLogTime && currentPlayerTimer > 0)
        {
            Debug.Log($"[Player Timer] Sisa giliran: {Mathf.CeilToInt(currentPlayerTimer)} Detik");
            nextPlayerLogTime -= 10f;
        }

        // Jika Player Timer Habis (AUTO-MOVE)
        if (currentPlayerTimer <= 0)
        {
            currentPlayerTimer = 0;
            Debug.Log("[Player Timer] Waktu habis! Sistem memilih pergerakan acak...");

            // Random move executed when player timer goes off
            int currentTeam = turnManager.isWhiteTurn ? 0 : 1;
            boardManager.ExecuteRandomMove(currentTeam);
        }
    }

    private void ResetPlayerTimer(bool isWhiteTurn)
    {
        currentPlayerTimer = playerTimerDuration;
        nextPlayerLogTime = currentPlayerTimer - 10f;
        Debug.Log($"[TimerManager] Giliran berganti! Player Timer direset ke {playerTimerDuration} detik.");
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnChanged -= ResetPlayerTimer;
    }
}