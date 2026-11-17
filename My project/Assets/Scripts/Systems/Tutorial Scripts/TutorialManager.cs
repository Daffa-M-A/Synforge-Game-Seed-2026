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

    [Header("Visual Indicators (Material)")]
    public Material objectiveHighlightMaterial;
    private Vector2Int currentHighlightedTile = -Vector2Int.one;
    private Material originalTileMaterial;

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
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void LoadStage(int index)
    {
        if (index >= tutorialStages.Length)
        {
            isTutorialActive = false;
            ClearObjectiveHighlight();

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

            UpdateObjectiveHighlight();

            if (stageData.forceWhiteTurnOnStart && ChessBoardManager.Instance.turnManager != null)
            {
                ChessBoardManager.Instance.turnManager.ForceSetTurn(true);
            }
        }
    }

    public void UpdateObjectiveHighlight()
    {
        ClearObjectiveHighlight();

        if (!isTutorialActive) return;

        TutorialStepData currentData = tutorialStages[currentStageIndex];

        if (!currentData.showObjectiveHighlight || currentData.objectives == null || currentObjectiveIndex >= currentData.objectives.Count)
            return;

        TutorialObjective currentObj = currentData.objectives[currentObjectiveIndex];

        if (currentObj.type == TutorialObjective.ObjectiveType.MoveToTile)
        {
            if (objectiveHighlightMaterial != null && ChessBoardManager.Instance != null && ChessBoardManager.Instance.generateBoard != null)
            {
                int targetX = currentObj.targetPos.x;
                int targetY = currentObj.targetPos.y;

                currentHighlightedTile = new Vector2Int(targetX, targetY);
                originalTileMaterial = ChessBoardManager.Instance.generateBoard.GetTileMaterial(targetX, targetY);

                ChessBoardManager.Instance.generateBoard.SetTileMaterial(targetX, targetY, objectiveHighlightMaterial);
            }
        }
    }

    private void ClearObjectiveHighlight()
    {
        if (currentHighlightedTile != -Vector2Int.one && originalTileMaterial != null)
        {
            if (ChessBoardManager.Instance != null && ChessBoardManager.Instance.generateBoard != null)
            {
                ChessBoardManager.Instance.generateBoard.SetTileMaterial(currentHighlightedTile.x, currentHighlightedTile.y, originalTileMaterial);
            }

            currentHighlightedTile = -Vector2Int.one;
            originalTileMaterial = null;
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

            case TutorialObjective.ObjectiveType.HoldEnemyZone:
                // Pemain boleh bergerak bebas sambil menunggu poin masuk; objective selesai lewat NotifyZonePointsAwarded.
                return true;
        }

        if (isMoveCorrect)
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(AudioManager.SfxTutorialObjectiveCleared);

            currentObjectiveIndex++;

            if (currentObjectiveIndex >= currentData.objectives.Count)
            {
                isGameplayPhaseActive = false;
                ClearObjectiveHighlight();
                Invoke("LoadNextStage", 1.5f); 
            }
            else
            {
                UpdateObjectiveHighlight();
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

    // Dipanggil ChessBoardManager saat sebuah tim mendapat Zone Domination Point.
    public void NotifyZonePointsAwarded(int team, int pointsGained)
    {
        if (!isTutorialActive || !isGameplayPhaseActive) return;
        if (team != 0 || pointsGained <= 0) return;
        if (currentStageIndex >= tutorialStages.Length) return;

        TutorialStepData currentData = tutorialStages[currentStageIndex];
        if (currentData.objectives == null || currentObjectiveIndex >= currentData.objectives.Count) return;

        TutorialObjective currentObj = currentData.objectives[currentObjectiveIndex];
        if (currentObj.type != TutorialObjective.ObjectiveType.HoldEnemyZone) return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.SfxTutorialObjectiveCleared);

        currentObjectiveIndex++;

        if (currentObjectiveIndex >= currentData.objectives.Count)
        {
            isGameplayPhaseActive = false;
            ClearObjectiveHighlight();
            Invoke("LoadNextStage", 1.5f);
        }
        else
        {
            UpdateObjectiveHighlight();
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