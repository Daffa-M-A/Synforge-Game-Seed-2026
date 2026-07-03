using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("Canvas & Panels")]
    public Canvas tutorialCanvas;
    public GameObject popupPanel;
    public GameObject instructionPanel;

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI pageNumberText;

    [Header("Buttons")]
    public Button nextButton;
    public Button prevButton;
    public TextMeshProUGUI nextButtonText;

    [Header("Dependencies")]
    public TimerManager timerManager;

    private TutorialStepData currentStageData;
    private int currentPageIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowStageUI(TutorialStepData stageData)
    {
        currentStageData = stageData;
        currentPageIndex = 0;

        tutorialCanvas.gameObject.SetActive(true);
        popupPanel.SetActive(true);
        instructionPanel.SetActive(false);

        if (timerManager != null && currentStageData.enableTimer)
        {
            timerManager.PauseTimer();
        }

        titleText.text = currentStageData.stageTitle;
        instructionText.text = currentStageData.mainInstruction;

        UpdatePageUI();
    }

    private void UpdatePageUI()
    {
        int totalPages = (currentStageData.descriptions != null) ? currentStageData.descriptions.Length : 1;

        if (currentStageData.descriptions != null && currentStageData.descriptions.Length > 0)
        {
            descriptionText.text = currentStageData.descriptions[currentPageIndex];
        }

        if (pageNumberText != null)
        {
            pageNumberText.text = $"{currentPageIndex + 1} / {totalPages}";
        }

        prevButton.gameObject.SetActive(currentPageIndex > 0);

        bool isLastPage = (currentStageData.descriptions == null) || (currentPageIndex == currentStageData.descriptions.Length - 1);

        if (nextButtonText != null)
        {
            nextButtonText.text = isLastPage ? "Start" : "Next";
        }

        nextButton.onClick.RemoveAllListeners();
        if (isLastPage)
        {
            nextButton.onClick.AddListener(OnStartGameplayClicked);
        }
        else
        {
            nextButton.onClick.AddListener(NextPage);
        }

        prevButton.onClick.RemoveAllListeners();
        prevButton.onClick.AddListener(PrevPage);
    }

    private void NextPage()
    {
        currentPageIndex++;
        UpdatePageUI();
    }

    private void PrevPage()
    {
        currentPageIndex--;
        UpdatePageUI();
    }

    private void OnStartGameplayClicked()
    {
        if (timerManager != null && currentStageData.enableTimer)
        {
            timerManager.ResetTimers();
            timerManager.ResumeTimer();
        }

        if (currentStageData.objectives == null || currentStageData.objectives.Count == 0)
        {
            TutorialManager.Instance.LoadNextStage();
        }
        else
        {
            popupPanel.SetActive(false);
            instructionPanel.SetActive(true);

            TutorialManager.Instance.StartGameplayPhase();
        }
    }

    public void HideAllTutorialUI()
    {
        tutorialCanvas.gameObject.SetActive(false);
    }
}