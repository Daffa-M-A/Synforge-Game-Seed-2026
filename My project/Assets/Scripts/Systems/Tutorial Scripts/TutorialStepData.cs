using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PieceSetup
{
    public ChessPieceType pieceType;
    public int team;
    public Vector2Int startPos;
    public int customChargeOrder; 
    public bool isPrisoner;
}

[System.Serializable]
public class TutorialObjective
{
    public enum ObjectiveType { MoveToTile, CaptureEnemy, FreePrisoner, CustomAnyMove, WaitForTimer, CaptureFortress, FreePlay }
    public ObjectiveType type;
    public ChessPieceType pieceToMove;
    public Vector2Int targetPos; 
}

[CreateAssetMenu(fileName = "New Tutorial Stage", menuName = "Bentengan/Tutorial Stage")]
public class TutorialStepData : ScriptableObject
{
    [Header("Popup UI Content")]
    public string stageTitle;
    public bool skipEnemyTurn = true;
    public bool enableTimer = false;
    public bool showObjectiveHighlight = true;
    [TextArea(2, 3)]
    public string mainInstruction; 
    [TextArea(3, 5)]
    public string[] descriptions;

    [Header("Board Setup")]
    public List<PieceSetup> playerPieces;
    public List<PieceSetup> enemyPieces;
    public bool forceWhiteTurnOnStart = false;
    public bool enemyAutoMove = false;
    public bool disableHighlights = false;

    [Header("Win Condition")]
    public List<TutorialObjective> objectives;
}