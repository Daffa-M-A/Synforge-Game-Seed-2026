using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChargeLogItem : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI logText;
    [Tooltip("Image latar panel. Kosongkan untuk otomatis memakai Image di object ini.")]
    public Image background;

    [Header("Team Colors")]
    [Tooltip("Tim merah (team 0 / White).")]
    public Color redTeamColor = new Color32(0xC0, 0x52, 0x4A, 0xFF);
    [Tooltip("Tim biru (team 1 / Black).")]
    public Color blueTeamColor = new Color32(0x57, 0x9E, 0xDB, 0xFF);

    // sequence = nomor urut charge seumur match (01, 02, ... 23, ...), yang terbaru paling atas.
    public void SetupData(ChessPiece piece)
    {
        if (piece == null) return;

        if (nameText != null) nameText.text = piece.pieceName;
        if (logText != null) logText.text = piece.chargeSequence.ToString("00");

        if (background == null) background = GetComponent<Image>();
        if (background != null)
            background.color = piece.team == 0 ? redTeamColor : blueTeamColor;
    }
}
