using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public static class SceneBuilder
{
    const string ModelPath = "Assets/Models/PC_Components_v2.fbx";
    const string CpuPreviewPath = "Assets/Models/cpu_preview.fbx";
    const string RamPreviewPath = "Assets/Models/ram_preview.fbx";
    const string SsdPreviewPath = "Assets/Models/ssd_preview.fbx";
    public const string ImageTargetName = "ImageTarget";
    public const string ScenePath = "Assets/Scenes/SampleScene.unity";

    // El marcador se imprime en A4 apaisado (29.7 x 21 cm). El PNG Assets/Markers/placa_madre_marker.png
    // mide 2480x1754 px, que son 83.50 px/cm en horizontal y 83.52 px/cm en vertical: esta hecho a
    // medida para A4. Estos valores TIENEN que coincidir con el tamano real impreso o el tracking
    // entrega una pose escalada y el modelo sale pequeno o descolocado.
    const float MarkerWidth = 0.297f;
    const float MarkerHeight = 0.21f;

    // Cuanto ocupa el modelo respecto al marcador. 1 = el modelo llena el ancho del papel.
    const float ModelFillFactor = 1.0f;

    // Compensacion por desajuste entre dataset Vuforia (entrenado con otra imagen/ratio)
    // y marcador impreso real. Si se reentrena el dataset con placa_madre_marker.png
    // a 0.297x0.21m, este factor debe volver a 1.0.
    const float ModelScaleCompensation = 100f;

    // El marcador A4 es apaisado (1.414:1) pero la huella proyectada del modelo es casi cuadrada
    // (~1:1), asi que ajustar por los dos ejes dejaba el ancho clavado en el alto del papel
    // (0.21 m) y el modelo no crecia nada. Se ajusta por el ancho y se permite que el alto
    // desborde el papel hasta este factor: el modelo se sale por arriba y por abajo, que en AR
    // se lee como intencionado y es lo que hace que se vea mas grande.
    const float ModelHeightOverflow = 1.45f;

    // Giro del modelo sobre su eje normal, en grados, para alinearlo con el marcador.
    // Valores utiles: 0, 90, 180, -90 (segun hacia donde quede el panel I/O del marcador).
    const float ModelYawDegrees = 0f;

    // Inclinacion del modelo respecto al plano del marcador y altura de su base sobre el papel.
    // Son los dos parametros a tocar cuando el modelo se vea muy plano o muy suelto.
    const float ModelTiltDegrees = -30f;
    const float ModelElevation = 0.04f;

    const string OutlineMaterialPath = "Assets/Resources/SelectionOutline.mat";

    [MenuItem("Tools/AR/1. Importar recursos TMP", false, 1)]
    public static void ImportTMP()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
        TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] TMP Essential Resources importadas.");
    }

    // SelectionOutline lee los vertices de la malla en runtime, lo que exige Read/Write Enabled.
    // Sin esto el contorno de seleccion lanza UnityException en el dispositivo, aunque en el
    // editor funcione, porque en Play Mode las mallas siempre son legibles.
    [MenuItem("Tools/AR/5. Corregir importacion del modelo", false, 5)]
    public static void FixModelImport()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("[SceneBuilder] No se encontro ModelImporter en " + ModelPath);
            return;
        }
        if (importer.isReadable) { Debug.Log("[SceneBuilder] isReadable ya activo."); return; }

        importer.isReadable = true;
        importer.SaveAndReimport();
        Debug.Log("[SceneBuilder] isReadable=true aplicado en " + ModelPath);
    }

    [MenuItem("Tools/AR/4. Diagnóstico modelo", false, 4)]
    public static void Diagnose()
    {
        Object prefab = AssetDatabase.LoadMainAssetAtPath(ModelPath);
        if (prefab == null) { Debug.LogError("Sin modelo"); return; }
        GameObject m = (GameObject)Object.Instantiate(prefab);
        int total = 0, unmatched = 0;
        foreach (MeshRenderer r in m.GetComponentsInChildren<MeshRenderer>())
        {
            foreach (Material mt in r.sharedMaterials)
            {
                total++;
                if (mt == null || !mt.name.StartsWith("PC_"))
                {
                    unmatched++;
                    if (unmatched <= 25)
                        Debug.Log("[D] Renderer=" + r.name + " material=" + (mt == null ? "<null>" : mt.name));
                }
            }
        }
        Debug.Log("[D] total materiales=" + total + " sin PC_=" + unmatched);
        LocalBounds lb = GetLocalBounds(m.transform);
        if (lb.valid)
            Debug.Log("[D] bounds local X=" + (lb.max.x - lb.min.x).ToString("0.000")
                + " Y=" + (lb.max.y - lb.min.y).ToString("0.000")
                + " Z=" + (lb.max.z - lb.min.z).ToString("0.000")
                + " upAxis=" + lb.upAxis);
        Object.DestroyImmediate(m);
    }

    // El ImageTargetBehaviour de Vuforia viene como DLL compilado, asi que no se puede leer su codigo
