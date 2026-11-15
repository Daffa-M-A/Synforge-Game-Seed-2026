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
        if (captor == null || victim == null) return CaptureConditionType.Pattern;

        int dx = Mathf.Abs(victim.currentX - captor.currentX);
        int dy = Mathf.Abs(victim.currentY - captor.currentY);
        bool isAdjacent = dx <= 1 && dy <= 1 && (dx + dy) > 0;

        if (isAdjacent) return CaptureConditionType.Adjacent;

        return CaptureConditionType.Pattern;
    }
}
