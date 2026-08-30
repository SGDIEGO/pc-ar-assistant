using UnityEngine;
using UnityEngine.SceneManagement;

namespace PCAR.UI
{
    /// <summary>
    /// FASE 15: pantalla de inicio / menu principal.
    /// Muestra titulo y boton "Escanear" que carga el flujo AR (MainAR).
    /// Implementado con GUI inmediata (OnGUI) por consistencia con el resto.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        private const string GameSceneName = "MainAR";

        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;

        private void BuildStyles()
        {
            _titleStyle = new GUIStyle
            {
                fontSize = 52,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _subStyle = new GUIStyle
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.7f, 0.75f, 0.8f) }
            };
        }

        private void OnGUI()
        {
            if (_titleStyle == null)
            {
                BuildStyles();
            }

            float w = Screen.width * 0.8f;
            float x = (Screen.width - w) / 2f;

            GUILayout.BeginArea(new Rect(x, Screen.height * 0.2f, w, Screen.height * 0.6f));
            GUILayout.Label("PCAR ASSISTANT", _titleStyle);
            GUILayout.Space(12f);
            GUILayout.Label("Guia de componentes de PC en Realidad Aumentada", _subStyle);
            GUILayout.Space(48f);

            if (GUILayout.Button("Escanear", GUILayout.Height(88)))
            {
                StartGame();
            }

            GUILayout.Space(16f);
            if (GUILayout.Button("Salir", GUILayout.Height(64)))
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }

            GUILayout.EndArea();
        }

        private void StartGame()
        {
            SceneManager.LoadScene(GameSceneName);
        }
    }
}
