using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public bool IsWhiteTurn { get; private set; } = true;

    public Action<bool> OnTurnChanged;

    public void EndTurn()
    {
        IsWhiteTurn = !IsWhiteTurn;

        OnTurnChanged?.Invoke(IsWhiteTurn);
    }
}