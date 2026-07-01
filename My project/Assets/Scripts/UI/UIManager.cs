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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged += UpdateTurnUI;
        }

        HidePieceInfo();
    }

    private void OnDestroy()
    {
        if (turnManager != null)
        {
            turnManager.OnTurnChanged -= UpdateTurnUI;
        }
    }

    public void UpdateTeamStats(int maxPieces, int whiteJailCount, int blackJailCount)
    {
        int whiteAlive = maxPieces - whiteJailCount;
        int blackAlive = maxPieces - blackJailCount;

        if (whitePieceCountText != null) whitePieceCountText.text = $"{whiteAlive}/{maxPieces}";
        if (blackPieceCountText != null) blackPieceCountText.text = $"{blackAlive}/{maxPieces}";

        if (whitePieceIcons != null)
        {
            for (int i = 0; i < whitePieceIcons.Length; i++)
            {
                if (whitePieceIcons[i] != null)
                {
                    whitePieceIcons[i].SetActive(i < whiteAlive);
                }
            }
        }

        if (blackPieceIcons != null)
        {
            for (int i = 0; i < blackPieceIcons.Length; i++)
            {
                if (blackPieceIcons[i] != null)
                {
                    blackPieceIcons[i].SetActive(i < blackAlive);
                }
            }
        }
    }

    private void UpdateTurnUI(bool isWhiteTurn)
    {
        if (turnIndicatorText != null)
        {
            turnIndicatorText.text = isWhiteTurn ? "Giliran TIM A" : "Giliran TIM B";
        }

        if (turnCountText != null && turnManager != null)
        {
            turnCountText.text = $"TURN {turnManager.currentTurnCount}";
        }
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
            chargeOrderContent.parent.parent.gameObject.SetActive(false); 
            return;
        }
        else
        {
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

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}