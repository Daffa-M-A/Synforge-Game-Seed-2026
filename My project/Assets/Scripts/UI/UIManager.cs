using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Dependencies")]
    public TurnManager turnManager;
    public TimerManager timerManager;
    public ChessBoardManager boardManager;

    [Header("Teams (Player & Enemy)")]
    public TextMeshProUGUI whitePieceCountText;
    public TextMeshProUGUI blackPieceCountText;

    [Header("Turn Info")]
    public TextMeshProUGUI turnIndicatorText;
    public TextMeshProUGUI turnCountText;

    [Header("Timers")]
    public TextMeshProUGUI globalTimerText;
    public TextMeshProUGUI turnTimerText;

    [Header("Held Piece Info")]
    public GameObject pieceInfoPanel;
    public TextMeshProUGUI pieceNameText;
    public TextMeshProUGUI pieceRoleText;

    [Header("Charge Order (List)")]
    public RectTransform chargeOrderContent;
    public GameObject chargeLogPrefab;

    [Header("Sprite Assets")]
    public GameObject[] whitePieceIcons;
    public GameObject[] blackPieceIcons;

    [Header("Game Over UI")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject drawPanel;

    [Header("Turn Info")]
        public GameObject redTeamTurnUI;   // drag "Red Team Turn" dari prefab
        public GameObject blueTeamTurnUI;  // drag "Blue Team Turn" dari prefab

    [Header("Pause & Countdown Settings")]
        public GameObject settingPanel; // Panel Setting/Pause
        public GameObject gameplayUI;   // UI HUD saat main
        public GameObject countdownOverlay; // Object yang berisi Text 3-2-1
        public TextMeshProUGUI countdownText;

        private bool isPaused = false;
        public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        ResolveDependencies();
    }

    private void Start()
    {
        ResolveDependencies();

        if (turnManager != null)
        {
            turnManager.OnTurnChanged += UpdateTurnUI;
            UpdateTurnUI(turnManager.isWhiteTurn);
        }

        if (timerManager != null)
            timerManager.PushTimerDisplayToUI();

        if (boardManager != null && MatchManager.Instance != null)
        {
            UpdateChargeList(boardManager.chessPieces);
            UpdateTeamStats(
                MatchManager.Instance.maxPiecesPerTeam,
                boardManager.whiteJailCount,
                boardManager.blackJailCount);
        }

        HidePieceInfo();
    }

    private void ResolveDependencies()
    {
        if (turnManager == null) turnManager = GetComponent<TurnManager>();
        if (timerManager == null) timerManager = GetComponent<TimerManager>();
        if (boardManager == null) boardManager = FindFirstObjectByType<ChessBoardManager>();
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= UpdateTurnUI;
        }
    }

    // --- PERBAIKAN DI SINI: Menambahkan kurung kurawal yang hilang ---
    public void UpdateTeamStats(int maxPieces, int whiteJailCount, int blackJailCount)
    {
        int whiteAlive = maxPieces - whiteJailCount;
        int blackAlive = maxPieces - blackJailCount;

        if (whitePieceCountText != null) whitePieceCountText.text = $"{whiteAlive}/{maxPieces}";
        if (blackPieceCountText != null) blackPieceCountText.text = $"{blackAlive}/{maxPieces}";

        // Panggil fungsi pembantu untuk update checkmark
        UpdateTeamCheckmarks(whitePieceIcons, whiteAlive);
        UpdateTeamCheckmarks(blackPieceIcons, blackAlive);
    }

    private void UpdateTeamCheckmarks(GameObject[] icons, int aliveCount)
    {
        if (icons == null) return;
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;

            bool isAlive = i < aliveCount;

            Transform checkmark = icons[i].transform.Find("Checkmark");
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(isAlive);
            }
            else
            {
                UnityEngine.UI.Toggle toggle = icons[i].GetComponent<UnityEngine.UI.Toggle>();
                if (toggle != null)
                {
                    toggle.isOn = isAlive;
                }
            }
        }
    }

    private void UpdateTurnUI(bool isWhiteTurn)
    {
        // isWhiteTurn true = giliran tim merah (team 0)
        if (redTeamTurnUI != null)
            redTeamTurnUI.SetActive(isWhiteTurn);

        if (blueTeamTurnUI != null)
            blueTeamTurnUI.SetActive(!isWhiteTurn);

        if (turnCountText != null && turnManager != null)
            turnCountText.text = $"TURN {turnManager.currentTurnCount}";
    }
    