// para saber que relacion impone entre mWidth / mHeight / mAspectRatio. Este probe escribe valores
// conocidos y vuelve a leerlos tras aplicar, para deducirla empiricamente.
//
// SIN MenuItem a proposito: modifica los tres campos del target, asi que no debe poder dispararse
// por accidente desde el menu. Se invoca con -executeMethod BuildScript.ProbeOnly. Conclusion
// obtenida: mAspectRatio es ALTO/ANCHO y el componente conserva la ratio recalculando el eje que
// no se toca.
public static void ProbeTargetSize()
    {
        GameObject target = GameObject.Find(ImageTargetName);
        if (target == null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            target = GameObject.Find(ImageTargetName);
        }
        ImageTargetBehaviour itb = target != null ? target.GetComponent<ImageTargetBehaviour>() : null;
        if (itb == null)
        {
            Debug.LogError("[Probe] No se encontro ImageTargetBehaviour.");
            return;
        }

        Debug.Log("[Probe] ESTADO INICIAL: " + Dump(itb));

        // Prueba 1: solo mWidth y mAspectRatio (mHeight se recalcula).
        Set(itb, 0.297f, -1f, 1.4142857f);
        Debug.Log("[Probe] P1 tras set w=0.297, ar=1.4142857: " + Dump(itb));

        // Prueba 2: mHeight ToF de la relacion hallada en P1.
        Set(itb, 0.297f, 0.21f, -1f);
        Debug.Log("[Probe] P2 tras set w=0.297, h=0.21: " + Dump(itb));

        // Prueba 3: los tres a la vez.
        Set(itb, 0.297f, 0.21f, 0.7071429f);
        Debug.Log("[Probe] P3 tras set w=0.297, h=0.21, ar=0.7071429: " + Dump(itb));

        Debug.LogWarning("[Probe] La escena queda MODIFICADA por el probe; ejecutar despues"
            + " Tools/AR/3. Configurar ImageTarget para dejar los valores correctos.");
    }

    static string Dump(ImageTargetBehaviour itb)
    {
        SerializedObject so = new SerializedObject(itb);
        return "mWidth=" + Read(so, "mWidth")
            + " mHeight=" + Read(so, "mHeight")
            + " mAspectRatio=" + Read(so, "mAspectRatio");
    }

    static string Read(SerializedObject so, string prop)
    {
        SerializedProperty p = so.FindProperty(prop);
        return p == null ? "<no existe>" : p.floatValue.ToString("0.######");
    }

    static void Set(ImageTargetBehaviour itb, float width, float height, float aspect)
    {
        SerializedObject so = new SerializedObject(itb);
        if (width >= 0f) so.FindProperty("mWidth").floatValue = width;
        if (height >= 0f) so.FindProperty("mHeight").floatValue = height;
        if (aspect >= 0f) so.FindProperty("mAspectRatio").floatValue = aspect;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/AR/3. Configurar ImageTarget (dataset nuevo)", false, 3)]
    public static void ConfigureTarget()
    {
        GameObject target = GameObject.Find(ImageTargetName);
        if (target == null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            target = GameObject.Find(ImageTargetName);
        }
        if (target == null)
        {
            Debug.LogError("[SceneBuilder] No se encontró '" + ImageTargetName + "'.");
            return;
        }

        const string trackable = "placa_madre_referencia_ccbysa";
        const string dataset = "Vuforia/Componentes.xml";
        float width = MarkerWidth;
        float height = MarkerHeight;

        ImageTargetBehaviour itb = target.GetComponent<ImageTargetBehaviour>();
        bool changed = false;
        if (itb != null)
        {
            // mAspectRatio de Vuforia es ALTO/ANCHO, no ancho/alto (deducido con Tools/AR/7:
            // el componente conserva la ratio y recalcula el eje que no se toca). Por eso el valor
            // es height/width y no width/height; con 1.414 Vuforia reportaba mHeight=0.42 en vez
            // de 0.21 y la pose metricica del marcador quedaba mal escalada.
            float aspect = height / width;

            SerializedObject so = new SerializedObject(itb);
            so.FindProperty("mTrackableName").stringValue = trackable;
            so.FindProperty("mDataSetPath").stringValue = dataset;
            so.FindProperty("mImageTargetType").intValue = 0;
            so.FindProperty("mWidth").floatValue = width;
            so.FindProperty("mAspectRatio").floatValue = aspect;
            // mHeight se fija despues de la ratio para que OnValidate no lo recalcule.
            so.FindProperty("mHeight").floatValue = height;
            changed = so.ApplyModifiedPropertiesWithoutUndo();

            // OnValidate puede reescribir mHeight; se vuelve a fijar y se verifica.
            SerializedObject check = new SerializedObject(itb);
            SerializedProperty hProp = check.FindProperty("mHeight");
            if (hProp != null && Mathf.Abs(hProp.floatValue - height) > 1e-4f)
            {
                hProp.floatValue = height;
                check.ApplyModifiedPropertiesWithoutUndo();
            }
            Debug.Log("[SceneBuilder] Verificacion: " + Dump(itb));
        }

        foreach (MonoBehaviour mb in target.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb == (itb as MonoBehaviour)) continue;
            SerializedObject mbSo = new SerializedObject(mb);
            SerializedProperty p = mbSo.FindProperty("mTargetName");
            if (p != null)
            {
                p.stringValue = trackable;
                mbSo.ApplyModifiedPropertiesWithoutUndo();
            }
            p = mbSo.FindProperty("mDatasetName");
            if (p != null)
            {
                p.stringValue = dataset;
                mbSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        target.transform.localPosition = Vector3.zero;
        EditorUtility.SetDirty(target);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] ImageTarget configurado: '" + trackable + "' en " + dataset + " (" + width + " x " + height + " m). Cambios: " + changed);
        AssetDatabase.Refresh();
    }

    // SelectionOutline carga este material desde Resources en runtime. Al estar en Resources Unity lo
// empaqueta en el build siempre, asi que ya no hace falta Shader.Find (que en un player solo
// encuentra shaders incluidos explicitamente y por eso a veces devolvia null).
[MenuItem("Tools/AR/6. Crear material del contorno", false, 6)]
    public static void CreateOutlineMaterial()
    {
        Shader unlit = Shader.Find("Unlit/Color");
        if (unlit == null)
        {
            Debug.LogError("[SceneBuilder] No se encontro el shader 'Unlit/Color'. "
                + "El contorno dejaria de verse; revisa que el proyecto siga en Built-in RP.");
            return;
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        bool created = false;
        if (mat == null)
        {
            EnsureFolder("Assets/Resources");
            mat = new Material(unlit) { name = "SelectionOutline" };
            AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
            AssetDatabase.ImportAsset(OutlineMaterialPath);
            mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            created = true;
        }

        if (mat == null)
        {
            Debug.LogError("[SceneBuilder] No se pudo crear " + OutlineMaterialPath);
            return;
        }

        mat.shader = unlit;
        mat.color = new Color(0f, 0.92f, 1f, 1f);
        if (mat.HasProperty("_ZTest")) mat.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)CullMode.Off);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        Debug.Log("[SceneBuilder] Material del contorno " + (created ? "creado" : "actualizado")
            + " en " + OutlineMaterialPath + " con shader " + unlit.name);
    }

    [MenuItem("Tools/AR/2. Reconstruir escena AR", false, 2)]
    public static void RebuildScene()
    {
        ImportTMP();
        FixModelImport();
        CreateOutlineMaterial();

        GameObject target = GameObject.Find(ImageTargetName);
        if (target == null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            target = GameObject.Find(ImageTargetName);
        }
        if (target == null)
        {
            Debug.LogError("[SceneBuilder] No se encontrÃ³ '" + ImageTargetName + "'. No se puede reconstruir.");
            return;
        }

        var children = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in target.transform) children.Add(child);
        foreach (Transform child in children)
            if (child != null) Object.DestroyImmediate(child.gameObject);

        CleanStaleRoots(target);

        Camera cam = GameObject.Find("ARCamera") != null
            ? GameObject.Find("ARCamera").GetComponent<Camera>()
            : Camera.main;
        if (cam == null)
        {
            Debug.LogError("[SceneBuilder] No se encontrÃ³ la cÃ¡mara AR.");
            return;
        }

        GameObject model = BuildModel(target.transform);
        if (model == null) return;

        ComponentInfoUI panel = BuildInfoPanel3D(cam, target.transform, model);
        BuildInteraction(cam, model, panel);

        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SceneBuilder] Escena reconstruida OK. Modelo=" + model.name);
    }

    static void CleanStaleRoots(GameObject keep)
    {
        int removed = 0;
        foreach (GameObject go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go == keep || go == null) continue;
            if (go.GetComponent<EventSystem>() != null) continue;

            bool stale = go.name.StartsWith("AR_UI")
                || go.name == "Interaction"
                || go.name == "PC_Model"
                || go.GetComponent<Canvas>() != null
                || go.GetComponent<ComponentInfoUI>() != null
                || go.GetComponent<InfoLabel3D>() != null;

            if (stale)
            {
                Debug.Log("[SceneBuilder] Eliminado residuo raiz: " + go.name);
                Object.DestroyImmediate(go);
                removed++;
            }
        }
        if (removed > 0)
            Debug.Log("[SceneBuilder] Residuos raiz eliminados: " + removed);
    }

    static GameObject BuildModel(Transform parent)
    {
        Object prefab = AssetDatabase.LoadMainAssetAtPath(ModelPath);
        if (prefab == null)
        {
            Debug.LogError("[SceneBuilder] No se encontrÃ³ el modelo en " + ModelPath);
            return null;
        }

        GameObject model = (GameObject)Object.Instantiate(prefab);
        model.name = "PC_Model";
        model.transform.SetParent(parent, false);

        LocalBounds lb = GetLocalBounds(model.transform);
        if (lb.valid)
        {
            Quaternion align = Quaternion.FromToRotation(lb.upAxis, Vector3.forward);
            model.transform.localRotation = align * Quaternion.Euler(ModelTiltDegrees, ModelYawDegrees, 0f);
        }

        Bounds raw = WorldBounds(model);
        float s = FitScale(model, MarkerWidth, MarkerHeight);
        model.transform.localScale = new Vector3(s, s, s) * ModelScaleCompensation;
        Debug.Log("[SceneBuilder] Bounds sin escala X=" + raw.size.x.ToString("0.000")
            + " Y=" + raw.size.y.ToString("0.000") + " Z=" + raw.size.z.ToString("0.000")
            + " -> escala=" + s.ToString("0.0000")
            + ", ancho final=" + (raw.size.x * s).ToString("0.000") + " m"
            + ", alto final=" + (raw.size.y * s).ToString("0.000") + " m"
            + " (marcador " + MarkerWidth + " x " + MarkerHeight + " m)");

        CenterOnMarker(model.transform, parent.transform, ModelElevation);

        AssignMaterials(model);
        AddColliders(model);
        SetupHitboxes(model);
        BuildPreview(model.transform);

        // Unity colapsa la raiz unica del FBX como raiz del prefab y esta se renombra a PC_Model,
        // por lo que MotherboardRoot puede o no existir como nivel intermedio.
        Transform cpu = FindAssembly(model.transform, "CPU_Assembly");
        Transform ram = FindAssembly(model.transform, "RAM_Assembly");
        Transform ssd = FindAssembly(model.transform, "SSD_Assembly");

        if (cpu != null) cpu.gameObject.AddComponent<ComponentInfo>(); Configure(cpu, PCComponentsData.GetCPU());
        if (ram != null) ram.gameObject.AddComponent<ComponentInfo>(); Configure(ram, PCComponentsData.GetRAM());
        if (ssd != null) ssd.gameObject.AddComponent<ComponentInfo>(); Configure(ssd, PCComponentsData.GetSSD());

        if (cpu == null || ram == null || ssd == null)
            Debug.LogWarning("[SceneBuilder] No se localizaron todas las asamblajes (CPU/RAM/SSD).");

        return model;
    }

    static void Configure(Transform t, ComponentInfo data)
    {
        if (t == null) return;
        ComponentInfo ci = t.GetComponent<ComponentInfo>();
        ci.componentName = data.componentName;
        ci.buttonLabel = data.buttonLabel;
        ci.description = data.description;
        ci.function = data.function;
        ci.specifications = data.specifications;
        ci.assembleSteps = data.assembleSteps;
        ci.disassembleSteps = data.disassembleSteps;
    }

    static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder.Substring(folder.LastIndexOf('/') + 1));
        }
    }

    static void AssignMaterials(GameObject root)
    {
        EnsureFolder("Assets/Materials");

        // Este proyecto corre con el Built-in Render Pipeline (no hay ningun Render Pipeline Asset
        // asignado en Graphics/Quality Settings), asi que "Standard" es el shader correcto.
        // Si se activase URP, estos materiales se verian en magenta y habria que migrarlos a
        // "Universal Render Pipeline/Lit" con _BaseColor / _Metallic / _Smoothness.
        Shader std = Shader.Find("Standard");
        if (std == null)
        {
            Debug.LogError("[SceneBuilder] No se encontro el shader Standard. "
                + "No se asignan materiales para evitar crearlos con un shader nulo.");
            return;
        }

        var palette = BuildPalette();
        Material fallback = GetOrCreateMaterial(std, "PC_default", new PaletteColor
        {
            color = new Color(0.45f, 0.45f, 0.48f),
            metallic = 0f,
            smoothness = 0.4f
        });

        int reassigned = 0, defaulted = 0;
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.sharedMaterials == null || r.sharedMaterials.Length == 0) continue;
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];

                // La paleta se indexa sin el prefijo "PC_" que llevan los assets ya creados, asi
                // que hay que quitarselo antes de buscar. Sin esto, la segunda vez que se ejecuta
                // RebuildScene todos los materiales caen en el fallback y el modelo pierde el color.
                string key = src != null ? src.name : null;
                if (key != null && key.StartsWith("PC_")) key = key.Substring(3);

                // El FBX se importa con materialImportMode=2 y externalObjects vacio, asi que los
                // renderers cuyo material no esta en la paleta conservan el material embebido del
                // FBX. Se sustituyen por el material por defecto para que nada se quede sin color.
                // La variable out se declara en la propia condicion porque Roslyn solo garantiza
                // que queda asignada dentro de ese if.
                if (key != null && palette.TryGetValue(key, out PaletteColor p))
                {
                    mats[i] = GetOrCreateMaterial(std, "PC_" + key, p);
                    reassigned++;
                }
                else
                {
                    Debug.LogWarning("[SceneBuilder] Renderer '" + r.name + "' con material '"
                        + (src == null ? "<null>" : src.name) + "' fuera de la paleta; se usa PC_default.");
                    mats[i] = fallback;
                    defaulted++;
                }
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
        Debug.Log("[SceneBuilder] Materiales: " + reassigned + " de la paleta, " + defaulted + " por defecto.");
    }

    static Material GetOrCreateMaterial(Shader shader, string matName, PaletteColor p)
    {
        string assetPath = "Assets/Materials/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat != null) return mat;

        mat = new Material(shader) { name = matName };
        mat.SetColor("_Color", p.color);
        mat.SetFloat("_Metallic", p.metallic);
        mat.SetFloat("_Glossiness", p.smoothness);
        mat.enableInstancing = true;
        AssetDatabase.CreateAsset(mat, assetPath);
        return mat;
    }

    struct PaletteColor { public Color color; public float metallic, smoothness; }

    static System.Collections.Generic.Dictionary<string, PaletteColor> BuildPalette()
    {
        var d = new System.Collections.Generic.Dictionary<string, PaletteColor>();
        void Add(string n, Color c, float met, float sm)
        {
            d[n] = new PaletteColor { color = c, metallic = met, smoothness = sm };
        }
        Add("board_dark", new Color(0.055f, 0.16f, 0.10f), 0f, 0.35f);
        Add("board_light", new Color(0.09f, 0.24f, 0.15f), 0f, 0.35f);
        Add("black", new Color(0.10f, 0.10f, 0.10f), 0f, 0.45f);
        Add("darkgray", new Color(0.22f, 0.22f, 0.24f), 0f, 0.4f);
        Add("silver", new Color(0.62f, 0.62f, 0.66f), 0.9f, 0.6f);
        Add("gold", new Color(0.85f, 0.69f, 0.30f), 1f, 0.7f);
        Add("white", new Color(0.93f, 0.93f, 0.95f), 0f, 0.5f);
        Add("green", new Color(0.09f, 0.28f, 0.16f), 0f, 0.4f);
        return d;
    }

    static void AddColliders(GameObject root)
    {
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.GetComponent<Collider>() == null)
                r.gameObject.AddComponent<MeshCollider>();
        }
    }

    // Vitrina flotante: muestra el modelo 3D ampliado de la pieza seleccionada en un
    // punto fijo sobre la placa. Las proporciones vienen HORNEADAS de Blender (cada
    // preview mide 0.532 m = 28% de la placa) y aqui solo se usan datos de malla
    // (Mesh.bounds) y valores locales: a proposito NO se usan Renderer.bounds ni
    // matrices de mundo, que en batchmode devuelven datos obsoletos y dejaban la
    // vitrina microscopica/invisible. Sin colliders (los taps atraviesan) y apagados
    // hasta que ComponentSelector los pida.
    static void BuildPreview(Transform model)
    {
        Shader std = Shader.Find("Standard");
        if (std == null)
        {
            Debug.LogError("[SceneBuilder] Sin shader Standard, no se arma la vitrina.");
            return;
        }

        Transform unifiedT = FindRecursive(model, "Motherboard_Unified");
        // OJO: el MeshFilter PROPIO del unificado (no el primero de los hijos, que
        // seria una caja limite). El unificado no tiene hijos con malla.
        MeshFilter unifiedMF = unifiedT != null ? unifiedT.GetComponent<MeshFilter>() : null;
        Mesh boardMesh = unifiedMF != null ? unifiedMF.sharedMesh : null;
        if (boardMesh == null)
        {
            Debug.LogWarning("[SceneBuilder] Sin malla unificada, no se arma la vitrina.");
            return;
        }
        Bounds bb = boardMesh.bounds;
        float boardLongest = Mathf.Max(bb.size.x, Mathf.Max(bb.size.y, bb.size.z));
        Vector3 boardThinAxis = bb.size.x <= bb.size.y && bb.size.x <= bb.size.z ? Vector3.right
            : (bb.size.y <= bb.size.z ? Vector3.up : Vector3.forward);
        float boardThinHalf = (Mathf.Abs(boardThinAxis.x) > 0.5f ? bb.size.x
            : (Mathf.Abs(boardThinAxis.y) > 0.5f ? bb.size.y : bb.size.z)) * 0.5f;
        if (boardLongest < 1e-6f)
        {
            Debug.LogWarning("[SceneBuilder] Malla unificada vacia, no se arma la vitrina.");
            return;
        }

        GameObject anchor = new GameObject("PreviewShowcase");
        anchor.transform.SetParent(unifiedT, false);
        anchor.transform.localPosition = Vector3.zero;
        anchor.transform.localRotation = Quaternion.identity;

        var defs = new (string path, string label, string matName, Color color, float metallic)[]
        {
            (CpuPreviewPath, "CPU", "PC_PreviewCPU", new Color(0.72f, 0.73f, 0.76f), 0.15f),
            (RamPreviewPath, "RAM", "PC_PreviewRAM", new Color(0.10f, 0.30f, 0.17f), 0f),
            (SsdPreviewPath, "SSD", "PC_PreviewSSD", new Color(0.85f, 0.69f, 0.30f), 0.3f),
        };

        GameObject cpuGo = null, ramGo = null, ssdGo = null;
        float previewHalfMax = 0f;
        float previewLongMax = 0f;
        const float showcaseScale = 1.6f;
        foreach (var d in defs)
        {
            Object prefab = AssetDatabase.LoadMainAssetAtPath(d.path);
            if (prefab == null)
            {
                Debug.LogWarning("[SceneBuilder] Sin preview en " + d.path);
                continue;
            }
            GameObject prefabGo = prefab as GameObject;
            Mesh prefabMesh = prefabGo != null ? prefabGo.GetComponentInChildren<MeshFilter>().sharedMesh : null;
            if (prefabMesh == null)
            {
                Debug.LogWarning("[SceneBuilder] Preview sin malla: " + d.path);
                continue;
            }
            Bounds pb = prefabMesh.bounds;
            float pLongest = Mathf.Max(pb.size.x, Mathf.Max(pb.size.y, pb.size.z));
            Vector3 pThin = pb.size.x <= pb.size.y && pb.size.x <= pb.size.z ? Vector3.right
                : (pb.size.y <= pb.size.z ? Vector3.up : Vector3.forward);
            float pThinSize = Mathf.Abs(pThin.x) > 0.5f ? pb.size.x
                : (Mathf.Abs(pThin.y) > 0.5f ? pb.size.y : pb.size.z);
            if (pLongest < 1e-6f)
            {
                Debug.LogWarning("[SceneBuilder] Preview vacio: " + d.path);
                continue;
            }

            GameObject go = (GameObject)Object.Instantiate(prefab);
            go.name = "Preview_" + d.label;
            go.transform.SetParent(anchor.transform, false);
            go.transform.localPosition = Vector3.zero;
            // Tamano de vitrina (multiplicador fijo sobre lo horneado en Blender).
            go.transform.localScale = Vector3.one * showcaseScale;

            // Collider SOLO para deteccion tactil (la malla sigue sin renderer propio
            // nuevo; los taps los resuelve ComponentSelector).
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.convex = false;

            Material m = GetOrCreateMaterial(std, d.matName,
                new PaletteColor { color = d.color, metallic = d.metallic, smoothness = 0.5f });
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>())
                r.sharedMaterial = m;

            // Eje delgado del preview hacia la normal de la placa (todo en espacio local
            // del ancla; r0 es la rotacion de importacion del FBX, leida, no supuesta).
            Quaternion r0 = go.transform.localRotation;
            Quaternion q = Quaternion.FromToRotation((r0 * pThin).normalized, boardThinAxis);
            go.transform.localRotation = q * r0;

            previewHalfMax = Mathf.Max(previewHalfMax, pThinSize * 0.5f * showcaseScale);
            previewLongMax = Mathf.Max(previewLongMax, pLongest * showcaseScale);

            go.SetActive(false);
            if (d.label == "CPU") cpuGo = go;
            else if (d.label == "RAM") ramGo = go;
            else ssdGo = go;
        }

        if (cpuGo == null && ramGo == null && ssdGo == null)
        {
            Debug.LogWarning("[SceneBuilder] Vitrina vacia, se retira el ancla.");
            Object.DestroyImmediate(anchor);
            return;
        }

        float gap = previewLongMax * 1.0f;
        anchor.transform.localPosition = bb.center + boardThinAxis * (boardThinHalf + gap + previewHalfMax);

        ComponentPreview3D preview = anchor.AddComponent<ComponentPreview3D>();
        preview.cpuModel = cpuGo;
        preview.ramModel = ramGo;
        preview.ssdModel = ssdGo;
        preview.spinAxis = boardThinAxis;
        preview.maxRadius = boardLongest * 0.5f;
        preview.minAxial = -gap;
        Debug.Log("[SceneBuilder] Vitrina lista sobre la placa (board=" + bb.size.ToString("F4")
            + " ancla=" + anchor.transform.localPosition.ToString("F4") + ").");
    }

    static float ThinHalfWorld(LocalBounds lb, Transform model)
    {
        Vector3 size = lb.max - lb.min;
        float thin = Mathf.Abs(lb.upAxis.x) > 0.5f ? size.x
            : (Mathf.Abs(lb.upAxis.y) > 0.5f ? size.y : size.z);
        float s = Mathf.Abs(model.lossyScale.x);
        return thin * 0.5f * (s > 1e-6f ? s : 1f);
    }
    // - Motherboard_Unified pierde su collider para no bloquear los taps (sigue visible).
    // - A cada *_Limit se le CONSERVA el MeshRenderer y se le pone un material translucido
    //   de su color, para que en el dispositivo se vea que area de la placa corresponde a
    //   cada componente (antes se destruia el renderer y solo quedaba el MeshFilter para
    //   el contorno cyan y el MeshCollider para el tap: solo se veia la placa).
    //   Se mantienen tambien el MeshFilter y el MeshCollider: el contorno cyan sigue
    //   leyendo la malla y el tap sigue entrando por el collider.
    static void SetupHitboxes(GameObject root)
    {
        Transform unified = FindRecursive(root.transform, "Motherboard_Unified");
        if (unified != null)
        {
            foreach (Collider c in unified.GetComponents<Collider>())
                Object.DestroyImmediate(c);
            Debug.Log("[SceneBuilder] Collider retirado de Motherboard_Unified (visual intacto).");
        }

        // Losa de la placa leida de la MALLA, no de Renderer.bounds (en batchmode devuelve
        // datos obsoletos). Sirve de referencia para sacar las cajas de su interior.
        Vector3 thinAxis = Vector3.forward;
        float boardCenterAlong = 0f;
        float boardHalf = 0f;
        MeshFilter unifiedMF = unified != null ? unified.GetComponent<MeshFilter>() : null;
        Mesh boardMesh = unifiedMF != null ? unifiedMF.sharedMesh : null;
        if (boardMesh != null)
        {
            Vector3 size = boardMesh.bounds.size;
            thinAxis = size.x <= size.y && size.x <= size.z ? Vector3.right
                : (size.y <= size.z ? Vector3.up : Vector3.forward);
            float thinSize = Mathf.Abs(thinAxis.x) > 0.5f ? size.x
                : (Mathf.Abs(thinAxis.y) > 0.5f ? size.y : size.z);
            boardCenterAlong = Vector3.Dot(boardMesh.bounds.center, thinAxis);
            boardHalf = thinSize * 0.5f;
        }
        else
        {
            Debug.LogWarning("[SceneBuilder] Sin malla de Motherboard_Unified: "
                + "las cajas no se pueden realinear sobre la superficie.");
        }

        var zones = new (string limit, string material, Color color)[]
        {
            ("CPU_Limit", "PC_ZoneCPU", new Color(0.20f, 0.78f, 1.00f)),
            ("RAM_Limit", "PC_ZoneRAM", new Color(0.30f, 1.00f, 0.55f)),
            ("SSD_Limit", "PC_ZoneSSD", new Color(1.00f, 0.76f, 0.28f)),
        };

        int ok = 0;
        foreach (var z in zones)
        {
            Transform t = FindRecursive(root.transform, z.limit);
            if (t == null)
            {
                Debug.LogWarning("[SceneBuilder] No se encontro hitbox " + z.limit + ".");
                continue;
            }
            if (t.GetComponent<Collider>() == null)
                t.gameObject.AddComponent<MeshCollider>();

            MeshRenderer r = t.GetComponent<MeshRenderer>();
            if (r == null)
            {
                Debug.LogWarning("[SceneBuilder] " + z.limit + " sin MeshRenderer tras importar el FBX.");
                continue;
            }
            r.sharedMaterial = GetOrCreateZoneMaterial(z.material, z.color);
            RaiseAboveBoard(t, thinAxis, boardCenterAlong + boardHalf);
            ok++;
        }
        Debug.Log("[SceneBuilder] Zonas tactiles visibles: " + ok + "/3.");
    }

    // En el modelo v6 las cajas quedan HUECAS dentro del volumen de la placa: su centro cae
    // dentro del grosor de la losa, asi que la placa (opaca) las tapa y no se ven. Se compara
    // el centro de la caja con la cara superior de la losa y se desplaza por el eje delgado
    // lo justo para que su base apoye en la superficie. Los *_Limit cuelgan de los
    // *_Assembly con transformacion identidad, asi que ambos espacios coinciden.
    static void RaiseAboveBoard(Transform limit, Vector3 thinAxis, float boardSurface)
    {
        MeshFilter mf = limit.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("[SceneBuilder] " + limit.name + " sin malla: no se realinea.");
            return;
        }

        Bounds b = mf.sharedMesh.bounds;
        float centerAlong = Vector3.Dot(b.center, thinAxis);
        float halfAlong = Mathf.Abs(Vector3.Dot(thinAxis, b.extents));
        if (halfAlong < 1e-6f) return;

        // Base de la caja en la cara superior de la placa, mas un pequeno margen
        // (10% de su semigrosor) para que no pegue con la superficie.
        float margin = halfAlong * 0.10f;
        float targetCenter = boardSurface + margin + halfAlong;
        float delta = targetCenter - centerAlong;
        if (Mathf.Abs(delta) < 1e-6f) return;

        limit.localPosition += thinAxis * delta;
        Debug.Log("[SceneBuilder] " + limit.name + " alzada " + delta.ToString("0.0000")
            + " sobre la placa (semigrosor=" + halfAlong.ToString("0.0000") + ").");
    }

    // Material translucido para las cajas que encapsulan cada componente. El shader es
    // "Standard" (proyecto en Built-in Render Pipeline) y se pasa a modo transparente a
    // mano: _Mode 3 + keyword _ALPHABLEND_ON + blend SrcAlpha/OneMinusSrcAlpha + ZWrite
    // off + cola de render transparente (3000). Se re-aplica al material existente para
    // que ajustar el color o la opacidad surta efecto aunque el asset ya este creado.
    static Material GetOrCreateZoneMaterial(string matName, Color color)
    {
        Shader std = Shader.Find("Standard");
        if (std == null)
        {
            Debug.LogError("[SceneBuilder] No se encontro el shader Standard; "
                + "las zonas quedarian invisibles.");
            return null;
        }

        EnsureFolder("Assets/Materials");
        string assetPath = "Assets/Materials/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        bool created = mat == null;
        if (created)
        {
            mat = new Material(std) { name = matName };
            AssetDatabase.CreateAsset(mat, assetPath);
        }

        const float alpha = 0.22f;
        mat.shader = std;
        mat.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));
        mat.SetFloat("_Metallic", 0.05f);
        mat.SetFloat("_Glossiness", 0.45f);
        mat.SetFloat("_Mode", 3f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.enableInstancing = true;
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();

        if (created)
            Debug.Log("[SceneBuilder] Material de zona translucido creado: " + assetPath);
        return mat;
    }

    struct LocalBounds
    {
        public bool valid;
        public Vector3 min, max;
        public bool bigX, bigY, bigZ;
        public Vector3 upAxis;
        public Vector3 center { get { return (min + max) * 0.5f; } }
    }

    static LocalBounds GetLocalBounds(Transform root)
    {
        Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 mx = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        bool any = false;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            if (r.bounds.size.sqrMagnitude < 1e-12f) continue;
            Bounds b = r.bounds;
            Vector3[] corners =
            {
                new Vector3(b.min.x, b.min.y, b.min.z), new Vector3(b.min.x, b.min.y, b.max.z),
                new Vector3(b.min.x, b.max.y, b.min.z), new Vector3(b.min.x, b.max.y, b.max.z),
                new Vector3(b.max.x, b.min.y, b.min.z), new Vector3(b.max.x, b.min.y, b.max.z),
                new Vector3(b.max.x, b.max.y, b.min.z), new Vector3(b.max.x, b.max.y, b.max.z),
            };
            foreach (Vector3 c in corners)
            {
                Vector3 l = root.InverseTransformPoint(c);
                mn = Vector3.Min(mn, l);
                mx = Vector3.Max(mx, l);
                any = true;
            }
        }
        float ex = mx.x - mn.x, ey = mx.y - mn.y, ez = mx.z - mn.z;
        return new LocalBounds
        {
            valid = any,
            min = mn,
            max = mx,
            upAxis = (ex <= ey && ex <= ez) ? Vector3.right : ((ey <= ez) ? Vector3.up : Vector3.forward)
        };
    }

    static Bounds WorldBounds(GameObject root)
    {
        Renderer[] rs = root.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);
        return b;
    }

    // Ajusta la escala para que el modelo llene el ancho del marcador, permitiendo que el alto
    // desborde el papel hasta ModelHeightOverflow.
