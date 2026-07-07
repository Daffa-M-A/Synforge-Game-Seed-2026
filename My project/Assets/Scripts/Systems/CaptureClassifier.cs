using UnityEngine;

public enum CaptureConditionType
{
    Pattern,
    Adjacent,
    Sacrifice
}

public static class CaptureClassifier
{
    public static CaptureConditionType Classify(ChessPiece captor, ChessPiece victim)
    {
        bool amIInMyTerritory = (captor.team == 0 && captor.currentY <= 7) || (captor.team == 1 && captor.currentY >= 8);
        bool isEnemyInTheirTerritory = (captor.team == 0 && victim.currentY >= 8) || (captor.team == 1 && victim.currentY <= 7);

        if (amIInMyTerritory && isEnemyInTheirTerritory && captor.orderCharge < victim.orderCharge)
        {
            return CaptureConditionType.Sacrifice;
        }

        int dx = Mathf.Abs(victim.currentX - captor.currentX);
        int dy = Mathf.Abs(victim.currentY - captor.currentY);
        bool isAdjacent = dx <= 1 && dy <= 1 && (dx + dy) > 0;

        if (isAdjacent && captor.orderCharge > victim.orderCharge)
        {
            return CaptureConditionType.Adjacent;
        }

        return CaptureConditionType.Pattern;
    }
}
