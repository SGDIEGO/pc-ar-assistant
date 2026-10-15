using UnityEngine;

// Vitrina flotante: al seleccionar CPU/RAM/SSD muestra su modelo 3D ampliado
// en un punto fijo sobre la placa. Solo una pieza visible a la vez y SIN
// colliders, para no interceptar los taps que van a la placa.
public class ComponentPreview3D : MonoBehaviour
{
    public GameObject cpuModel;
    public GameObject ramModel;
    public GameObject ssdModel;

    [Tooltip("Eje de giro (normal de la placa) en espacio del ancla.")]
    public Vector3 spinAxis = Vector3.forward;

    [Tooltip("Radio maximo de traslacion desde el punto base (unidades del ancla).")]
    public float maxRadius = 0.01f;

    [Tooltip("Desplazamiento axial minimo permitido (negativo = hacia la placa).")]
    public float minAxial = 0f;

    [Tooltip("Grados de giro por pixel arrastrado.")]
    public float rotateDegreesPerPixel = 0.4f;

    GameObject _current;

    public void Show(string buttonLabel)
    {
        HideAll();
        if (buttonLabel == "CPU") _current = cpuModel;
        else if (buttonLabel == "RAM") _current = ramModel;
        else if (buttonLabel == "SSD") _current = ssdModel;
        if (_current != null) _current.SetActive(true);
    }

    public void HideAll()
    {
        if (cpuModel != null) cpuModel.SetActive(false);
        if (ramModel != null) ramModel.SetActive(false);
        if (ssdModel != null) ssdModel.SetActive(false);
        _current = null;
    }
}
