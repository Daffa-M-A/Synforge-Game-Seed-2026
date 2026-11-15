using UnityEngine;
using TMPro;

public class PieceWorldUI : MonoBehaviour
{
    private ChessPiece piece;
    private TextMeshProUGUI rankText;
    private Transform mainCameraTransform;

    void Start()
    {
        piece = GetComponentInParent<ChessPiece>();
        rankText = GetComponentInChildren<TextMeshProUGUI>();
        if (rankText != null) rankText.richText = true;
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (piece == null || rankText == null) return;

        string chargeText = piece.orderCharge > 0 ? piece.orderCharge.ToString() : "";
        int returnCountdown = piece.GetHomeReturnCountdownDisplay();

        if (returnCountdown > 0)
        {
            string countdownText = $"<size=80%><color=#FFE27A>{returnCountdown}</color></size>";
            rankText.text = string.IsNullOrEmpty(chargeText) ? countdownText : chargeText + "\n" + countdownText;
        }
        else
        {
            rankText.text = chargeText;
        }

        // Billboard: Membuat teks selalu menghadap kamera
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }
}