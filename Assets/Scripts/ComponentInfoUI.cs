using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ComponentInfoUI : MonoBehaviour, IComponentInfoReceiver
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI stepText;
    public TextMeshProUGUI stepCounterText;
    public Button infoTabButton;
    public Button assembleTabButton;
    public Button disassembleTabButton;
    public Button nextButton;
    public Button prevButton;
    public GameObject stepsArea;
    public GameObject infoArea;
    public GameObject disassembleHintArea;
    public RectTransform viewport; // Para detección de swipe en carrusel
    public ScrollRect bodyScrollRect;

    [Tooltip("Receptor secundario (etiqueta 3D) que se actualiza junto al banner. Debe ser tipo concreto para serializar.")]
    public InfoLabel3D mirror;

    public ComponentInfo Current { get; private set; }
    public int CurrentTab { get; private set; }

    const int TAB_INFO = 0;
    const int TAB_ASSEMBLE = 1;
    const int TAB_DISASSEMBLE = 2;
    static ScreenOrientation _lastKnownGestureOrientation = ScreenOrientation.Portrait;

    int _stepIndex;
    Vector2 _dragStartPos;
    bool _draggingSteps;
    ScreenOrientation _dragOrientation;
    ScreenOrientation _lastHintOrientation = ScreenOrientation.Unknown;
    const float SwipeThreshold = 40f; // píxeles

    public void ShowComponent(ComponentInfo component)
    {
        Current = component;
        _stepIndex = 0;
        gameObject.SetActive(true);
        if (mirror != null) mirror.ShowComponent(component);
        if (titleText != null)
            titleText.text = component != null ? component.componentName : "";
        if (component == null)
        {
            SetBody("", 0);
            return;
        }
        SelectTab(CurrentTab);
    }

    public void SelectTab(int tab)
    {
        CurrentTab = tab;
        UpdateTabVisual();
        if (bodyScrollRect != null)
        {
            // El scroll del texto solo debe consumir gestos verticales en Descripción.
            // En Ensamblaje/Desensamblaje, el gesto vertical cambia de paso.
            bodyScrollRect.horizontal = false;
            bodyScrollRect.vertical = tab == TAB_INFO;
            bodyScrollRect.StopMovement();
        }
        if (Current == null)
        {
            SetBody("", tab);
            return;
        }
        _stepIndex = 0;
        switch (tab)
        {
            case TAB_INFO:
                SetBody(Current.description, tab);
                break;
            case TAB_ASSEMBLE:
                ShowStep(Current.assembleSteps, tab);
                break;
            case TAB_DISASSEMBLE:
                ShowStep(Current.disassembleSteps, tab);
                break;
        }
    }

    public void NextStep()
    {
        if (Current == null) return;
        string[] steps = StepsForTab(CurrentTab);
        if (steps == null || steps.Length == 0) return;
        _stepIndex = Mathf.Min(_stepIndex + 1, steps.Length - 1);
        ShowStep(steps, CurrentTab);
    }

    public void PrevStep()
    {
        if (Current == null) return;
        _stepIndex = Mathf.Max(_stepIndex - 1, 0);
        ShowStep(StepsForTab(CurrentTab), CurrentTab);
    }

    public void ShowMainMenu()
    {
        Current = null;
        gameObject.SetActive(false);
        if (mirror != null) mirror.ShowMainMenu();
        if (titleText != null) titleText.text = "Componentes de la PC";
        SelectTab(TAB_INFO);
    }

    void ShowStep(string[] steps, int tab)
    {
        if (steps == null || steps.Length == 0)
        {
            SetBody("No hay pasos definidos para esta sección.", tab);
            return;
        }
        _stepIndex = Mathf.Clamp(_stepIndex, 0, steps.Length - 1);
        string content = "<color=#FFD27F>PASO " + (_stepIndex + 1) + " de " + steps.Length + "</color>\n\n" + steps[_stepIndex];
        if (steps.Length > 1)
        {
            Vector2 nextDirection = PortraitDeltaToScreen(Vector2.up, GestureOrientation);
            Vector2 previousDirection = -nextDirection;
            content += "\n\n<size=70%><color=#AAAAAA>"
                + DirectionArrow(nextDirection) + " Siguiente    "
                + DirectionArrow(previousDirection) + " Anterior</color></size>";
        }
        SetBody(content, tab);
        if (stepCounterText != null)
            stepCounterText.text = (_stepIndex + 1) + " / " + steps.Length;
        bool hasNav = steps.Length > 1;
        if (prevButton != null) prevButton.interactable = hasNav && _stepIndex > 0;
        if (nextButton != null) nextButton.interactable = hasNav && _stepIndex < steps.Length - 1;
    }

    string[] StepsForTab(int tab)
    {
        if (Current == null) return null;
        return tab == TAB_ASSEMBLE ? Current.assembleSteps : Current.disassembleSteps;
    }

    void SetBody(string text, int tab)
    {
        if (bodyText != null) bodyText.text = text;
        bool isSteps = tab == TAB_ASSEMBLE || tab == TAB_DISASSEMBLE;
        if (stepsArea != null) stepsArea.SetActive(isSteps);
        // infoArea (el scroll con el texto) se mantiene SIEMPRE visible.
        // Solo se oculta/muestra los botones de navegación.
        if (stepCounterText != null)
        {
            if (Current != null && isSteps)
            {
                string[] s = StepsForTab(tab);
                stepCounterText.text = (s != null && s.Length > 0) ? "1 / " + s.Length : "";
            }
            else
            {
                stepCounterText.text = "";
            }
        }
        // NO tocar interactable aquí: ShowStep ya lo configura según _stepIndex
    }

    void UpdateTabVisual()
    {
        Color hl = new Color(0.3f, 0.75f, 1f);
        Color dim = new Color(0.16f, 0.18f, 0.22f);
        SetTabColor(infoTabButton, CurrentTab == TAB_INFO ? hl : dim);
        SetTabColor(assembleTabButton, CurrentTab == TAB_ASSEMBLE ? hl : dim);
        SetTabColor(disassembleTabButton, CurrentTab == TAB_DISASSEMBLE ? hl : dim);
    }

    static void SetTabColor(Button button, Color color)
    {
        if (button == null) return;
        Image img = button.targetGraphic as Image;
        if (img != null) img.color = color;
    }

    void Update()
    {
        ScreenOrientation orientation = GestureOrientation;
        if (orientation != _lastHintOrientation)
        {
            _lastHintOrientation = orientation;
            _draggingSteps = false;
            if (Current != null && IsInStepsTab())
                ShowStep(StepsForTab(CurrentTab), CurrentTab);
        }

        if (!IsInStepsTab() || Current == null) return;
        HandleStepSwipe();
    }

    bool IsInStepsTab()
    {
        return CurrentTab == TAB_ASSEMBLE || CurrentTab == TAB_DISASSEMBLE;
    }

    void HandleStepSwipe()
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null) return;
        var primary = touch.primaryTouch;

        if (primary.press.wasPressedThisFrame)
        {
            _dragStartPos = primary.position.ReadValue();
            _draggingSteps = IsOverViewport(_dragStartPos);
            _dragOrientation = GestureOrientation;
        }
        else if (_draggingSteps && primary.press.isPressed)
        {
            Vector2 screenDelta = primary.position.ReadValue() - _dragStartPos;
            Vector2 delta = ScreenDeltaToPortrait(screenDelta, _dragOrientation);
            if (Mathf.Abs(delta.y) > SwipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.5f)
            {
                if (delta.y > 0) NextStep();
                else PrevStep();
                _draggingSteps = false;
            }
        }
        else if (primary.press.wasReleasedThisFrame)
        {
            _draggingSteps = false;
        }
    }

    bool IsOverViewport(Vector2 screenPos)
    {
        if (viewport == null) return false;
        // Para world-space canvas, necesitamos pasar la cámara del canvas
        Camera canvasCam = viewport.GetComponentInParent<Canvas>().worldCamera;
        return RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPos, canvasCam);
    }

    public static ScreenOrientation GestureOrientation
    {
        get
        {
            ScreenOrientation screenOrientation = Screen.orientation;
            if (IsConcreteOrientation(screenOrientation))
            {
                _lastKnownGestureOrientation = screenOrientation;
                return screenOrientation;
            }

            switch (Input.deviceOrientation)
            {
                case DeviceOrientation.Portrait: return RememberOrientation(ScreenOrientation.Portrait);
                case DeviceOrientation.PortraitUpsideDown: return RememberOrientation(ScreenOrientation.PortraitUpsideDown);
                case DeviceOrientation.LandscapeLeft: return RememberOrientation(ScreenOrientation.LandscapeLeft);
                case DeviceOrientation.LandscapeRight: return RememberOrientation(ScreenOrientation.LandscapeRight);
            }

            if (Screen.width <= Screen.height)
                return RememberOrientation(ScreenOrientation.Portrait);

            if (_lastKnownGestureOrientation == ScreenOrientation.LandscapeLeft
                || _lastKnownGestureOrientation == ScreenOrientation.LandscapeRight)
                return _lastKnownGestureOrientation;

            return RememberOrientation(ScreenOrientation.LandscapeLeft);
        }
    }

    static ScreenOrientation RememberOrientation(ScreenOrientation orientation)
    {
        _lastKnownGestureOrientation = orientation;
        return orientation;
    }

    static bool IsConcreteOrientation(ScreenOrientation orientation)
    {
        return orientation == ScreenOrientation.Portrait
            || orientation == ScreenOrientation.PortraitUpsideDown
            || orientation == ScreenOrientation.LandscapeLeft
            || orientation == ScreenOrientation.LandscapeRight;
    }

    public static Vector2 ScreenDeltaToPortrait(Vector2 screenDelta, ScreenOrientation orientation)
    {
        switch (orientation)
        {
            case ScreenOrientation.PortraitUpsideDown:
                return -screenDelta;
            case ScreenOrientation.LandscapeLeft:
                return new Vector2(screenDelta.y, -screenDelta.x);
            case ScreenOrientation.LandscapeRight:
                return new Vector2(-screenDelta.y, screenDelta.x);
            default:
                return screenDelta;
        }
    }

    static Vector2 PortraitDeltaToScreen(Vector2 portraitDelta, ScreenOrientation orientation)
    {
        switch (orientation)
        {
            case ScreenOrientation.PortraitUpsideDown:
                return -portraitDelta;
            case ScreenOrientation.LandscapeLeft:
                return new Vector2(-portraitDelta.y, portraitDelta.x);
            case ScreenOrientation.LandscapeRight:
                return new Vector2(portraitDelta.y, -portraitDelta.x);
            default:
                return portraitDelta;
        }
    }

    static string DirectionArrow(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0f ? "→" : "←";
        return direction.y > 0f ? "↑" : "↓";
    }
}
