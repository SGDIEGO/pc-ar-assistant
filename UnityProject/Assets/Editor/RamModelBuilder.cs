using UnityEditor;
using UnityEngine;

namespace PCAR.EditorTools
{
    /// <summary>
    /// FASE 12: construye un modelo 3D de RAM con primitivos de Unity
    /// (Cubes) y lo guarda como Prefab reutilizable en Assets/Prefabs/Components/.
    /// Los modelos hechos con Blender (D:\Blender) pueden sustituir a este prefab
    /// mas adelante sin tocar el codigo.
    /// </summary>
    public static class RamModelBuilder
    {
        private const string MaterialsPath = "Assets/Materials";
        private const string PrefabPath = "Assets/Prefabs/Components/RAM.prefab";

        public static void BuildRamPrefab()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                var pcbMat = GetOrCreateMaterial("PCB_DarkGreen", new Color(0.15f, 0.45f, 0.20f));
                var chipMat = GetOrCreateMaterial("Chip_Black", new Color(0.12f, 0.12f, 0.14f));
                var goldMat = GetOrCreateMaterial("Contact_Gold", new Color(0.83f, 0.69f, 0.22f));

                var root = new GameObject("RAM");
                root.transform.localPosition = Vector3.zero;

                // Placa PCB (base alargada)
                BuildBox(root, "PCB", new Vector3(13.3f, 0.08f, 3.1f), Vector3.zero, pcbMat);

                // Chips laterales (izquierda y derecha) para dar el aspecto de modulos
                const int chipsPerSide = 8;
                for (int i = 0; i < chipsPerSide; i++)
                {
                    float z = -1.05f + i * 0.30f;
                    // columna izquierda
                    BuildBox(root, "ChipL" + i, new Vector3(1.1f, 0.10f, 0.24f), new Vector3(-5.0f, 0.16f, z), chipMat);
                    // columna derecha
                    BuildBox(root, "ChipR" + i, new Vector3(1.1f, 0.10f, 0.24f), new Vector3(5.0f, 0.16f, z), chipMat);
                }

                // Contactos dorados en el borde inferior (z = -1.55)
                const int contacts = 40;
                for (int i = 0; i < contacts; i++)
                {
                    float x = -6.35f + i * 0.32f;
                    BuildBox(root, "Contact" + i, new Vector3(0.16f, 0.10f, 0.45f), new Vector3(x, 0.10f, -1.32f), goldMat);
                }

                // Muesca de polaridad (una pequena pieza dorada desplazada en el centro)
                BuildBox(root, "Notch", new Vector3(1.1f, 0.10f, 0.5f), new Vector3(0f, 0.10f, -1.5f), goldMat);

                // Prefab reutilizable
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[PCAR][Model] Prefab RAM creado: " + PrefabPath);

                Object.DestroyImmediate(root);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
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

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            System.IO.Directory.CreateDirectory(MaterialsPath);
            string path = MaterialsPath + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = ResolveShader();
                mat = new Material(shader);
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

            // Ultimo recurso: el shader por defecto del material (siempre existe).
            var builtin = new Material(Shader.Find("Standard") ?? Shader.Find("Unlit/Color"));
            return builtin.shader;
        }
    }
}
