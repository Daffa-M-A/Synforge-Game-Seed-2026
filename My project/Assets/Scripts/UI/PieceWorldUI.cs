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
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (piece == null || rankText == null) return;

        // Update angka sesuai orderCharge bidak
        // Jika 0 (belum keluar base), sembunyikan atau biarkan kosong
        rankText.text = piece.orderCharge > 0 ? piece.orderCharge.ToString() : "";

        // Billboard: Membuat teks selalu menghadap kamera
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }
}