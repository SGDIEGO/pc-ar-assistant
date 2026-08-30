using PCAR.Components;
using UnityEngine;

namespace PCAR.Recognition
{
    /// <summary>
    /// FASE 8: prueba del reconocimiento simulado.
    /// Muestra un boton "Simular reconocimiento RAM" en pantalla. Al pulsarlo
    /// invoca el IRecognitionService (mock), resuelve el id contra el
    /// ComponentRepository y muestra la info del componente detectado.
    /// </summary>
    public class MockRecognitionButton : MonoBehaviour
    {
        private IRecognitionService _service;
        private GUIStyle _style;
        private string _resultText = "Pulsa el boton para simular el reconocimiento.";

        private void Awake()
        {
            _service = new MockRecognitionService();
            _style = new GUIStyle
            {
                fontSize = 24,
                normal = { textColor = Color.white }
            };
            _style.normal.background = Texture2D.grayTexture;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, Screen.height - 120, 320, 96));
            if (GUILayout.Button("Simular reconocimiento RAM", GUILayout.Height(56)))
            {
                var result = _service.Recognize();
                if (result.success)
                {
                    var component = ComponentRepository.Get(result.componentId);
                    _resultText = component != null
                        ? $"[OK] Componente: {component.name} (confianza {result.confidence:P0})"
                        : $"[OK] Id '{result.componentId}' pero no hay datos cargados.";
                    Debug.Log($"[PCAR][Mock] Reconocido -> {result.componentId} (confianza {result.confidence})");
                }
                else
                {
                    _resultText = result.message;
                }
            }

            GUILayout.Box(_resultText, _style);
            GUILayout.EndArea();
        }
    }
}
