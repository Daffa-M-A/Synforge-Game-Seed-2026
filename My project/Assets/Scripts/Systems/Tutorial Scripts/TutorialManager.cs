using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Data")]
    public bool isTutorialActive = true;
    public bool isGameplayPhaseActive = false;
    public TutorialStepData[] tutorialStages;
    public int currentStageIndex = 0;
    private int currentObjectiveIndex = 0;
    public float enemyMoveDelay = 0.75f;

    private void Start()
    {
        if (ChessBoardManager.Instance != null && ChessBoardManager.Instance.turnManager != null)
        {
            ChessBoardManager.Instance.turnManager.OnTurnChanged += HandleTurnChangedForAutoMove;
        }

        if (isTutorialActive && tutorialStages.Length > 0)
        {
            LoadStage(0);
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void LoadStage(int index)
    {
        if (index >= tutorialStages.Length)
        {
            isTutorialActive = false;

            if (TutorialUIManager.Instance != null)
            {
                TutorialUIManager.Instance.HideAllTutorialUI();
            }
            return;
        }

        currentStageIndex = index;
        currentObjectiveIndex = 0;
        isGameplayPhaseActive = false;

        TutorialStepData stageData = tutorialStages[currentStageIndex];

        if (TutorialUIManager.Instance != null)
        {
            TutorialUIManager.Instance.ShowStageUI(stageData);
        }

        if (ChessBoardManager.Instance != null)
        {
            ChessBoardManager.Instance.SetupTutorialBoard(stageData.playerPieces, stageData.enemyPieces);

            if (stageData.forceWhiteTurnOnStart && ChessBoardManager.Instance.turnManager != null)
            {
                ChessBoardManager.Instance.turnManager.ForceSetTurn(true);
            }
        }
    }

    public void StartGameplayPhase()
    {
        isGameplayPhaseActive = true;
    }

    public bool IsFreePlayStage()
    {
        if (!isTutorialActive || tutorialStages.Length == 0) return false;
        var objs = tutorialStages[currentStageIndex].objectives;
        if (objs != null && objs.Count > 0)
        {
            return objs[0].type == TutorialObjective.ObjectiveType.FreePlay;
        }
        return false;
    }

    public bool ValidateMove(ChessPiece piece, Vector2Int targetPos)
    {
        if (!isTutorialActive) return true;
        if (!isGameplayPhaseActive) return false;

        TutorialStepData currentData = tutorialStages[currentStageIndex];
        if (currentData.objectives == null || currentData.objectives.Count == 0) return true;

        TutorialObjective currentObj = currentData.objectives[currentObjectiveIndex];
        bool isMoveCorrect = false;

        switch (currentObj.type)
        {
            case TutorialObjective.ObjectiveType.MoveToTile:
                if (piece.type == currentObj.pieceToMove && targetPos == currentObj.targetPos)
                {
                    isMoveCorrect = true;
                }
                break;

            case TutorialObjective.ObjectiveType.CustomAnyMove:
                isMoveCorrect = true;
                break;

            case TutorialObjective.ObjectiveType.CaptureEnemy:
                ChessPiece targetEnemy = ChessBoardManager.Instance.chessPieces[targetPos.x, targetPos.y];

                if (targetEnemy != null && targetEnemy.team != piece.team)
                {
                    if (piece.type == currentObj.pieceToMove)
                    {
                        isMoveCorrect = true;
                    }
                }
                break;

            case TutorialObjective.ObjectiveType.FreePrisoner:
                ChessPiece targetPrisoner = ChessBoardManager.Instance.prisonerPieces[targetPos.x, targetPos.y];

                if (targetPrisoner != null && targetPrisoner.team == piece.team)
                {
                    if (piece.type == currentObj.pieceToMove)
                    {
                        isMoveCorrect = true;
                    }
                }
                break;
            case TutorialObjective.ObjectiveType.CaptureFortress:
                Vector2Int enemyFortress = (piece.team == 0) ? new Vector2Int(3, 15) : new Vector2Int(3, 0);

                if (targetPos == enemyFortress)
                {
                    if (piece.type == currentObj.pieceToMove)
                    {
                        isMoveCorrect = true;
                    }
                }
                break;
            case TutorialObjective.ObjectiveType.FreePlay:
                return true;
        }

        if (isMoveCorrect)
        {
            currentObjectiveIndex++;

            if (currentObjectiveIndex >= currentData.objectives.Count)
            {
                isGameplayPhaseActive = false;
                Invoke("LoadNextStage", 1.5f); 
            }

            return true;
        }

        return false; 
    }

    public void NotifyAutoMoveExecuted()
    {
        if (!isTutorialActive) return;

        TutorialStepData currentData = tutorialStages[currentStageIndex];
        if (currentData.objectives == null || currentData.objectives.Count == 0) return;

        TutorialObjective currentObj = currentData.objectives[currentObjectiveIndex];

        if (currentObj.type != TutorialObjective.ObjectiveType.WaitForTimer) return;

        currentObjectiveIndex++;

        if (currentObjectiveIndex >= currentData.objectives.Count)
        {
            Invoke("LoadNextStage", 1.5f);
        }
    }

    public bool ShouldSkipEnemyTurn()
    {
        if (!isTutorialActive) return false;

        return tutorialStages[currentStageIndex].skipEnemyTurn;
    }

    private void HandleTurnChangedForAutoMove(bool isWhiteTurn)
    {
        if (!isTutorialActive || !isGameplayPhaseActive) return;
        if (currentStageIndex >= tutorialStages.Length) return;
        if (isWhiteTurn) return; // cuma trigger pas giliran hitam

        TutorialStepData stage = tutorialStages[currentStageIndex];
        if (!stage.enemyAutoMove) return;

        StartCoroutine(EnemyAutoMoveDelayed());
    }

    private IEnumerator EnemyAutoMoveDelayed()
    {
        yield return new WaitForSeconds(enemyMoveDelay);

        if (ChessBoardManager.Instance != null)
        {
            ChessBoardManager.Instance.ExecuteRandomMove(1);
        }
    }

    public void LoadNextStage()
    {
        LoadStage(currentStageIndex + 1);
    }

    private void OnDestroy()
    {
        if (ChessBoardManager.Instance != null && ChessBoardManager.Instance.turnManager != null)
        {
            ChessBoardManager.Instance.turnManager.OnTurnChanged -= HandleTurnChangedForAutoMove;
        }
    }
}