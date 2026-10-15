using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SelectionOutline : MonoBehaviour
{
    [Tooltip("Ancho del borde como fraccion del tamano del mesh")]
    public float outlineWidth = 0.03f;
    public Color outlineColor = new Color(0f, 0.92f, 1f, 1f);

    [Tooltip("Material del contorno. Si se deja vacio se carga de Resources/SelectionOutline.")]
    public Material outlineMaterial;

    // El material vive en Resources para que Unity lo empaquete en el build sin depender de
    // Shader.Find, que en un player solo encuentra shaders incluidos explicitamente. Antes este
    // script resolvia "Universal Render Pipeline/Unlit", shader exclusivo de SRP que bajo el
    // Built-in Render Pipeline (el que usa este proyecto) se dibuja en magenta y ademas puede
    // devolver null, dejando el MeshRenderer sin material y la pieza invisible.
    const string ResourcesMaterialPath = "SelectionOutline";

    GameObject _outlineRoot;
    readonly List<GameObject> _pieceObjects = new List<GameObject>();
    readonly List<Mesh> _pieceMeshes = new List<Mesh>();
    Material _outlineMaterialInstance;
    Transform _currentTarget;

    void Awake()
    {
        ResolveMaterial();
    }

    Material ResolveMaterial()
    {
        if (_outlineMaterialInstance != null) return _outlineMaterialInstance;

        Material source = outlineMaterial != null ? outlineMaterial : null;
        if (source == null) source = Resources.Load<Material>(ResourcesMaterialPath);

        if (source == null)
        {
            Debug.LogError("[SelectionOutline] No se encontro el material del contorno. "
                + "Ejecuta Tools/AR/6. Crear material del contorno en el editor.");
            return null;
        }

        // Instancia propia para poder tintar sin mutar el asset compartido en Resources.
        _outlineMaterialInstance = new Material(source);
        _outlineMaterialInstance.name = source.name + " (Instance)";
        _outlineMaterialInstance.color = outlineColor;
        if (_outlineMaterialInstance.HasProperty("_ZTest"))
            _outlineMaterialInstance.SetInt("_ZTest", (int)CompareFunction.LessEqual);
        if (_outlineMaterialInstance.HasProperty("_Cull"))
            _outlineMaterialInstance.SetInt("_Cull", (int)CullMode.Off);
        return _outlineMaterialInstance;
    }

    public void Show(ComponentInfo target)
    {
        Show(target != null ? target.transform : null);
    }

    public void Show(Transform target)
    {
        if (target == null)
        {
            Hide();
            return;
        }
        if (_currentTarget == target && _outlineRoot != null && _outlineRoot.activeSelf) return;

        Hide();
        _currentTarget = target;

        Material mat = ResolveMaterial();
        if (mat == null) return;

        // Se resaltaba solo el primer MeshFilter del assembly, asi que en una ensamblaje con
        // varias piezas (CPU + disipador, RAM + modulo, ...) el borde solo cubria una parte.
        // Se recogen ANTES de colgar el contorno del target para no incluirlo a el mismo.
        MeshFilter[] filters = GetSourceFilters(target);
        if (filters == null || filters.Length == 0) return;

        EnsureRoot(target);

        int built = 0;
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter filter = filters[i];
            if (filter == null) continue;
            Mesh src = filter.sharedMesh;
            if (src == null || src.vertexCount == 0) continue;

            Mesh inflated = BuildInflatedMesh(src);
            if (inflated == null) continue;

            GameObject piece = GetPieceObject(built);
            piece.transform.localPosition = Vector3.zero;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = Vector3.one;
            piece.GetComponent<MeshFilter>().sharedMesh = inflated;
            MeshRenderer pieceRenderer = piece.GetComponent<MeshRenderer>();
            if (pieceRenderer != null) pieceRenderer.sharedMaterial = mat;
            SetPieceMesh(built, inflated);
            piece.SetActive(true);
            built++;
        }

        // Sobran piezas de la seleccion anterior que tenia mas mallas.
        for (int i = built; i < _pieceObjects.Count; i++)
        {
            if (_pieceObjects[i] == null) continue;
            _pieceObjects[i].SetActive(false);
            MeshFilter mf = _pieceObjects[i].GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = null;
            SetPieceMesh(i, null);
        }

        if (built == 0)
        {
            Debug.LogWarning("[SelectionOutline] No se pudo construir el contorno de '" + target.name + "'.");
            Hide();
            return;
        }

        _outlineRoot.SetActive(true);
    }

    // El contorno se cuelga del propio target, asi que sus propias mallas quedarian dentro del
    // recorrido y el contorno se incluiria a si mismo en la siguiente pulsacion.
    MeshFilter[] GetSourceFilters(Transform target)
    {
        MeshFilter[] all = target.GetComponentsInChildren<MeshFilter>(false);
        var result = new List<MeshFilter>(all.Length);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            if (_outlineRoot != null && all[i].transform.IsChildOf(_outlineRoot.transform)) continue;
            result.Add(all[i]);
        }
        return result.ToArray();
    }

    void EnsureRoot(Transform target)
    {
        if (_outlineRoot == null) _outlineRoot = new GameObject("SelectionOutline");
        if (_outlineRoot.hideFlags == HideFlags.None) _outlineRoot.hideFlags = HideFlags.DontSave;
        _outlineRoot.transform.SetParent(target, false);
    }

    GameObject GetPieceObject(int index)
    {
        while (_pieceObjects.Count <= index)
        {
            GameObject piece = new GameObject("OutlinePiece_" + index);
            piece.hideFlags = HideFlags.DontSave;
            piece.transform.SetParent(_outlineRoot.transform, false);
            piece.AddComponent<MeshFilter>();
            MeshRenderer mr = piece.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _pieceObjects.Add(piece);
            _pieceMeshes.Add(null);
        }
        return _pieceObjects[index];
    }

    void SetPieceMesh(int index, Mesh mesh)
    {
        while (_pieceMeshes.Count <= index) _pieceMeshes.Add(null);
        if (_pieceMeshes[index] != null && _pieceMeshes[index] != mesh)
            Destroy(_pieceMeshes[index]);
        _pieceMeshes[index] = mesh;
    }

    Mesh BuildInflatedMesh(Mesh src)
    {
        Vector3[] verts;
        Vector3[] normals;
        try
        {
            verts = src.vertices;
            normals = src.normals;
        }
        catch (UnityException e)
        {
            Debug.LogError("[SelectionOutline] No se pudo leer la malla '" + src.name
                + "'. Ejecuta Tools/AR/5. Corregir importacion del modelo para activar Read/Write. "
                + e.Message);
            return null;
        }

        if (verts == null || verts.Length == 0) return null;

        Mesh mesh = new Mesh { name = "Outline_" + src.name, hideFlags = HideFlags.HideAndDontSave };
        Vector3[] outVerts = new Vector3[verts.Length];
        float size = src.bounds.size.magnitude;
        float offset = size * outlineWidth;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = normals != null && normals.Length > i ? normals[i] : Vector3.one;
            outVerts[i] = verts[i] + n * offset;
        }
        mesh.vertices = outVerts;
        // tangents, uv y colors pueden venir null en mallas importadas y asignar null lanza excepcion.
        if (normals != null) mesh.normals = normals;
        if (src.tangents != null) mesh.tangents = src.tangents;
        if (src.uv != null) mesh.uv = src.uv;
        if (src.colors != null) mesh.colors = src.colors;
        mesh.triangles = src.triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    public void Hide()
    {
        _currentTarget = null;
        for (int i = 0; i < _pieceObjects.Count; i++)
        {
            if (_pieceObjects[i] != null)
            {
                _pieceObjects[i].SetActive(false);
                MeshFilter mf = _pieceObjects[i].GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = null;
            }
            SetPieceMesh(i, null);
        }
        if (_outlineRoot != null) _outlineRoot.SetActive(false);
    }

    void OnDestroy()
    {
        for (int i = 0; i < _pieceMeshes.Count; i++)
            if (_pieceMeshes[i] != null) Destroy(_pieceMeshes[i]);
        _pieceMeshes.Clear();
        if (_outlineMaterialInstance != null) Destroy(_outlineMaterialInstance);
        if (_outlineRoot != null) Destroy(_outlineRoot);
    }
}