//
// La version anterior usaba Mathf.Max(size.x, size.y). Como el modelo se inclina solo sobre X,
// size.x no cambia al inclinar pero size.y si, de modo que la escala resultante dependia de la
// inclinacion: por eso la escena guardaba un 0.0607 calculado para una inclinacion que ya no es
// la de la escena. Ajustar por los dos ejes con min() da una escala estable.
static float FitScale(GameObject model, float targetWidth, float targetHeight)
    {
        Bounds wb = WorldBounds(model);
        float w = targetWidth * ModelFillFactor;
        float h = targetHeight * ModelHeightOverflow;
        if (wb.size.x < 1e-6f || wb.size.y < 1e-6f || w <= 0f || h <= 0f) return 1f;
        return Mathf.Min(w / wb.size.x, h / wb.size.y);
    }

    static void CenterOnMarker(Transform model, Transform parent, float bottomZ)
    {
        Bounds wb = WorldBounds(model.gameObject);
        if (wb.size == Vector3.zero) return;

        Vector3 c = parent.InverseTransformPoint(wb.center);
        model.position -= parent.TransformVector(new Vector3(c.x, c.y, 0f));

        wb = WorldBounds(model.gameObject);
        Vector3 minPt = parent.InverseTransformPoint(new Vector3(wb.center.x, wb.center.y, wb.min.z));
        model.position += parent.TransformVector(new Vector3(0f, 0f, bottomZ - minPt.z));
    }

    static Transform FindAssembly(Transform model, string name)
    {
        Transform t = model.Find("MotherboardRoot/" + name);
        if (t == null) t = model.Find(name);
        if (t == null) t = FindRecursive(model, name);
        if (t == null)
            Debug.LogWarning("[SceneBuilder] No se encontro la ensamblaje " + name + ".");
        return t;
    }

    static Transform FindRecursive(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.name == name) return t;
        return null;
    }

    // Banner inferior tipo carrusel: titulo + 3 pestanas (Descripcion / Ensamblaje /
    // Desensamblaje, tambien por swipe) + cuerpo con scroll + Anterior/Siguiente.
    // Reutiliza ComponentInfoUI (toda su logica ya existe); aqui solo se construye
    // el lienzo y se cablean sus referencias. Es raiz (no hijo del ImageTarget) y
    // CleanStaleRoots lo regenera en cada rebuild.
    static ComponentInfoUI BuildInfoBanner(InfoLabel3D label)
    {
        GameObject canvasGo = new GameObject("AR_BannerCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2400);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("BannerPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0f);
        prt.anchorMax = new Vector2(1f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.offsetMin = new Vector2(16f, 24f);
        prt.offsetMax = new Vector2(-16f, 664f);
        UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.93f);

        TextMeshProUGUI title = MakeBannerText(panel, "BannerTitle", 40, true,
            new Vector2(24f, -92f), new Vector2(-24f, -12f));

        GameObject tabRow = new GameObject("BannerTabs");
        tabRow.transform.SetParent(panel.transform, false);
        RectTransform trt = tabRow.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.offsetMin = new Vector2(16f, -192f);
        trt.offsetMax = new Vector2(-16f, -100f);
        HorizontalLayoutGroup tabs = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabs.spacing = 12f;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        tabs.childForceExpandWidth = true;
        tabs.childForceExpandHeight = true;
        Button tabInfo = MakeTabButton(tabRow, "TabDescripcion", "Descripción");
        Button tabAsm = MakeTabButton(tabRow, "TabEnsamblaje", "Ensamblaje");
        Button tabDis = MakeTabButton(tabRow, "TabDesensamblaje", "Desensamblaje");

        GameObject scrollGo = new GameObject("BannerScroll");
        scrollGo.transform.SetParent(panel.transform, false);
        RectTransform srt = scrollGo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.offsetMin = new Vector2(16f, -520f);
        srt.offsetMax = new Vector2(-16f, -200f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        GameObject viewport = new GameObject("BannerViewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vrt = viewport.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vrt;

        GameObject content = new GameObject("BannerContent");
        content.transform.SetParent(viewport.transform, false);
        RectTransform crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(8f, 0f);
        crt.offsetMax = new Vector2(-8f, 0f);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = crt;

        TextMeshProUGUI body = content.AddComponent<TextMeshProUGUI>();
        body.fontSize = 34f;
        body.color = new Color(0.93f, 0.95f, 0.97f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.richText = true;

        GameObject navRow = new GameObject("BannerNav");
        navRow.transform.SetParent(panel.transform, false);
        RectTransform nrt = navRow.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0f);
        nrt.anchorMax = new Vector2(1f, 0f);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.offsetMin = new Vector2(16f, 12f);
        nrt.offsetMax = new Vector2(-16f, 100f);
        HorizontalLayoutGroup nav = navRow.AddComponent<HorizontalLayoutGroup>();
        nav.spacing = 12f;
        nav.childControlWidth = true;
        nav.childControlHeight = true;
        nav.childForceExpandWidth = true;
        nav.childForceExpandHeight = true;
        Button prev = MakeTabButton(navRow, "StepPrev", "◀ Anterior");
        GameObject counterGo = new GameObject("StepCounter");
        counterGo.transform.SetParent(navRow.transform, false);
        TextMeshProUGUI counter = counterGo.AddComponent<TextMeshProUGUI>();
        counter.fontSize = 32f;
        counter.color = Color.white;
        counter.alignment = TextAlignmentOptions.Center;
        counter.text = "";
        Button next = MakeTabButton(navRow, "StepNext", "Siguiente ▶");

        ComponentInfoUI ui = panel.AddComponent<ComponentInfoUI>();
        ui.titleText = title;
        ui.bodyText = body;
        ui.stepText = null;
        ui.stepCounterText = counter;
        ui.infoTabButton = tabInfo;
        ui.assembleTabButton = tabAsm;
        ui.disassembleTabButton = tabDis;
        ui.nextButton = next;
        ui.prevButton = prev;
        ui.stepsArea = navRow;
        ui.infoArea = null;
        ui.disassembleHintArea = null;
        ui.mirror = label;

        tabInfo.onClick.AddListener(() => ui.SelectTab(0));
        tabAsm.onClick.AddListener(() => ui.SelectTab(1));
        tabDis.onClick.AddListener(() => ui.SelectTab(2));
        next.onClick.AddListener(() => ui.NextStep());
        prev.onClick.AddListener(() => ui.PrevStep());

        BannerSwipe swipe = viewport.AddComponent<BannerSwipe>();
        swipe.target = ui;

        ui.ShowMainMenu();
        Debug.Log("[SceneBuilder] Banner inferior listo (titulo + 3 pestanas + swipe).");
        return ui;
    }

    static TextMeshProUGUI MakeBannerText(GameObject parent, string name, float size, bool bold, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = new Color(0.93f, 0.95f, 0.97f);
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.richText = true;
        return t;
    }

    static Button MakeTabButton(GameObject parent, string name, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        UnityEngine.UI.Image bg = go.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.16f, 0.18f, 0.22f);
        Button b = go.AddComponent<Button>();
        GameObject tg = new GameObject(name + "_Label");
        tg.transform.SetParent(go.transform, false);
        RectTransform rt = tg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        TextMeshProUGUI t = tg.AddComponent<TextMeshProUGUI>();
        t.fontSize = 32f;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.text = label;
        return b;
    }

    // Panel 3D tipo carrusel sobre la placa: titulo + 3 pestanas (Descripcion /
    // Ensamblaje / Desensamblaje, tambien por swipe) + cuerpo con scroll +
    // Anterior/Siguiente. Vive en World-Space colgado del unificado (mismo patron
    // que la vitrina: ancla en espacio de malla, sin Renderer.bounds), mira a la
    // camara (billboard) y SOLO es visible al seleccionar un componente.
    // Reutiliza ComponentInfoUI, MakeBannerText/MakeTabButton y BannerSwipe.
    static ComponentInfoUI BuildInfoPanel3D(Camera cam, Transform parent, GameObject model)
    {
        Transform unifiedT = FindRecursive(model.transform, "Motherboard_Unified");
        MeshFilter unifiedMF = unifiedT != null ? unifiedT.GetComponent<MeshFilter>() : null;
        Mesh boardMesh = unifiedMF != null ? unifiedMF.sharedMesh : null;
        Vector3 bbCenter = Vector3.zero;
        Vector3 bbSize = new Vector3(0.02f, 0.02f, 0.004f);
        if (boardMesh != null)
        {
            bbCenter = boardMesh.bounds.center;
            bbSize = boardMesh.bounds.size;
        }
        float panelW = Mathf.Max(bbSize.x, Mathf.Max(bbSize.y, bbSize.z)) * 1.15f;
        float panelH = panelW * 0.66f;
        float thinHalf = Mathf.Min(bbSize.x, Mathf.Min(bbSize.y, bbSize.z)) * 0.5f;
        // Normal de la placa en espacio de malla = eje delgado (la placa esta plana).
        Vector3 n = bbSize.x <= bbSize.y && bbSize.x <= bbSize.z ? Vector3.right
            : (bbSize.y <= bbSize.z ? Vector3.up : Vector3.forward);

        GameObject root = new GameObject("AR_InfoPanel3D");
        root.transform.SetParent(unifiedT != null ? unifiedT : model.transform, false);
        RectTransform rrt = root.AddComponent<RectTransform>();
        rrt.anchorMin = new Vector2(0.5f, 0.5f);
        rrt.anchorMax = new Vector2(0.5f, 0.5f);
        rrt.pivot = new Vector2(0.5f, 0f);
        rrt.sizeDelta = new Vector2(panelW, panelH);
        rrt.localPosition = bbCenter + n * (thinHalf + panelH * 0.6f);
        rrt.localRotation = Quaternion.identity;
        rrt.localScale = Vector3.one;
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        CanvasScaler cscale = root.AddComponent<CanvasScaler>();
        cscale.dynamicPixelsPerUnit = 8f;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<PanelBillboard>();
        UnityEngine.UI.Image bg = root.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.93f);

        float titleH = panelH * 0.14f;
        float tabsH = panelH * 0.15f;
        float navH = panelH * 0.15f;
        float topY = panelH;

        GameObject titleGo = new GameObject("PanelTitle");
        titleGo.transform.SetParent(root.transform, false);
        RectTransform titRt = titleGo.AddComponent<RectTransform>();
        titRt.anchorMin = new Vector2(0f, 0f);
        titRt.anchorMax = new Vector2(1f, 0f);
        titRt.pivot = new Vector2(0.5f, 0f);
        titRt.offsetMin = new Vector2(panelW * 0.03f, topY - titleH);
        titRt.offsetMax = new Vector2(-panelW * 0.03f, topY);
        TextMeshProUGUI title = titleGo.AddComponent<TextMeshProUGUI>();
        title.fontSize = panelW * 0.07f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(0.93f, 0.95f, 0.97f);
        title.alignment = TextAlignmentOptions.Center;
        title.textWrappingMode = TextWrappingModes.Normal;
        title.richText = true;

        GameObject tabRow = new GameObject("PanelTabs");
        tabRow.transform.SetParent(root.transform, false);
        RectTransform trt = tabRow.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 0f);
        trt.pivot = new Vector2(0.5f, 0f);
        trt.offsetMin = new Vector2(panelW * 0.02f, topY - titleH - tabsH);
        trt.offsetMax = new Vector2(-panelW * 0.02f, topY - titleH);
        HorizontalLayoutGroup tabs = tabRow.AddComponent<HorizontalLayoutGroup>();
        tabs.spacing = panelW * 0.012f;
        tabs.childControlWidth = true;
        tabs.childControlHeight = true;
        tabs.childForceExpandWidth = true;
        tabs.childForceExpandHeight = true;
        Button tabInfo = MakeTabButton(tabRow, "TabDescripcion", "Descripción");
        Button tabAsm = MakeTabButton(tabRow, "TabEnsamblaje", "Ensamblaje");
        Button tabDis = MakeTabButton(tabRow, "TabDesensamblaje", "Desensamblaje");
        foreach (Button b in new[] { tabInfo, tabAsm, tabDis })
            b.GetComponentInChildren<TextMeshProUGUI>().fontSize = panelW * 0.05f;

        GameObject scrollGo = new GameObject("PanelScroll");
        scrollGo.transform.SetParent(root.transform, false);
        RectTransform srt = scrollGo.AddComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.offsetMin = new Vector2(panelW * 0.02f, navH + panelW * 0.015f);
        srt.offsetMax = new Vector2(-panelW * 0.02f, topY - titleH - tabsH);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = panelW * 0.04f;

        GameObject viewport = new GameObject("PanelViewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vrt = viewport.AddComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero;
        vrt.anchorMax = Vector2.one;
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = vrt;

        GameObject content = new GameObject("PanelContent");
        content.transform.SetParent(viewport.transform, false);
        RectTransform crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(0.5f, 1f);
        crt.offsetMin = new Vector2(panelW * 0.01f, 0f);
        crt.offsetMax = new Vector2(-panelW * 0.01f, 0f);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = crt;

        TextMeshProUGUI body = content.AddComponent<TextMeshProUGUI>();
        body.fontSize = panelW * 0.055f;
        body.color = new Color(0.93f, 0.95f, 0.97f);
        body.alignment = TextAlignmentOptions.TopLeft;
        body.textWrappingMode = TextWrappingModes.Normal;
        body.richText = true;

        GameObject navRow = new GameObject("PanelNav");
        navRow.transform.SetParent(root.transform, false);
        RectTransform nrt = navRow.AddComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0f);
        nrt.anchorMax = new Vector2(1f, 0f);
        nrt.pivot = new Vector2(0.5f, 0f);
        nrt.offsetMin = new Vector2(panelW * 0.02f, panelW * 0.012f);
        nrt.offsetMax = new Vector2(-panelW * 0.02f, navH);
        HorizontalLayoutGroup nav = navRow.AddComponent<HorizontalLayoutGroup>();
        nav.spacing = panelW * 0.012f;
        nav.childControlWidth = true;
        nav.childControlHeight = true;
        nav.childForceExpandWidth = true;
        nav.childForceExpandHeight = true;
        Button prev = MakeTabButton(navRow, "StepPrev", "◀ Ant.");
        GameObject counterGo = new GameObject("StepCounter");
        counterGo.transform.SetParent(navRow.transform, false);
        TextMeshProUGUI counter = counterGo.AddComponent<TextMeshProUGUI>();
        counter.fontSize = panelW * 0.05f;
        counter.color = Color.white;
        counter.alignment = TextAlignmentOptions.Center;
        counter.text = "";
        Button next = MakeTabButton(navRow, "StepNext", "Sig. ▶");
        foreach (Button b in new[] { prev, next })
            b.GetComponentInChildren<TextMeshProUGUI>().fontSize = panelW * 0.05f;

        ComponentInfoUI ui = root.AddComponent<ComponentInfoUI>();
        ui.titleText = title;
        ui.bodyText = body;
        ui.stepText = null;
        ui.stepCounterText = counter;
        ui.infoTabButton = tabInfo;
        ui.assembleTabButton = tabAsm;
        ui.disassembleTabButton = tabDis;
        ui.nextButton = next;
        ui.prevButton = prev;
        ui.stepsArea = navRow;
        ui.infoArea = null;
        ui.disassembleHintArea = null;
        ui.mirror = null;

        tabInfo.onClick.AddListener(() => ui.SelectTab(0));
        tabAsm.onClick.AddListener(() => ui.SelectTab(1));
        tabDis.onClick.AddListener(() => ui.SelectTab(2));
        next.onClick.AddListener(() => ui.NextStep());
        prev.onClick.AddListener(() => ui.PrevStep());

        BannerSwipe swipe = viewport.AddComponent<BannerSwipe>();
        swipe.target = ui;

        ui.ShowMainMenu();
        Debug.Log("[SceneBuilder] Panel 3D listo (ancho=" + panelW.ToString("F4") + ").");
        return ui;
    }

    static InfoLabel3D BuildLabel(Camera cam, Transform parent, GameObject model)
    {
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0)
            rs = parent.GetComponentsInChildren<Renderer>();

        Bounds wb = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            wb.Encapsulate(rs[i].bounds);

        float panelW = Mathf.Max(wb.size.x * 0.9f, 0.08f);
        float panelH = panelW * 0.65f;
        float fontSize = panelW * 0.09f;

        GameObject go = new GameObject("InfoLabel3D", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        TextMeshPro tm = go.GetComponent<TextMeshPro>();
        tm.fontSize = fontSize;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = new Color(0.93f, 0.95f, 0.97f);
        tm.textWrappingMode = TextWrappingModes.Normal;

        float gap = Mathf.Max(wb.size.y * 0.004f, 0.002f);
        Vector3 top = parent.InverseTransformPoint(new Vector3(wb.center.x, wb.max.y, wb.center.z));
        Vector3 front = parent.InverseTransformPoint(new Vector3(wb.center.x, wb.center.y, wb.max.z));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(panelW, panelH);
        rt.anchoredPosition = new Vector2(top.x, top.y + gap);
        Vector3 lp = rt.localPosition;
        rt.localPosition = new Vector3(lp.x, lp.y, front.z + gap * 0.25f);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        InfoLabel3D label = go.AddComponent<InfoLabel3D>();
        label.text = tm;
        label.ShowMainMenu();
        return label;
    }

    static void BuildInteraction(Camera cam, GameObject model, ComponentInfoUI banner)
    {
        GameObject inter = new GameObject("Interaction");
        ComponentSelector sel = inter.AddComponent<ComponentSelector>();
        sel.arCamera = cam;
        sel.infoUI = banner;

        OrbitTouch orbit = model.GetComponent<OrbitTouch>();
        if (orbit == null) orbit = model.AddComponent<OrbitTouch>();
        // Factores relativos a la escala con la que se reconstruye el modelo. Antes se fijaban
        // limites absolutos (0.4 / 2.5) sobre una escala real de ~0.06, asi que el primer
        // pinching saltaba el modelo a 6.6x su tamano y ya no podia volver a encogerse.
        orbit.minScaleFactor = 0.5f;
        orbit.maxScaleFactor = 3f;
        orbit.rotateSpeed = 0.3f;
    }
}
