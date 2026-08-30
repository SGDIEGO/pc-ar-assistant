using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCAR.Components
{
    /// <summary>
    /// FASE 7: repositorio de componentes.
    /// Carga los datos desde JSON (Recursos) una sola vez y sirve los
    /// componentes por id. Modular: agregar un componente nuevo =
    /// anadir un JSON en Resources/Data/Components sin tocar la logica.
    /// </summary>
    public static class ComponentRepository
    {
        private const string DataPath = "Data/Components";

        private static Dictionary<string, ComponentInfo> _cache;
        private static bool _initialized;

        private static void EnsureLoaded()
        {
            if (_initialized)
            {
                return;
            }

            _cache = new Dictionary<string, ComponentInfo>(StringComparer.OrdinalIgnoreCase);
            var folder = Resources.LoadAll<TextAsset>(DataPath);
            foreach (var asset in folder)
            {
                var info = JsonUtility.FromJson<ComponentInfo>(asset.text);
                if (info != null && !string.IsNullOrEmpty(info.id))
                {
                    _cache[info.id] = info;
                    Debug.Log($"[PCAR] Componente cargado: {info.id} -> {info.name}");
                }
                else
                {
                    Debug.LogWarning($"[PCAR] JSON invalido ignorado: {asset.name}");
                }
            }

            _initialized = true;
        }

        /// <summary>Devuelve un componente por id (case-insensitive) o null si no existe.</summary>
        public static ComponentInfo Get(string id)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            _cache.TryGetValue(id, out var info);
            return info;
        }

        /// <summary>Devuelve el componente RAM (reconocimiento simulado de Fase 8).</summary>
        public static ComponentInfo GetRam()
        {
            return Get("ram");
        }

        public static IEnumerable<ComponentInfo> GetAll()
        {
            EnsureLoaded();
            return _cache.Values;
        }

        public static bool Loaded => _initialized;

        /// <summary>Solo para pruebas: fuerza recarga desde disco.</summary>
        public static void Reload()
        {
            _initialized = false;
            _cache = null;
            EnsureLoaded();
        }
    }
}
