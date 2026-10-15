using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Tooltip("Receptor secundario (etiqueta 3D) que se actualiza junto al banner. Debe ser tipo concreto para serializar.")]
    public InfoLabel3D mirror;

    public ComponentInfo Current { get; private set; }
    public int CurrentTab { get; private set; }

    const int TAB_INFO = 0;
    const int TAB_ASSEMBLE = 1;
    const int TAB_DISASSEMBLE = 2;

    int _stepIndex;

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
        if (Current == null)
        {
            SetBody("", tab);
            return;
        }
        _stepIndex = 0;
        switch (tab)
        {
            case TAB_INFO:
                SetBody(
                    Current.description + "\n\n<color=#8FCBFF>FUNCIÓN</color>\n" + Current.function +
                    (string.IsNullOrEmpty(Current.specifications)
                        ? ""
                        : "\n\n<color=#8FCBFF>ESPECIFICACIONES</color>\n" + Current.specifications),
                    tab);
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
            content += "\n\n<size=70%><color=#AAAAAA>Pulsa Siguiente / Anterior para navegar.</color></size>";
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
        if (infoArea != null) infoArea.SetActive(tab == TAB_INFO);
        if (disassembleHintArea != null) disassembleHintArea.SetActive(tab == TAB_DISASSEMBLE);
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
        if (prevButton != null) prevButton.interactable = isSteps;
        if (nextButton != null) nextButton.interactable = isSteps;
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
}