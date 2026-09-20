using UnityEngine;

public class ComponentInfo : MonoBehaviour
{
    [Header("Identificación")]
    public string componentName;
    public string buttonLabel;

    [Header("Información general")]
    [TextArea(3, 8)] public string description;
    [TextArea(3, 8)] public string function;
    [TextArea(3, 8)] public string specifications;

    [Header("Instrucciones")]
    [TextArea(2, 6)] public string[] assembleSteps;
    [TextArea(2, 6)] public string[] disassembleSteps;
}