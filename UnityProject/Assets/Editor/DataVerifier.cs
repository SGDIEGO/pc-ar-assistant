using System.IO;
using PCAR.Components;
using UnityEngine;

namespace PCAR.EditorTools
{
    /// <summary>
    /// FASE 13: valida que todos los JSON de componentes carguen correctamente
    /// (misma ruta que usa ComponentRepository en runtime). Imprime un resumen.
    /// </summary>
    public static class DataVerifier
    {
        public static void Verify()
        {
            var t = Resources.LoadAll<TextAsset>("Data/Components");
            Debug.Log("[PCAR][Verify] Total JSON en Resources/Data/Components: " + t.Length);

            foreach (var asset in t)
            {
                var info = JsonUtility.FromJson<ComponentInfo>(asset.text);
                bool ok = info != null && !string.IsNullOrEmpty(info.id);
                if (ok)
                {
                    Debug.Log(string.Format(
                        "[PCAR][Verify] OK id={0} name={1} specs={2} warn={3} assem={4} disassem={5}",
                        info.id, info.name, info.specifications.Count, info.warnings.Count,
                        info.assemblySteps.Count, info.disassemblySteps.Count));
                }
                else
                {
                    Debug.LogError("[PCAR][Verify] JSON INVALIDO: " + asset.name);
                }
            }
        }
    }
}
