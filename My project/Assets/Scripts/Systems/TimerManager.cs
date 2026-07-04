using UnityEngine;

public class TimerManager : MonoBehaviour
{

    private bool globalEndingTriggered = false;
    private int lastLoggedSecond = -1;

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

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("TimetickingSFX"); //sfx
    }

    private void Update()
    {
        if (!isTimerActive || boardManager == null || boardManager.isGameOver) return;

        // Logic for global timer
        currentGlobalTimer -= Time.deltaTime;

        // Global Timer Ending (1 Menit Terakhir)
        if (currentGlobalTimer <= 60f && currentGlobalTimer > 0 && !globalEndingTriggered)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("GlobalTimerEndingSFX");
            globalEndingTriggered = true; // Tambahkan variabel bool ini di atas
        }        

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTurnTimerDisplay(currentPlayerTimer);
            UIManager.Instance.UpdateGlobalTimerDisplay(currentGlobalTimer);
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

            if (matchManager != null)
                matchManager.ExecuteTiebreaker(); 

            return;
        }

        // Logic for player timer
        currentPlayerTimer -= Time.deltaTime;

        // Turn Timer Ending (10 Detik Terakhir)
        int secondsLeft = Mathf.CeilToInt(currentPlayerTimer);
        if (secondsLeft <= 10 && secondsLeft > 0 && secondsLeft != lastLoggedSecond)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("TurnTimerEndingSFX");
            lastLoggedSecond = secondsLeft; // Tambahkan variabel int ini di atas
        }        

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

            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("TurnChangeForcedSFX");

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