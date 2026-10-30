using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
using Image = UnityEngine.UI.Image;

/// <summary>
/// Pantalla inicial y guía visual de escaneo para el ImageTarget.
/// La guía desaparece cuando Vuforia hace visible la aumentación.
/// </summary>
public sealed class ARWelcomeScannerUI : MonoBehaviour
{
    public ImageTargetBehaviour target;

    GameObject welcomeScreen;
    GameObject scannerScreen;
    bool hasStarted;

    static readonly Color Ink = new Color(0.035f, 0.07f, 0.12f, 0.97f);
    static readonly Color Accent = new Color(0.10f, 0.82f, 0.78f, 1f);
    static readonly Color TextPrimary = new Color(0.94f, 0.97f, 1f, 1f);
    static readonly Color TextSecondary = new Color(0.70f, 0.79f, 0.86f, 1f);

    void Start()
    {
        BuildInterface();
        if (target != null)
        {
            target.OnTargetStatusChanged += OnTargetStatusChanged;
            UpdateTrackingState(target.TargetStatus);
        }
    }

    void OnDestroy()
    {
        if (target != null)
            target.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    void BuildInterface()
    {
        GameObject canvasObject = new GameObject("WelcomeScannerCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        welcomeScreen = MakeFullScreenPanel(canvasObject.transform, "WelcomeScreen", Ink);
        MakeText(welcomeScreen.transform, "Eyebrow", "REALIDAD AUMENTADA", 30, Accent,
            new Vector2(0, 0.75f), new Vector2(1, 0.75f), new Vector2(36, -34), new Vector2(-36, 34), FontStyles.Bold);
        MakeText(welcomeScreen.transform, "WelcomeTitle", "Explora una\ncomputadora", 74, TextPrimary,
            new Vector2(0.06f, 0.52f), new Vector2(0.94f, 0.72f), Vector2.zero, Vector2.zero, FontStyles.Bold);
        MakeText(welcomeScreen.transform, "WelcomeBody",
            "Reconoce los componentes de la placa y aprende cómo se ensamblan, paso a paso.", 36, TextSecondary,
            new Vector2(0.1f, 0.39f), new Vector2(0.9f, 0.53f), Vector2.zero, Vector2.zero, FontStyles.Normal);
        MakeText(welcomeScreen.transform, "WelcomeHint", "Ten a mano la imagen de la placa madre.", 30, TextSecondary,
            new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.34f), Vector2.zero, Vector2.zero, FontStyles.Normal);
        Button startButton = MakeButton(welcomeScreen.transform, "StartButton", "Comenzar", Accent,
            new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.205f));
        startButton.onClick.AddListener(BeginScanning);

        scannerScreen = MakeFullScreenPanel(canvasObject.transform, "ScannerScreen", new Color(0.01f, 0.04f, 0.08f, 0.38f));
        MakeText(scannerScreen.transform, "ScannerTitle", "BUSCA LA PLACA MADRE", 34, TextPrimary,
            new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.78f), Vector2.zero, Vector2.zero, FontStyles.Bold);
        MakeText(scannerScreen.transform, "ScannerInstructions",
            "Centra la imagen dentro del marco y mantén el teléfono quieto.", 30, TextPrimary,
            new Vector2(0.12f, 0.19f), new Vector2(0.88f, 0.27f), Vector2.zero, Vector2.zero, FontStyles.Normal);
        BuildReticle(scannerScreen.transform);
        scannerScreen.SetActive(false);
    }

    void BeginScanning()
    {
        hasStarted = true;
        welcomeScreen.SetActive(false);
        scannerScreen.SetActive(target == null || !IsRendered(target.TargetStatus));
    }

    void OnTargetStatusChanged(ObserverBehaviour _, TargetStatus status) => UpdateTrackingState(status);

    void UpdateTrackingState(TargetStatus status)
    {
        if (hasStarted && scannerScreen != null)
            scannerScreen.SetActive(!IsRendered(status));
    }

    static bool IsRendered(TargetStatus status)
    {
        // Coincide con el filtro Tracked_ExtendedTracked del DefaultObserverEventHandler.
        return status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
    }

    GameObject MakeFullScreenPanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return panel;
    }

    static TextMeshProUGUI MakeText(Transform parent, string name, string value, float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, FontStyles style)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    static Button MakeButton(Transform parent, string name, string label, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = go.AddComponent<Image>();
        image.color = color;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        MakeText(go.transform, "Label", label, 40, Ink, Vector2.zero, Vector2.one,
            new Vector2(12, 8), new Vector2(-12, -8), FontStyles.Bold);
        return button;
    }

    void BuildReticle(Transform parent)
    {
        GameObject reticle = new GameObject("ScannerReticle");
        reticle.transform.SetParent(parent, false);
        RectTransform rect = reticle.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(590, 420);
        AddCorner(reticle.transform, "TopLeft", new Vector2(0, 1), new Vector2(1, 0));
        AddCorner(reticle.transform, "TopRight", new Vector2(1, 1), new Vector2(-1, 0));
        AddCorner(reticle.transform, "BottomLeft", new Vector2(0, 0), new Vector2(1, 1));
        AddCorner(reticle.transform, "BottomRight", new Vector2(1, 0), new Vector2(-1, 1));
        AddLine(reticle.transform, "CenterDot", new Vector2(0.5f, 0.5f), new Vector2(12, 12), Accent);
    }

    static void AddCorner(Transform parent, string name, Vector2 anchor, Vector2 direction)
    {
        GameObject corner = new GameObject(name);
        corner.transform.SetParent(parent, false);
        RectTransform rect = corner.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(direction.x > 0 ? 0 : 1, direction.y > 0 ? 0 : 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(72, 72);
        // Una esquina se forma con dos trazos finos en ángulo recto.
        GameObject horizontal = new GameObject("Horizontal");
        horizontal.transform.SetParent(corner.transform, false);
        RectTransform h = horizontal.AddComponent<RectTransform>();
        h.anchorMin = h.anchorMax = new Vector2(direction.x > 0 ? 0 : 1, direction.y > 0 ? 0 : 1);
        h.pivot = new Vector2(direction.x > 0 ? 0 : 1, direction.y > 0 ? 0 : 1);
        h.sizeDelta = new Vector2(72, 7);
        Image hi = horizontal.AddComponent<Image>(); hi.color = Accent; hi.raycastTarget = false;
        GameObject vertical = new GameObject("Vertical");
        vertical.transform.SetParent(corner.transform, false);
        RectTransform v = vertical.AddComponent<RectTransform>();
        v.anchorMin = v.anchorMax = new Vector2(direction.x > 0 ? 0 : 1, direction.y > 0 ? 0 : 1);
        v.pivot = new Vector2(direction.x > 0 ? 0 : 1, direction.y > 0 ? 0 : 1);
        v.sizeDelta = new Vector2(7, 72);
        Image vi = vertical.AddComponent<Image>(); vi.color = Accent; vi.raycastTarget = false;
    }

    static void AddLine(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        Image image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }
}