public void OpenPauseMenu()
    {
        if (isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (AudioManager.Instance != null) AudioManager.Instance.SetGameplayAudioPaused(true);
        if (timerManager != null) timerManager.PauseTimer();
        if (gameplayUI != null) gameplayUI.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(true);
        if (countdownOverlay != null) countdownOverlay.SetActive(false);
    }

    public void OnContinueClicked()
    {
        StartCoroutine(ResumeWithCountdown());
    }

    private System.Collections.IEnumerator ResumeWithCountdown()
{
    // 1. Matikan Panel Setting SEGERA setelah tombol diklik
    if (settingPanel != null) 
    {
        settingPanel.SetActive(false);
    }

    // 2. Aktifkan Overlay Countdown
    if (countdownOverlay != null)
    {
        countdownOverlay.SetActive(true);
        
        for (int i = 3; i >= 1; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            // Gunakan WaitForSecondsRealtime karena Time.timeScale sedang 0
            yield return new WaitForSecondsRealtime(1f);
        }
        
        // Matikan overlay setelah selesai
        countdownOverlay.SetActive(false);
    }

    // 3. Kembalikan waktu ke normal dan nyalakan HUD gameplay
    ClosePauseMenu();
}

    private void ClosePauseMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (settingPanel != null) settingPanel.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(true);
        if (AudioManager.Instance != null) AudioManager.Instance.SetGameplayAudioPaused(false);
        if (timerManager != null) timerManager.ResumeTimer();
    }

    public void OnRetryFromPauseClicked()
    {
        Time.timeScale = 1f; // Sangat penting!
        OnReplayClicked();
    }

    public void OnMainMenuFromPauseClicked()
    {
        Time.timeScale = 1f; // Sangat penting!
        OnMainMenuClicked();
    }

    public void UpdateChargeList(ChessPiece[,] activePieces)
    {
        for (int i = chargeOrderContent.childCount - 1; i >= 0; i--)
        {
            Destroy(chargeOrderContent.GetChild(i).gameObject);
        }

        System.Collections.Generic.List<ChessPiece> allPieces = new System.Collections.Generic.List<ChessPiece>();
        for (int x = 0; x < 8; x++) 
        {
            for (int y = 0; y < 16; y++) 
            {
                if (activePieces[x, y] != null && !activePieces[x, y].isGhost && activePieces[x, y].orderCharge > 0)
                {
                    allPieces.Add(activePieces[x, y]);
                }
            }
        }

        if (allPieces.Count == 0)
        {
            if (chargeOrderContent.parent.parent != null)
                chargeOrderContent.parent.parent.gameObject.SetActive(false); 
            return;
        }
        else
        {
            if (chargeOrderContent.parent.parent != null)
                chargeOrderContent.parent.parent.gameObject.SetActive(true); 
        }

        allPieces.Sort((a, b) => b.orderCharge.CompareTo(a.orderCharge));

        for (int i = 0; i < allPieces.Count; i++)
        {
            GameObject newPanel = Instantiate(chargeLogPrefab, chargeOrderContent);
            ChargeLogItem logItem = newPanel.GetComponent<ChargeLogItem>();

            if (logItem != null)
            {
                logItem.SetupData(i + 1, allPieces[i].pieceName, allPieces[i].orderCharge);
            }
        }
    }
    
    public void UpdateGlobalTimerDisplay(float timeRemaining)
    {
        if (globalTimerText == null) return;
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);
        globalTimerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void UpdateTurnTimerDisplay(float timeRemaining)
    {
        if (turnTimerText == null) return;
        turnTimerText.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
    }

    public void ShowPieceInfo(ChessPiece piece)
    {
        if (pieceInfoPanel != null) pieceInfoPanel.SetActive(true);
        if (pieceNameText != null) pieceNameText.text = piece.pieceName;
        if (pieceRoleText != null) pieceRoleText.text = piece.roleName;
    }

    public void HidePieceInfo()
    {
        if (pieceInfoPanel != null) pieceInfoPanel.SetActive(false);
    }

    public void LoadLevel(string sceneName)
    {
        int levelPilihan = (sceneName == "Prototype Chess") ? 2 : 1;
        PlayerPrefs.SetInt("LevelTerpilih", levelPilihan);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    public void ShowGameOverUI(int resultTeam)
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);

        if (resultTeam == 0 && winPanel != null) winPanel.SetActive(true);
        else if (resultTeam == 1 && losePanel != null) losePanel.SetActive(true);
        else if (resultTeam == -1 && drawPanel != null) drawPanel.SetActive(true);
    }

    public void OnReplayClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}