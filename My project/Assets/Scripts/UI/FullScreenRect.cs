using UnityEngine;

// Membuat RectTransform ini selalu menutupi seluruh layar (root canvas),
// apa pun posisi/ukuran parent-nya. Dipakai untuk Dimmer (lapisan gelap)
// dan Exit Panel (area klik di luar menu) supaya tetap menutup penuh
// di rasio layar selain 16:9 (canvas Expand bisa lebih lebar/tinggi dari 1920x1080).
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class FullScreenRect : MonoBehaviour
{
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private readonly Vector3[] corners = new Vector3[4];

    private void OnEnable()
    {
        rectTransform = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        if (canvasRect == null) return;

        Transform parent = rectTransform.parent;
        if (parent == null) return;

        canvasRect.GetWorldCorners(corners);
        Vector2 bottomLeft = parent.InverseTransformPoint(corners[0]);
        Vector2 topRight = parent.InverseTransformPoint(corners[2]);

        Vector2 size = topRight - bottomLeft;
        Vector2 center = (bottomLeft + topRight) * 0.5f;

        // Anchor tengah: anchoredPosition diukur dari titik tengah rect parent.
        RectTransform parentRect = parent as RectTransform;
        Vector2 parentCenter = parentRect != null ? parentRect.rect.center : Vector2.zero;
        Vector2 targetPos = center - parentCenter;

        Vector2 half = new Vector2(0.5f, 0.5f);
        if (rectTransform.anchorMin != half) rectTransform.anchorMin = half;
        if (rectTransform.anchorMax != half) rectTransform.anchorMax = half;
        if (rectTransform.pivot != half) rectTransform.pivot = half;
        if ((rectTransform.sizeDelta - size).sqrMagnitude > 0.01f) rectTransform.sizeDelta = size;
        if ((rectTransform.anchoredPosition - targetPos).sqrMagnitude > 0.01f) rectTransform.anchoredPosition = targetPos;
    }
}
