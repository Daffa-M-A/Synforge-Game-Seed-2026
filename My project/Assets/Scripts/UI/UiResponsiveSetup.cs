using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public class UiResponsiveSetup : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

    private CanvasScaler canvasScaler;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        canvasScaler = GetComponent<CanvasScaler>();
        ApplySettings();
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplySettings();
    }

    private void Update()
    {
        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        if (current == lastScreenSize)
        {
            return;
        }

        lastScreenSize = current;
        ApplySettings();
        Canvas.ForceUpdateCanvases();
    }

    private void ApplySettings()
    {
        if (canvasScaler == null)
        {
            return;
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = referenceResolution;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
        canvasScaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
