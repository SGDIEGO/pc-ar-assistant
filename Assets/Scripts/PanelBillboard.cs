using UnityEngine;

// El panel 3D mira a la camara cada frame (mismo patron que la etiqueta anterior).
public class PanelBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
    }
}
