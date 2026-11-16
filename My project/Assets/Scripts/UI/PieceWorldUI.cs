using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Overhead UI di atas kepala bidak, disusun vertikal dan rata tengah:
//   1. Icon tipe bidak
//   2. Nama karakter (ChessPiece.pieceName, sama dengan yang tampil di Charge Log)
//   3. ReturnCountdownBar (3 strip)
//   4. Counter jarak ke petak benteng lawan, mis. "-12 tiles"
//      (merah = jauh, jingga = sedang, hijau = dekat)
public class PieceWorldUI : MonoBehaviour
{
    [Header("Piece Type Icon (urutan: Pawn, Rook, Knight, Bishop, Queen, King)")]
    [Tooltip("Tim merah (team 0) = icon White.")]
    [SerializeField] private Texture2D[] redTeamIcons = new Texture2D[6];
    [Tooltip("Tim biru (team 1) = icon Black.")]
    [SerializeField] private Texture2D[] blueTeamIcons = new Texture2D[6];
    [Tooltip("Tinggi icon dalam unit canvas (lebar mengikuti rasio gambar).")]
    [SerializeField] private float iconHeight = 62f;

    [Header("Posisi")]
    [Tooltip("Kenaikan posisi lokal popup di sumbu Y (unit dunia) relatif terhadap posisi di prefab bidak.")]
    [SerializeField] private float heightOffset = 1.62f;

    [Header("Layout Vertikal")]
    [Tooltip("Offset titik atas stack relatif terhadap posisi awal RankNumber di prefab.")]
    [SerializeField] private float stackTopOffset = 40f;
    [Tooltip("Jarak icon -> nama.")]
    [SerializeField] private float gapIconToName = 6f;
    [Tooltip("Jarak nama -> bar countdown.")]
    [SerializeField] private float gapNameToBar = 8f;
    [Tooltip("Jarak bar countdown -> counter jarak.")]
    [SerializeField] private float gapBarToDistance = 8f;

    [Header("Nama Karakter")]
    [SerializeField] private float nameFontSize = 17f;
    [SerializeField] private Color nameColor = Color.white;

    [Header("Return Countdown Bar")]
    [SerializeField] private Vector2 segmentSize = new Vector2(40f, 14f);
    [SerializeField] private float segmentSpacing = 8f;
    [SerializeField] private Color fullColor = new Color32(0xFF, 0xE2, 0x7A, 0xFF);   // 3 strip
    [SerializeField] private Color midColor = new Color32(0xFF, 0x9F, 0x43, 0xFF);    // 2 strip
    [SerializeField] private Color lowColor = new Color32(0xFF, 0x4D, 0x4D, 0xFF);    // 1 strip
    [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 0.45f);

    [Header("Counter Jarak ke Benteng Lawan")]
    [SerializeField] private float distanceFontSize = 20f;
    [Tooltip("Jauh = merah, tengah = jingga, dekat = hijau.")]
    [SerializeField] private Color farColor = new Color32(0xFF, 0x4D, 0x4D, 0xFF);
    [SerializeField] private Color middleColor = new Color32(0xFF, 0x9F, 0x43, 0xFF);
    [SerializeField] private Color nearColor = new Color32(0x5C, 0xE6, 0x5C, 0xFF);
    [Tooltip("True = hitung diagonal sebagai 1 langkah (Chebyshev). False = jumlah tile horizontal + vertikal (Manhattan).")]
    [SerializeField] private bool diagonalCountsAsOne = false;

    private ChessPiece piece;
    private Transform mainCameraTransform;

    private TextMeshProUGUI distanceText; // reuse RankNumber dari prefab
    private TextMeshProUGUI nameText;

    private GameObject barRoot;
    private RectTransform barRect;
    private Image[] segments;

    private RectTransform iconRect;
    private float iconWidth;

    private string lastName;
    private int lastDistance = int.MinValue;
    private bool lastDistanceVisible = true;

    // Titik acuan = posisi awal RankNumber di prefab. Semua elemen rata tengah di sumbu X ini.
    private Vector2 groupCenter;
    private int maxDistance = 1;

    void Start()
    {
        piece = GetComponentInParent<ChessPiece>();
        distanceText = GetComponentInChildren<TextMeshProUGUI>();
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;

        // Naikkan seluruh popup di sumbu Y lokal (bukan via prefab, supaya satu tempat untuk semua bidak).
        transform.localPosition += Vector3.up * heightOffset;

        if (distanceText == null) return;

        groupCenter = distanceText.rectTransform.anchoredPosition;

        SetupText(distanceText, distanceFontSize, distanceText.color);
        distanceText.name = "DistanceCounter";
        distanceText.text = "";

        int maxDx = Mathf.Max(ChessPiece.FortressColumnX, ChessBoardManager.TILE_WIDTH - 1 - ChessPiece.FortressColumnX);
        int maxDy = ChessBoardManager.TILE_HEIGHT - 1;
        maxDistance = diagonalCountsAsOne ? Mathf.Max(maxDx, maxDy) : maxDx + maxDy;

        BuildIcon();
        BuildName();
        BuildBar();
        LayoutStack();
    }

    // Auto Size membuat lebar/tinggi teks tidak bisa diukur dengan akurat.
    // Kunci ke ukuran tetap supaya layout stabil.
    private static void SetupText(TextMeshProUGUI tmp, float fontSize, Color color)
    {
        tmp.richText = true;
        tmp.enableAutoSizing = false;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.horizontalAlignment = HorizontalAlignmentOptions.Center;
        tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }

    private RectTransform CreateChildRect(string objName, params System.Type[] components)
    {
        RectTransform textRect = distanceText.rectTransform;
        GameObject go = new GameObject(objName, components);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.SetParent(textRect.parent, false);
        rt.anchorMin = textRect.anchorMin;
        rt.anchorMax = textRect.anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    private void BuildIcon()
    {
        if (piece == null) return;

        Texture2D[] set = piece.team == 0 ? redTeamIcons : blueTeamIcons;
        int index = (int)piece.type - 1;
        if (set == null || index < 0 || index >= set.Length || set[index] == null) return;

        Texture2D tex = set[index];
        iconRect = CreateChildRect("PieceTypeIcon", typeof(RectTransform), typeof(RawImage));
        iconWidth = iconHeight * tex.width / tex.height;
        iconRect.sizeDelta = new Vector2(iconWidth, iconHeight);

        RawImage img = iconRect.GetComponent<RawImage>();
        img.texture = tex;
        img.raycastTarget = false;
    }

    // Nama dibuat dengan menduplikasi RankNumber supaya font, material, dan outline sama persis.
    private void BuildName()
    {
        nameText = Instantiate(distanceText, distanceText.rectTransform.parent);
        nameText.name = "CharacterName";
        SetupText(nameText, nameFontSize, nameColor);
        nameText.text = "";
    }

    private void BuildBar()
    {
        barRect = CreateChildRect("ReturnCountdownBar", typeof(RectTransform));
        barRoot = barRect.gameObject;

        int count = ChessPiece.AutoReturnOwnerTurns;
        float totalWidth = count * segmentSize.x + (count - 1) * segmentSpacing;
        barRect.sizeDelta = new Vector2(totalWidth, segmentSize.y);

        segments = new Image[count];
        for (int i = 0; i < count; i++)
        {
            GameObject seg = new GameObject("Segment" + (i + 1), typeof(RectTransform), typeof(Image));
            RectTransform segRect = seg.GetComponent<RectTransform>();
            segRect.SetParent(barRect, false);
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

    private static float MeasureHeight(TextMeshProUGUI tmp)
    {
        if (tmp == null) return 0f;
        tmp.ForceMeshUpdate();
        return tmp.fontSize * tmp.font.faceInfo.lineHeight / tmp.font.faceInfo.pointSize;
    }

    // Susun semua elemen dari atas ke bawah, rata tengah di groupCenter.x.
    // Slot bar selalu dicadangkan walau bar sedang disembunyikan, supaya elemen lain tidak melompat.
    private void LayoutStack()
    {
        float y = groupCenter.y + stackTopOffset;
        float cx = groupCenter.x;

        if (iconRect != null)
        {
            iconRect.anchoredPosition = new Vector2(cx, y - iconHeight * 0.5f);
            y -= iconHeight + gapIconToName;
        }

        if (nameText != null)
        {
            float h = MeasureHeight(nameText);
            nameText.rectTransform.sizeDelta = new Vector2(nameText.rectTransform.sizeDelta.x, h);
            nameText.rectTransform.anchoredPosition = new Vector2(cx, y - h * 0.5f);
            y -= h + gapNameToBar;
        }

        if (barRect != null)
        {
            barRect.anchoredPosition = new Vector2(cx, y - segmentSize.y * 0.5f);
            y -= segmentSize.y + gapBarToDistance;
        }

        if (distanceText != null)
        {
            float h = MeasureHeight(distanceText);
            distanceText.rectTransform.sizeDelta = new Vector2(distanceText.rectTransform.sizeDelta.x, h);
            distanceText.rectTransform.anchoredPosition = new Vector2(cx, y - h * 0.5f);
        }
    }

    void LateUpdate()
    {
        if (piece == null || distanceText == null) return;

        UpdateName();
        UpdateDistance();
        UpdateBar(piece.GetHomeReturnCountdownDisplay());

        // Billboard: Membuat UI selalu menghadap kamera
        if (mainCameraTransform != null)
        {
            transform.LookAt(transform.position + mainCameraTransform.rotation * Vector3.forward,
                             mainCameraTransform.rotation * Vector3.up);
        }
    }

    private void UpdateName()
    {
        if (nameText == null) return;
        string n = piece.pieceName ?? "";
        if (n == lastName) return;
        nameText.text = n;
        lastName = n;
    }

    private int ComputeDistanceToEnemyFortress()
    {
        int dx = Mathf.Abs(piece.currentX - ChessPiece.FortressColumnX);
        int dy = Mathf.Abs(piece.currentY - ChessPiece.GetEnemyFortressY(piece.team));
        return diagonalCountsAsOne ? Mathf.Max(dx, dy) : dx + dy;
    }

    private void UpdateDistance()
    {
        bool visible = !piece.isJailed && !piece.isGhost;
        if (visible != lastDistanceVisible)
        {
            distanceText.gameObject.SetActive(visible);
            lastDistanceVisible = visible;
        }
        if (!visible) return;

        int dist = ComputeDistanceToEnemyFortress();
        if (dist == lastDistance) return;
        lastDistance = dist;

        distanceText.text = dist > 0 ? $"-{dist} tiles" : "0 tiles";

        // t = 0 -> jauh (merah), 0.5 -> jingga, 1 -> dekat (hijau)
        float t = 1f - Mathf.Clamp01((float)dist / maxDistance);
        distanceText.color = t < 0.5f
            ? Color.Lerp(farColor, middleColor, t * 2f)
            : Color.Lerp(middleColor, nearColor, (t - 0.5f) * 2f);
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
