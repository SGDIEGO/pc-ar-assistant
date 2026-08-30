using System;
using System.Collections.Generic;
using PCAR.Components;
using UnityEngine;

namespace PCAR.Assembly
{
    /// <summary>
    /// FASE 10: guia paso a paso generica y reutilizable.
    /// Recibe una lista de AssemblyStep (ensamblaje o desensamblaje) y permite
    /// navegar con "Anterior / Siguiente". Muestra numero, titulo, descripcion,
    /// hint AR y advertencia opcional. Al llegar al ultimo paso, "Siguiente"
    /// completa la guia y dispara OnCompleted.
    /// </summary>
    public class AssemblyGuideController : MonoBehaviour
    {
        private List<AssemblyStep> _steps = new List<AssemblyStep>();
        private int _currentIndex;

        private GUIStyle _stepStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _descStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _completionStyle;

        private bool _completed;

        /// <summary>Se dispara al terminar todos los pasos.</summary>
        public event Action OnCompleted;

        /// <summary>Se dispara al pulsar "Salir" para volver a la informacion.</summary>
        public event Action OnExitRequested;

        private void BuildStyles()
        {
            _stepStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 0.85f, 1f) }
            };

            _titleStyle = new GUIStyle
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _descStyle = new GUIStyle
            {
                fontSize = 20,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _hintStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            _warningStyle = new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = Color.yellow }
            };

            _completionStyle = new GUIStyle
            {
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.3f, 1f, 0.4f) }
            };
        }

        /// <summary>
        /// Configura la guia con los pasos a recorrer.
        /// Puede usarse para ensamblaje o desensamblaje (misma arquitectura).
        /// </summary>
        public void Configure(List<AssemblyStep> steps)
        {
            _steps = steps != null ? steps : new List<AssemblyStep>();
            _currentIndex = 0;
            _completed = _steps.Count == 0;
            if (_completed)
            {
                Debug.LogWarning("[PCAR][Assembly] Guia sin pasos configurados.");
            }
        }

        private void OnGUI()
        {
            if (_steps.Count == 0)
            {
                return;
            }

            float w = Screen.width * 0.9f;
            float x = (Screen.width - w) / 2f;
            const float top = 20f;
            const float bottomMargin = 150f;

            GUILayout.BeginArea(new Rect(x, top, w, Screen.height - top - bottomMargin));
            GUILayout.BeginVertical();

            if (_completed)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Procedimiento completado.", _completionStyle);
                GUILayout.FlexibleSpace();
            }
            else
            {
                var step = _steps[_currentIndex];
                DrawStep(step);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            DrawNavigationButtons();
        }

        private void DrawStep(AssemblyStep step)
        {
            GUILayout.Label("Paso " + step.stepNumber + " de " + _steps.Count, _stepStyle);
            GUILayout.Space(10f);
            GUILayout.Label(step.title, _titleStyle);
            GUILayout.Space(14f);
            GUILayout.Label(step.description, _descStyle);

            if (!string.IsNullOrEmpty(step.arHint))
            {
                GUILayout.Space(14f);
                GUILayout.Label("AR: " + step.arHint, _hintStyle);
            }

            if (!string.IsNullOrEmpty(step.warning))
            {
                GUILayout.Space(14f);
                GUILayout.Label("Advertencia: " + step.warning, _warningStyle);
            }
        }

        private void DrawNavigationButtons()
        {
            float w = Screen.width * 0.9f;
            float x = (Screen.width - w) / 2f;

            GUILayout.BeginArea(new Rect(x, Screen.height - 100f, w, 88f));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Salir", GUILayout.Height(70), GUILayout.Width(w * 0.2f)))
            {
                OnExitRequested?.Invoke();
            }

            if (_completed)
            {
                if (GUILayout.Button("Volver a la informacion", GUILayout.Height(70)))
                {
                    OnExitRequested?.Invoke();
                }
            }
            else
            {
                bool isFirst = _currentIndex == 0;
                bool isLast = _currentIndex == _steps.Count - 1;

                GUI.enabled = !isFirst;
                if (GUILayout.Button("Anterior", GUILayout.Height(70), GUILayout.ExpandWidth(true)))
                {
                    _currentIndex--;
                }
                GUI.enabled = true;

                if (GUILayout.Button(isLast ? "Finalizar" : "Siguiente", GUILayout.Height(70), GUILayout.ExpandWidth(true)))
                {
                    if (isLast)
                    {
                        _completed = true;
                        OnCompleted?.Invoke();
                    }
                    else
                    {
                        _currentIndex++;
                    }
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
