using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace PCAR.AR
{
    /// <summary>
    /// Informacion minima de estado AR para validar la escena en dispositivo.
    /// Muestra en pantalla si ARCore cargo y el estado del tracking.
    /// </summary>
    public class ARSessionStatus : MonoBehaviour
    {
        private ARSession _session;
        private GUIStyle _style;
        private string _message = "Inicializando AR...";

        private void Awake()
        {
            _session = GetComponentInParent<ARSession>();
        }

        private void Start()
        {
            _style = new GUIStyle
            {
                fontSize = 22,
                normal = { textColor = Color.green }
            };
            _style.normal.background = Texture2D.grayTexture;
            _message = _session != null
                ? $"[PCAR] AR Session presente (enabled={_session.enabled})."
                : "[PCAR] Warning: no hay ARSession en la escena.";
            Debug.Log(_message);
        }

        private void Update()
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            bool sessionReady = _session != null && _session.subsystem != null && _session.subsystem.running;

            if (loader == null)
            {
                _message = "AR detenido: el loader XR no cargo (verificar ARCore / dispositivo compatible).";
#if UNITY_STANDALONE
                _message = "AR (editor): usando simulacion del plano.";
#endif
            }
            else if (!sessionReady)
            {
                _message = "ARCore activo pero el tracking aun no inicia...";
            }
            else
            {
                _message = "AR OK - Tracking en curso. (ARCore activo)";
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, Screen.width - 32, 160));
            GUILayout.Box(_message, _style);
            GUILayout.EndArea();
        }
    }
}