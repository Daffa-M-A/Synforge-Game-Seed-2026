using UnityEngine;
using UnityEngine.UI;

// Menjamin CanvasScaler selalu memakai Scale With Screen Size + Expand.
// Expand = area canvas tidak pernah lebih kecil dari reference resolution,
// sehingga seluruh UI selalu muat di layar (16:9, 16:10, 4:3, ultrawide, layar kecil).
// Catatan: mode Shrink justru memotong area canvas, jangan dipakai.
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public class UiResponsiveSetup : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private CanvasScaler.ScreenMatchMode screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
    [Tooltip("Hanya dipakai jika Screen Match Mode = MatchWidthOrHeight.")]
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

    private CanvasScaler canvasScaler;

    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        ApplySettings();
    }

    private void OnValidate()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (canvasScaler == null) return;

        // CanvasScaler sendiri sudah menghitung ulang skala saat ukuran layar berubah,
        // jadi cukup diset sekali.
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = screenMatchMode;
        canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
