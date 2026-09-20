using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;

public static class SceneBuilder
{
    const string ModelPath = "Assets/Models/PC_Components.fbx";
    const string imageTargetName = "ImageTarget";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const float ModelTargetSize = 0.11f;

    [MenuItem("Tools/AR/1. Importar recursos TMP", false, 1)]
    public static void ImportTMP()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return;
        TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
        AssetDatabase.Refresh();
        Debug.Log("[SceneBuilder] TMP Essential Resources importadas.");
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

    [MenuItem("Tools/AR/3. Configurar ImageTarget (dataset nuevo)", false, 3)]
    public static void ConfigureTarget()
    {
        GameObject target = GameObject.Find(imageTargetName);
        if (target == null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            target = GameObject.Find(imageTargetName);
        }
        if (target == null)
        {
            Debug.LogError("[SceneBuilder] No se encontró '" + imageTargetName + "'.");
            return;
        }

        const string trackable = "placa_madre_referencia_ccbysa";
        const string dataset = "Vuforia/Componentes.xml";
        const float width = 0.21f;
        const float height = 0.174158f;

        ImageTargetBehaviour itb = target.GetComponent<ImageTargetBehaviour>();
        bool changed = false;
        if (itb != null)
        {
            SerializedObject so = new SerializedObject(itb);
            so.FindProperty("mTrackableName").stringValue = trackable;
            so.FindProperty("mDataSetPath").stringValue = dataset;
            so.FindProperty("mImageTargetType").intValue = 0;
            so.FindProperty("mWidth").floatValue = width;
            so.FindProperty("mHeight").floatValue = height;
            so.FindProperty("mAspectRatio").floatValue = width / height;
            changed = so.ApplyModifiedPropertiesWithoutUndo();
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

    [MenuItem("Tools/AR/2. Reconstruir escena AR", false, 2)]
    public static void RebuildScene()
    {
        ImportTMP();

        GameObject target = GameObject.Find(imageTargetName);
        if (target == null)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            target = GameObject.Find(imageTargetName);
        }
        if (target == null)
        {
            Debug.LogError("[SceneBuilder] No se encontrÃ³ '" + imageTargetName + "'. No se puede reconstruir.");
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

        InfoLabel3D label = BuildLabel(cam, target.transform, model);
        BuildInteraction(cam, model, label);

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
            model.transform.localRotation = align * Quaternion.Euler(38f, 0f, 0f);
        }

        float s = FitScale(model, ModelTargetSize);
        model.transform.localScale = new Vector3(s, s, s);
        Debug.Log("[SceneBuilder] Ancho horizontal medido=" + (ModelTargetSize / s).ToString("0.000")
            + " m -> escala aplicada=" + s.ToString("0.00"));

        CenterOnMarker(model.transform, parent.transform, 0.02f);

        AssignMaterials(model);
        AddColliders(model);

        Transform cpu = model.transform.Find("MotherboardRoot/CPU_Assembly");
        Transform ram = model.transform.Find("MotherboardRoot/RAM_Assembly");
        Transform ssd = model.transform.Find("MotherboardRoot/SSD_Assembly");
        if (cpu == null) cpu = FindRecursive(model.transform, "CPU_Assembly");
        if (ram == null) ram = FindRecursive(model.transform, "RAM_Assembly");
        if (ssd == null) ssd = FindRecursive(model.transform, "SSD_Assembly");

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
        Shader std = Shader.Find("Standard");
        var palette = BuildPalette();
        foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.sharedMaterials == null || r.sharedMaterials.Length == 0) continue;
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material src = mats[i];
                if (src == null) continue;
                if (!palette.TryGetValue(src.name, out PaletteColor p)) continue;
                string matName = "PC_" + src.name;
                Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + matName + ".mat");
                if (mat == null)
                {
                    mat = new Material(std) { name = matName };
                    mat.SetColor("_Color", p.color);
                    mat.SetFloat("_Metallic", p.metallic);
                    mat.SetFloat("_Glossiness", p.smoothness);
                    mat.enableInstancing = true;
                    AssetDatabase.CreateAsset(mat, "Assets/Materials/" + matName + ".mat");
                }
                mats[i] = mat;
                changed = true;
            }
            if (changed) r.sharedMaterials = mats;
        }
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

    static float FitScale(GameObject model, float targetWidth)
    {
        Bounds wb = WorldBounds(model);
        float horizontal = Mathf.Max(wb.size.x, wb.size.y);
        if (horizontal < 1e-6f || targetWidth <= 0f) return 1f;
        return targetWidth / horizontal;
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

    static Transform FindRecursive(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
            if (t.name == name) return t;
        return null;
    }

    static InfoLabel3D BuildLabel(Camera cam, Transform parent, GameObject model)
    {
        Renderer[] rs = model.GetComponentsInChildren<Renderer>();
        if (rs == null || rs.Length == 0)
            rs = parent.GetComponentsInChildren<Renderer>();

        Bounds wb = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            wb.Encapsulate(rs[i].bounds);

        float panelW = Mathf.Max(wb.size.x * 2.4f, 0.22f);
        float panelH = panelW * 0.65f;
        float fontSize = panelW * 0.24f;

        GameObject go = new GameObject("InfoLabel3D", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        TextMeshPro tm = go.GetComponent<TextMeshPro>();
        tm.fontSize = fontSize;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = new Color(0.93f, 0.95f, 0.97f);
        tm.textWrappingMode = TextWrappingModes.Normal;

        float gap = Mathf.Max(wb.size.y * 0.06f, 0.01f);
        Vector3 top = parent.InverseTransformPoint(new Vector3(wb.center.x, wb.max.y, wb.center.z));
        Vector3 front = parent.InverseTransformPoint(new Vector3(wb.center.x, wb.center.y, wb.max.z));

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(panelW, panelH);
        rt.anchoredPosition = new Vector2(top.x, top.y + gap);
        Vector3 lp = rt.localPosition;
        rt.localPosition = new Vector3(lp.x, lp.y, front.z + gap);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        InfoLabel3D label = go.AddComponent<InfoLabel3D>();
        label.text = tm;
        label.ShowMainMenu();
        return label;
    }

    static void BuildInteraction(Camera cam, GameObject model, InfoLabel3D label)
    {
        GameObject inter = new GameObject("Interaction");
        ComponentSelector sel = inter.AddComponent<ComponentSelector>();
        sel.arCamera = cam;
        sel.infoUI = label;

        OrbitTouch orbit = model.GetComponent<OrbitTouch>();
        if (orbit == null) orbit = model.AddComponent<OrbitTouch>();
        orbit.minScale = 0.4f;
        orbit.maxScale = 2.5f;
        orbit.rotateSpeed = 0.3f;
    }
}
