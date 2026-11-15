using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Turn State")]
    public bool isWhiteTurn = true;
    public int currentTurnCount = 1;

    public event Action<bool> OnTurnChanged;
    public event Action<bool> OnTurnEnded;
    public event Action<int> OnOwnerTurnEnding;

    private void Start()
    {
        OnTurnChanged?.Invoke(isWhiteTurn);
    }

    public void EndTurn(bool forcedByTimeout = false)
    {
        int endingTeam = isWhiteTurn ? 0 : 1;
        OnOwnerTurnEnding?.Invoke(endingTeam);

        isWhiteTurn = !isWhiteTurn;

        currentTurnCount++;

        OnTurnEnded?.Invoke(forcedByTimeout);
        OnTurnChanged?.Invoke(isWhiteTurn);
    }

    public void ResetMatch()
    {
        isWhiteTurn = true;
        currentTurnCount = 1;
        OnTurnChanged?.Invoke(isWhiteTurn);
    }

    public void ForceSetTurn(bool setToWhiteTurn)
    {
        if (isWhiteTurn == setToWhiteTurn) return;

        isWhiteTurn = setToWhiteTurn;
        OnTurnChanged?.Invoke(isWhiteTurn);
    }
}