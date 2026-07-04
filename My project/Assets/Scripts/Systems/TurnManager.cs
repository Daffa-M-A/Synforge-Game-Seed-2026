using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Turn State")]
    public bool isWhiteTurn = true;
    public int currentTurnCount = 1;

    public event Action<bool> OnTurnChanged;

    private void Start()
    {
        OnTurnChanged?.Invoke(isWhiteTurn);
    }

    public void EndTurn()
    {
        isWhiteTurn = !isWhiteTurn;
        currentTurnCount++;

        if (AudioManager.Instance != null)
        {
            // Jika dipicu secara normal (bukan karena timer habis)
            AudioManager.Instance.PlaySFX("TurnChangeNormalSFX");
        }

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