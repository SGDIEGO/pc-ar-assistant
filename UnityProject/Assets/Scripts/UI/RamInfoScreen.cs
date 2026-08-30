using System;
using PCAR.Components;
using UnityEngine;

namespace PCAR.UI
{
    /// <summary>
    /// FASE 9: pantalla de informacion de un componente.
    /// Muestra nombre, funcion, especificaciones (tipo/capacidad/velocidad) y
    /// advertencias, con botones "Ensamblar" / "Desensamblar" fijos abajo.
    /// Implementada con GUI inmediata (OnGUI) para probarla sin wiring de
    /// prefabs; se puede sustituir por canvas uGUI en Fase 12.
    /// </summary>
    public class RamInfoScreen : MonoBehaviour
    {
        public ComponentInfo component;

        /// <summary>Se dispara al pulsar "Ensamblar" (lo engancha la Fase 10).</summary>
        public event Action OnAssembleRequested;

        /// <summary>Se dispara al pulsar "Desensamblar" (lo engancha la Fase 11).</summary>
        public event Action OnDisassembleRequested;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _specStyle;
        private GUIStyle _warningStyle;
        private Vector2 _scroll;

        private void Start()
        {
            if (component == null)
            {
                component = ComponentRepository.GetRam();
            }

            BuildStyles();

            if (component == null)
            {
                Debug.LogWarning("[PCAR][UI] No hay datos del componente. Revisa Resources/Data/Components.");
            }
        }

        private void BuildStyles()
        {
            _titleStyle = new GUIStyle
            {
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _labelStyle = new GUIStyle
            {
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _sectionStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.85f, 1f) }
            };

            _specStyle = new GUIStyle
            {
                fontSize = 18,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.94f, 1f) }
            };

            _warningStyle = new GUIStyle
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.yellow }
            };
        }

        private void OnGUI()
        {
            if (component == null)
            {
                GUILayout.Label("[PCAR] Componente no disponible.", _labelStyle);
                return;
            }

            float w = Screen.width * 0.9f;
            float x = (Screen.width - w) / 2f;
            const float top = 20f;
            const float bottomMargin = 130f;

            GUILayout.BeginArea(new Rect(x, top, w, Screen.height - top - bottomMargin));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.BeginVertical();

            GUILayout.Label(component.name, _titleStyle);
            GUILayout.Space(10f);
            GUILayout.Label("Funcion: " + component.function, _labelStyle);

            GUILayout.Space(18f);
            GUILayout.Label("CARACTERISTICAS", _sectionStyle);
            foreach (var spec in component.specifications)
            {
                GUILayout.Label("  - " + spec, _specStyle);
            }

            GUILayout.Space(18f);
            GUILayout.Label("ADVERTENCIAS", _sectionStyle);
            foreach (var warning in component.warnings)
            {
                GUILayout.Label("  ! " + warning, _warningStyle);
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            // Botones fijos en la parte inferior
            GUILayout.BeginArea(new Rect(x, Screen.height - 118f, w, 104f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ensamblar", GUILayout.Height(72), GUILayout.ExpandWidth(true)))
            {
                OnAssembleRequested?.Invoke();
            }
            if (GUILayout.Button("Desensamblar", GUILayout.Height(72), GUILayout.ExpandWidth(true)))
            {
                OnDisassembleRequested?.Invoke();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
