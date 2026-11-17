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

        SetupZoneTracker();
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
        if (boardManager != null)
        {
            boardManager.OnZonePointsChanged -= OnZonePointsChanged;
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

        RefreshZoneTracker();
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
        System.Collections.Generic.HashSet<ChessPiece> seen = new System.Collections.Generic.HashSet<ChessPiece>();

        void AddChargedFrom(ChessPiece[,] grid)
        {
            if (grid == null) return;
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 16; y++)
                {
                    ChessPiece piece = grid[x, y];
                    if (piece != null && !piece.isGhost && piece.orderCharge > 0 && seen.Add(piece))
                    {
                        allPieces.Add(piece);
                    }
                }
            }
        }

        AddChargedFrom(activePieces);
        if (ChessBoardManager.Instance != null)
        {
            AddChargedFrom(ChessBoardManager.Instance.prisonerPieces);
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

        // Yang paling baru charge (nomor urut terbesar) di paling atas.
        allPieces.Sort((a, b) => b.chargeSequence.CompareTo(a.chargeSequence));

        for (int i = 0; i < allPieces.Count; i++)
        {
            GameObject newPanel = Instantiate(chargeLogPrefab, chargeOrderContent);
            ChargeLogItem logItem = newPanel.GetComponent<ChargeLogItem>();

            if (logItem != null)
            {
                logItem.SetupData(allPieces[i]);
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

    // Panel "Pawn Information" lama sekarang menjadi UI Zone Domination Tracker dan selalu tampil,
    // jadi klik bidak tidak lagi mengubah isinya.
    public void ShowPieceInfo(ChessPiece piece) { }

    public void HidePieceInfo() { }

    // ---- UI Zone Domination Tracker ----
    [Header("Zone Domination Tracker")]
    public string redZoneLabel = "Red Team: Zone Domination Points";
    public string blueZoneLabel = "Blue Team: Zone Domination Points";
    public Color zoneBarRedColor = new Color32(0xC0, 0x52, 0x4A, 0xFF);
    public Color zoneBarBlueColor = new Color32(0x57, 0x9E, 0xDB, 0xFF);
    public Color zoneBarBackgroundColor = new Color(0f, 0f, 0f, 0.35f);
    [Tooltip("Tinggi bar di tempat teks Pawn Type lama.")]
    public float zoneBarHeight = 22f;

    private UnityEngine.UI.Image zoneBarFill;
    private RectTransform zoneBarFillRect;
    private TextMeshProUGUI zoneBarValueText;
    private bool zoneTrackerReady = false;

    private GameObject ResolveZoneTrackerPanel()
    {
        if (pieceInfoPanel != null) return pieceInfoPanel;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "UI Zone Domination Tracker" || t.name == "Pawn Information")
                return t.gameObject;
        }
        return null;
    }

    private void SetupZoneTracker()
    {
        if (zoneTrackerReady) return;

        GameObject panel = ResolveZoneTrackerPanel();
        if (panel == null) return;
        pieceInfoPanel = panel;
        panel.name = "UI Zone Domination Tracker";

        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in texts)
        {
            if (pieceNameText == null && t.name == "Pawn Name") pieceNameText = t;
            if (pieceRoleText == null && t.name == "Pawn Type") pieceRoleText = t;
        }

        if (pieceRoleText != null)
        {
            // Bar menggantikan teks Pawn Type: rect yang sama, dipotong setinggi zoneBarHeight.
            RectTransform src = pieceRoleText.rectTransform;
            pieceRoleText.gameObject.SetActive(false);

            GameObject bgGo = new GameObject("ZoneBarBackground", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.SetParent(src.parent, false);
            bgRect.anchorMin = src.anchorMin;
            bgRect.anchorMax = src.anchorMax;
            bgRect.pivot = src.pivot;
            bgRect.anchoredPosition = src.anchoredPosition;
            bgRect.sizeDelta = src.sizeDelta;
            bgRect.SetSiblingIndex(src.GetSiblingIndex() + 1);

            // Kunci tinggi bar: ubah anchor vertikal jadi titik tengah rect sumber
            float midY = 0.5f * (src.anchorMin.y + src.anchorMax.y);
            bgRect.anchorMin = new Vector2(src.anchorMin.x, midY);
            bgRect.anchorMax = new Vector2(src.anchorMax.x, midY);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(src.sizeDelta.x, zoneBarHeight);
            bgRect.anchoredPosition = new Vector2(src.anchoredPosition.x, src.anchoredPosition.y);

            UnityEngine.UI.Image bgImg = bgGo.GetComponent<UnityEngine.UI.Image>();
            bgImg.color = zoneBarBackgroundColor;
            bgImg.raycastTarget = false;

            GameObject fillGo = new GameObject("ZoneBarFill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.SetParent(bgRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            zoneBarFill = fillGo.GetComponent<UnityEngine.UI.Image>();
            zoneBarFill.raycastTarget = false;
            zoneBarFillRect = fillRect;
            fillRect.anchorMax = new Vector2(0f, 1f); // lebar isi diatur lewat anchorMax.x

            // Angka "40 / 100" di atas bar, memakai font yang sama dengan Pawn Type
            zoneBarValueText = Instantiate(pieceRoleText, bgRect);
            zoneBarValueText.gameObject.SetActive(true);
            zoneBarValueText.name = "ZoneBarValue";
            RectTransform vt = zoneBarValueText.rectTransform;
            vt.anchorMin = Vector2.zero;
            vt.anchorMax = Vector2.one;
            vt.offsetMin = Vector2.zero;
            vt.offsetMax = Vector2.zero;
            zoneBarValueText.enableAutoSizing = false;
            zoneBarValueText.fontSize = Mathf.Max(10f, zoneBarHeight * 0.7f);
            zoneBarValueText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            zoneBarValueText.verticalAlignment = VerticalAlignmentOptions.Middle;
            zoneBarValueText.color = Color.white;
            zoneBarValueText.raycastTarget = false;
        }

        if (boardManager != null) boardManager.OnZonePointsChanged += OnZonePointsChanged;

        panel.SetActive(true);
        zoneTrackerReady = true;
        RefreshZoneTracker();
    }

    private void OnZonePointsChanged(int team, int total, int delta)
    {
        RefreshZoneTracker();
    }

    // Menampilkan poin tim yang sedang mendapat giliran.
    private void RefreshZoneTracker()
    {
        if (!zoneTrackerReady) return;

        int team = (turnManager == null || turnManager.isWhiteTurn) ? 0 : 1;
        int max = boardManager != null ? boardManager.zoneWinPoints : 100;
        int points = boardManager != null ? boardManager.GetZonePoints(team) : 0;

        if (pieceNameText != null) pieceNameText.text = team == 0 ? redZoneLabel : blueZoneLabel;

        if (zoneBarFill != null && zoneBarFillRect != null)
        {
            float frac = max > 0 ? Mathf.Clamp01((float)points / max) : 0f;
            zoneBarFillRect.anchorMax = new Vector2(frac, 1f);
            zoneBarFillRect.offsetMin = new Vector2(2f, 2f);
            zoneBarFillRect.offsetMax = new Vector2(frac > 0f ? -2f : 0f, -2f);
            zoneBarFill.enabled = frac > 0f;
            zoneBarFill.color = team == 0 ? zoneBarRedColor : zoneBarBlueColor;
        }
        if (zoneBarValueText != null) zoneBarValueText.text = $"{points} / {max}";
        // BG Image (Blue/Red) sengaja dibiarkan apa adanya.
    }

    public void StartPvPGame(string sceneName)
    {
        PlayerPrefs.SetInt("IsPvE", 0);
        LoadLevel(sceneName);
    }

    public void StartPvEGame(string sceneName)
    {
        PlayerPrefs.SetInt("IsPvE", 1);
        LoadLevel(sceneName);
    }

    public void LoadLevel(string sceneName)
    {
        int levelPilihan = (sceneName == "Prototype Chess") ? 2 : 1;
        PlayerPrefs.SetInt("LevelTerpilih", levelPilihan);
        PlayerPrefs.Save();
        SceneManager.LoadScene(sceneName);
    }

    [Header("Game Over Victory Icons")]
    [Tooltip("Ikon kemenangan tim merah (team 0 / White).")]
    public Texture2D victoryIconRed;
    [Tooltip("Ikon kemenangan tim biru (team 1 / Black).")]
    public Texture2D victoryIconBlue;
    [Tooltip("Tinggi ikon di tengah panel (lebar mengikuti rasio gambar).")]
    public float victoryIconHeight = 320f;
    public string redWinRibbonText = "Tim Merah Menang";
    public string blueWinRibbonText = "Tim Biru Menang";
    public string drawRibbonText = "Seri";

    public void ShowGameOverUI(int resultTeam)
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (drawPanel != null) drawPanel.SetActive(false);

        GameObject active = null;
        if (resultTeam == 0) active = winPanel;
        else if (resultTeam == 1) active = losePanel;
        else if (resultTeam == -1) active = drawPanel;

        if (active == null) return;
        SetupGameOverPanel(active, resultTeam);
        active.SetActive(true);
    }

    // Mengisi ribbon "Team Win(TMP)", menyembunyikan Panel Blue/Panel Red beserta teks N/A,
    // lalu menaruh VictoryIcon tim pemenang di tengah container "Panels".
    private void SetupGameOverPanel(GameObject root, int resultTeam)
    {
        Transform[] all = root.GetComponentsInChildren<Transform>(true);

        Transform iconContainer = null;
        foreach (Transform t in all)
        {
            if (t.name == "Panel Blue" || t.name == "Panel Red")
            {
                t.gameObject.SetActive(false);
                iconContainer = t.parent;
            }
        }

        // Teks "VS" disembunyikan dan posisinya dipakai sebagai tempat ikon kemenangan.
        RectTransform vsRect = null;
        foreach (TextMeshProUGUI tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string txt = tmp.text != null ? tmp.text.Trim() : "";

            if (tmp.name == "Team Win(TMP)")
            {
                tmp.text = resultTeam == 0 ? redWinRibbonText
                         : resultTeam == 1 ? blueWinRibbonText
                         : drawRibbonText;
                tmp.gameObject.SetActive(true);
            }
            else if (txt == "N/A")
            {
                tmp.gameObject.SetActive(false);
            }
            else if (string.Equals(txt, "VS", System.StringComparison.OrdinalIgnoreCase))
            {
                tmp.gameObject.SetActive(false);
                vsRect = tmp.rectTransform;
            }
        }

        if (vsRect != null) iconContainer = vsRect.parent;
        if (iconContainer == null) return;

        Texture2D tex = resultTeam == 0 ? victoryIconRed : resultTeam == 1 ? victoryIconBlue : null;

        Transform existing = iconContainer.Find("VictoryIcon");
        if (tex == null)
        {
            if (existing != null) existing.gameObject.SetActive(false);
            return;
        }

        UnityEngine.UI.RawImage img;
        if (existing != null)
        {
            img = existing.GetComponent<UnityEngine.UI.RawImage>();
        }
        else
        {
            GameObject go = new GameObject("VictoryIcon", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            go.transform.SetParent(iconContainer, false);
            img = go.GetComponent<UnityEngine.UI.RawImage>();
        }

        RectTransform rt = img.rectTransform;
        if (vsRect != null)
        {
            // Tepat di tempat "VS" tadi berada
            rt.anchorMin = vsRect.anchorMin;
            rt.anchorMax = vsRect.anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = vsRect.anchoredPosition;
            rt.SetSiblingIndex(vsRect.GetSiblingIndex() + 1);
        }
        else
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        rt.sizeDelta = new Vector2(victoryIconHeight * tex.width / tex.height, victoryIconHeight);

        img.texture = tex;
        img.raycastTarget = false;
        img.gameObject.SetActive(true);
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