using UnityEngine;
using TMPro;

public class ChargeLogItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI logText;

    public void SetupData(int rank, string pieceName, int chargeValue)
    {
        nameText.text = $"{rank}. {pieceName}";

        logText.text = chargeValue.ToString("00");
    }
}