using UnityEditor;
using UnityEngine;

namespace PCAR.EditorTools
{
    /// <summary>
    /// FASE 13: construye los prefabs 3D de SSD, CPU y GPU con primitivos de Unity,
    /// replicando el patron de RamModelBuilder. Prefabs reutilizables en
    /// Assets/Prefabs/Components/. Sustituibles por modelos de Blender despues.
    /// </summary>
    public static class ComponentModelBuilder
    {
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabsPath = "Assets/Prefabs/Components";

        public static string BuildSsdPrefab()
        {
            return BuildPrefab("SSD", BuildSsd);
        }

        public static string BuildCpuPrefab()
        {
            return BuildPrefab("CPU", BuildCpu);
        }

        public static string BuildGpuPrefab()
        {
            return BuildPrefab("GPU", BuildGpu);
        }

        public static void BuildAllComponentPrefabs()
        {
            BuildSsdPrefab();
            BuildCpuPrefab();
            BuildGpuPrefab();
        }

        private static string BuildPrefab(string name, System.Action<GameObject> buildAction)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                var root = new GameObject(name);
                root.transform.localPosition = Vector3.zero;
                buildAction(root);

                string path = PrefabsPath + "/" + name + ".prefab";
                System.IO.Directory.CreateDirectory(PrefabsPath);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[PCAR][Model] Prefab creado: " + path);

                Object.DestroyImmediate(root);
                return path;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        // ---------- SSD (2.5") ----------
        private static void BuildSsd(GameObject root)
        {
            var casing = GetMaterial("SSD_Casing", new Color(0.25f, 0.25f, 0.28f));
            var label = GetMaterial("SSD_Label", new Color(0.9f, 0.9f, 0.95f));
            var connector = GetMaterial("SSD_Connector", new Color(0.4f, 0.5f, 0.6f));

            BuildBox(root, "Case", new Vector3(2.9f, 0.35f, 4.0f), Vector3.zero, casing);
            BuildBox(root, "Label", new Vector3(2.2f, 0.02f, 1.6f), new Vector3(0f, 0.19f, 0f), label);
            BuildBox(root, "Connector", new Vector3(0.6f, 0.3f, 0.5f), new Vector3(1.55f, 0.0f, 0f), connector);
        }

        // ---------- CPU (socket AM4) ----------
        private static void BuildCpu(GameObject root)
        {
            var ihs = GetMaterial("CPU_IHS", new Color(0.72f, 0.75f, 0.78f));
            var substrate = GetMaterial("CPU_Substrate", new Color(0.1f, 0.5f, 0.2f));
            var marking = GetMaterial("CPU_Marking", new Color(0.9f, 0.7f, 0.2f));

            BuildBox(root, "Substrate", new Vector3(1.8f, 0.12f, 1.8f), new Vector3(0f, -0.08f, 0f), substrate);
            BuildBox(root, "IHS", new Vector3(1.6f, 0.16f, 1.6f), new Vector3(0f, 0.02f, 0f), ihs);
            BuildBox(root, "Marking", new Vector3(0.25f, 0.02f, 0.25f), new Vector3(-0.65f, 0.10f, -0.65f), marking);
        }

        // ---------- GPU ----------
        private static void BuildGpu(GameObject root)
        {
            var pcb = GetMaterial("GPU_PCB", new Color(0.15f, 0.18f, 0.2f));
            var shroud = GetMaterial("GPU_Shroud", new Color(0.18f, 0.18f, 0.2f));
            var fan = GetMaterial("GPU_Fan", new Color(0.05f, 0.05f, 0.06f));
            var bracket = GetMaterial("GPU_Bracket", new Color(0.75f, 0.76f, 0.78f));
            var connector = GetMaterial("GPU_Connector", new Color(0.5f, 0.32f, 0.14f));

            // PCB largo y delgado
            BuildBox(root, "PCB", new Vector3(9.0f, 0.16f, 2.6f), new Vector3(0f, 0f, 0f), pcb);
            // Shroud / carcas superior
            BuildBox(root, "Shroud", new Vector3(7.6f, 1.5f, 2.5f), new Vector3(0.0f, 0.85f, 0f), shroud);
            // Dos ventiladores (cilindros)
            BuildCylinder(root, "Fan1", new Vector3(0.35f, 2.9f, 0.35f), new Vector3(-1.6f, 1.7f, 0f), fan);
            BuildCylinder(root, "Fan2", new Vector3(0.35f, 2.9f, 0.35f), new Vector3(1.6f, 1.7f, 0f), fan);
            // Bracket de fijacion trasera
            BuildBox(root, "Bracket", new Vector3(1.6f, 0.2f, 3.0f), new Vector3(4.7f, 0.7f, 0f), bracket);
            // Conector de alimentacion PCIe
            BuildBox(root, "PowerConnector", new Vector3(0.8f, 0.5f, 0.9f), new Vector3(3.9f, 0.9f, 0f), connector);
        }

        private static void BuildBox(GameObject parent, string name, Vector3 scale, Vector3 position, Material mat)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent.transform, false);
            cube.transform.localScale = scale;
            cube.transform.localPosition = position;
            var renderer = cube.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static void BuildCylinder(GameObject parent, string name, Vector3 scale, Vector3 position, Material mat)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent.transform, false);
            cylinder.transform.localScale = scale;
            cylinder.transform.localPosition = position;
            var renderer = cylinder.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = mat;
            }
        }

        private static Material GetMaterial(string name, Color color)
        {
            System.IO.Directory.CreateDirectory(MaterialsPath);
            string path = MaterialsPath + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(ResolveShader());
                mat.color = color;
                AssetDatabase.CreateAsset(mat, path);
            }

            return mat;
        }

        private static Shader ResolveShader()
        {
            var names = new[]
            {
                "Universal Render Pipeline/Lit",
                "Standard",
                "Unlit/Color",
                "Legacy Shaders/Diffuse"
            };

            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null)
                {
                    return s;
                }
            }

            var fallback = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
            return fallback.shader;
        }
    }
}
