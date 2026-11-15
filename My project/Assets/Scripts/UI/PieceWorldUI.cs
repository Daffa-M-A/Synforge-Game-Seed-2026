using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PieceWorldUI : MonoBehaviour
{
    [Header("Piece Type Icon (urutan: Pawn, Rook, Knight, Bishop, Queen, King)")]
    [Tooltip("Tim merah (team 0) = icon White.")]
    [SerializeField] private Texture2D[] redTeamIcons = new Texture2D[6];
    [Tooltip("Tim biru (team 1) = icon Black.")]
    [SerializeField] private Texture2D[] blueTeamIcons = new Texture2D[6];
    [Tooltip("Tinggi icon dalam unit canvas (lebar mengikuti rasio gambar).")]
    [SerializeField] private float iconHeight = 62f;
    [Tooltip("Jarak antara icon dan angka.")]
    [SerializeField] private float iconGap = 10f;

    [Header("Return Countdown Bar")]
    [Tooltip("Posisi bar relatif terhadap teks rank (dalam unit canvas).")]
    [SerializeField] private Vector2 barOffset = new Vector2(0f, -45f);
    [SerializeField] private Vector2 segmentSize = new Vector2(40f, 14f);
    [SerializeField] private float segmentSpacing = 8f;
    [SerializeField] private Color fullColor = new Color32(0xFF, 0xE2, 0x7A, 0xFF);   // 3 strip
    [SerializeField] private Color midColor = new Color32(0xFF, 0x9F, 0x43, 0xFF);    // 2 strip
    [SerializeField] private Color lowColor = new Color32(0xFF, 0x4D, 0x4D, 0xFF);    // 1 strip
    [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 0.45f);

    private ChessPiece piece;
    private TextMeshProUGUI rankText;
    private Transform mainCameraTransform;

    private GameObject barRoot;
    private Image[] segments;

    private RawImage iconImage;
    private RectTransform iconRect;
    private string lastText;
    private float iconWidth;

    // Titik tengah grup (icon + angka) = posisi awal RankNumber di prefab,
    // sama dengan titik tengah bar kuning (barOffset.x = 0).
    private Vector2 groupCenter;

    void Start()
    {
        piece = GetComponentInParent<ChessPiece>();
        rankText = GetComponentInChildren<TextMeshProUGUI>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        if (rankText != null)
        {
            rankText.richText = true;
            groupCenter = rankText.rectTransform.anchoredPosition;

            // Auto Size membuat lebar teks tidak bisa diukur dengan akurat
            // (pengukuran memakai Font Size Max). Kunci ke ukuran yang memang tampil.
            if (rankText.enableAutoSizing)
            {
                rankText.fontSize = rankText.fontSizeMin;
                rankText.enableAutoSizing = false;
            }
            rankText.horizontalAlignment = HorizontalAlignmentOptions.Center;
        }

        BuildIcon();
        BuildBar();
    }

    private void BuildIcon()
    {
        if (rankText == null || piece == null) return;

        Texture2D[] set = piece.team == 0 ? redTeamIcons : blueTeamIcons;
        int index = (int)piece.type - 1;
        if (set == null || index < 0 || index >= set.Length || set[index] == null) return;

        Texture2D tex = set[index];
        RectTransform textRect = rankText.rectTransform;

        GameObject go = new GameObject("PieceTypeIcon", typeof(RectTransform), typeof(RawImage));
        iconRect = go.GetComponent<RectTransform>();
        iconRect.SetParent(textRect.parent, false);
        iconRect.anchorMin = textRect.anchorMin;
        iconRect.anchorMax = textRect.anchorMax;
        iconRect.pivot = new Vector2(0.5f, 0.5f);

        iconWidth = iconHeight * tex.width / tex.height;
        iconRect.sizeDelta = new Vector2(iconWidth, iconHeight);

        iconImage = go.GetComponent<RawImage>();
        iconImage.texture = tex;
        iconImage.raycastTarget = false;
    }

    // Icon + jarak + angka diperlakukan sebagai satu grup yang rata tengah
    // di groupCenter (tepat di atas bar). Lebar angka diukur dari glyph yang
    // benar-benar dirender, bukan dari kotak teks 200px.
    // Jika bidak belum punya nomor urut, icon sendirian di tengah.
    private void LayoutGroup()
    {
        RectTransform textRect = rankText.rectTransform;

        float textWidth = 0f;
        Vector3 boundsCenter = Vector3.zero;
        if (!string.IsNullOrEmpty(rankText.text))
        {
            rankText.ForceMeshUpdate();
            Bounds b = rankText.textBounds;
            textWidth = b.size.x;
            boundsCenter = b.center; // offset glyph dari pivot RectTransform teks
        }

        if (iconRect == null)
        {
            // Tanpa icon: angka saja yang diletakkan di tengah.
            textRect.anchoredPosition = new Vector2(groupCenter.x - boundsCenter.x, groupCenter.y);
            return;
        }

        float gap = textWidth > 0f ? iconGap : 0f;
        float groupWidth = iconWidth + gap + textWidth;
        float left = groupCenter.x - groupWidth * 0.5f;

        // Sejajarkan tinggi icon dengan titik tengah visual angka.
        float centerY = groupCenter.y + boundsCenter.y;

        iconRect.anchoredPosition = new Vector2(left + iconWidth * 0.5f, centerY);

        float textCenterX = left + iconWidth + gap + textWidth * 0.5f;
        textRect.anchoredPosition = new Vector2(textCenterX - boundsCenter.x, groupCenter.y);
    }

    private void BuildBar()
    {
        if (rankText == null) return;

        RectTransform textRect = rankText.rectTransform;

        barRoot = new GameObject("ReturnCountdownBar", typeof(RectTransform));
        RectTransform rootRect = barRoot.GetComponent<RectTransform>();
        rootRect.SetParent(textRect.parent, false);
        rootRect.anchorMin = textRect.anchorMin;
        rootRect.anchorMax = textRect.anchorMax;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = groupCenter + barOffset;

        int count = ChessPiece.AutoReturnOwnerTurns;
        float totalWidth = count * segmentSize.x + (count - 1) * segmentSpacing;
        rootRect.sizeDelta = new Vector2(totalWidth, segmentSize.y);

        segments = new Image[count];
        for (int i = 0; i < count; i++)
        {
            GameObject seg = new GameObject("Segment" + (i + 1), typeof(RectTransform), typeof(Image));
            RectTransform segRect = seg.GetComponent<RectTransform>();
            segRect.SetParent(rootRect, false);
            segRect.anchorMin = segRect.anchorMax = new Vector2(0f, 0.5f);
            segRect.pivot = new Vector2(0f, 0.5f);
            segRect.sizeDelta = segmentSize;
            segRect.anchoredPosition = new Vector2(i * (segmentSize.x + segmentSpacing), 0f);

            Image img = seg.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = emptyColor;
            segments[i] = img;
        }

        barRoot.SetActive(false);
    }

    void LateUpdate()
    {
        if (piece == null || rankText == null) return;

        string text = piece.orderCharge > 0 ? piece.orderCharge.ToString() : "";
        if (text != lastText)
        {
            rankText.text = text;
            LayoutGroup();
            lastText = text;
        }
        UpdateBar(piece.GetHomeReturnCountdownDisplay());

        // Billboard: Membuat UI selalu menghadap kamera
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }

    private void UpdateBar(int remaining)
    {
        if (barRoot == null) return;

        bool show = remaining > 0;
        if (barRoot.activeSelf != show) barRoot.SetActive(show);
        if (!show) return;

        Color filled = remaining >= 3 ? fullColor : remaining == 2 ? midColor : lowColor;
        for (int i = 0; i < segments.Length; i++)
            segments[i].color = i < remaining ? filled : emptyColor;
    }
}
