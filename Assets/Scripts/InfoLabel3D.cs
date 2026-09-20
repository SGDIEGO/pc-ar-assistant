using TMPro;
using UnityEngine;

public class InfoLabel3D : MonoBehaviour, IComponentInfoReceiver
{
    public TextMeshPro text;

    const string HeaderOpen = "<color=#FFD27F>";
    const string ColorClose = "</color>";
    const int MaxChars = 220;

    void Awake()
    {
        if (text == null) text = GetComponent<TextMeshPro>();
        ShowMainMenu();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }

    public void ShowComponent(ComponentInfo component)
    {
        if (text == null) return;
        if (component == null)
        {
            ShowMainMenu();
            return;
        }

        string body = Clamp(component.description, MaxChars);
        text.text = HeaderOpen + component.componentName + ColorClose + "\n" + body;
    }

    public void ShowMainMenu()
    {
        if (text == null) return;
        text.text = HeaderOpen + "Componentes de una PC" + ColorClose +
                    "\nToca sobre la placa madre para ver cada componente.\nUsa dos dedos para girar o hacer zoom.";
    }

    static string Clamp(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "...";
    }
}