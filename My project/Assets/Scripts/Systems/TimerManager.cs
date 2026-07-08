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
        if (boardManager == null) return;

        if (boardManager.isGameOver)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.UpdateTimerWarnings(0f, 0f, isGameOver: true);
            }

            return;
        }

        if (!isTimerActive) return;

        // Logic for global timer
        currentGlobalTimer -= Time.deltaTime;

        PushTimerDisplayToUI();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.UpdateTimerWarnings(
                currentGlobalTimer,
                currentPlayerTimer,
                boardManager.isGameOver);
        }

        if (currentGlobalTimer <= nextGlobalLogTime && currentGlobalTimer > 0)
        {
            nextGlobalLogTime -= 60f;
        }

        // Tidebreaker
        if (currentGlobalTimer <= 0)
        {
            currentGlobalTimer = 0;
            isTimerActive = false;

            if (AudioManager.Instance != null)
                AudioManager.Instance.NotifyGlobalTimerExpired();

            if (matchManager != null)
                matchManager.ExecuteTiebreaker();

            return;
        }

        // Logic for player timer
        currentPlayerTimer -= Time.deltaTime;

        if (currentPlayerTimer <= nextPlayerLogTime && currentPlayerTimer > 0)
        {
            nextPlayerLogTime -= 10f;
        }

        // Jika Player Timer Habis (AUTO-MOVE)
        if (currentPlayerTimer <= 0)
        {
            currentPlayerTimer = 0;

            // Random move executed when player timer goes off
            int currentTeam = turnManager.isWhiteTurn ? 0 : 1;
            boardManager.ExecuteRandomMove(currentTeam, isFromTimeout: true);
        }
    }

    private void ResetPlayerTimer(bool isWhiteTurn)
    {
        currentPlayerTimer = playerTimerDuration;
        nextPlayerLogTime = currentPlayerTimer - 10f;
    }

    public void PauseTimer()
    {
        isTimerActive = false;
    }

    public void ResumeTimer()
    {
        isTimerActive = true;
    }

    public void ResetTimers()
    {
        currentGlobalTimer = globalTimerDuration;
        currentPlayerTimer = playerTimerDuration;

        nextGlobalLogTime = currentGlobalTimer - 60f;
        nextPlayerLogTime = currentPlayerTimer - 10f;

        PushTimerDisplayToUI();
    }

    public void PushTimerDisplayToUI()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnTimerDisplay(currentPlayerTimer);
            UIManager.Instance.UpdateGlobalTimerDisplay(currentGlobalTimer);
        }
    }

    private void OnDestroy()
    {
        if (turnManager != null) turnManager.OnTurnChanged -= ResetPlayerTimer;
    }
}