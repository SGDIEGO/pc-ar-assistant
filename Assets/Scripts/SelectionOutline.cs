using UnityEngine;
using UnityEngine.Rendering;

public class SelectionOutline : MonoBehaviour
{
    [Tooltip("Ancho del borde como fraccion del tamano del mesh")]
    public float outlineWidth = 0.03f;
    public Color outlineColor = new Color(0f, 0.92f, 1f, 1f);

    const string OutlineShader = "Universal Render Pipeline/Unlit";
    const string FallbackShader = "Unlit/Color";

    GameObject _outlineGO;
    Material _outlineMaterial;
    Transform _currentTarget;

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
        if (_currentTarget == target && _outlineGO != null && _outlineGO.activeSelf) return;

        Hide();

        MeshFilter filter = target.GetComponentInChildren<MeshFilter>();
        MeshRenderer renderer = target.GetComponentInChildren<MeshRenderer>();
        if (filter == null || renderer == null) return;

        _currentTarget = target;

        if (_outlineGO == null)
        {
            _outlineGO = new GameObject("SelectionOutline");
            _outlineGO.AddComponent<MeshFilter>();
            MeshRenderer mr = _outlineGO.AddComponent<MeshRenderer>();
            Shader sh = Shader.Find(OutlineShader);
            if (sh == null) sh = Shader.Find(FallbackShader);
            _outlineMaterial = new Material(sh);
            _outlineMaterial.color = outlineColor;
            if (_outlineMaterial.HasProperty("_ZTest"))
                _outlineMaterial.SetInt("_ZTest", (int)CompareFunction.LessEqual);
            if (_outlineMaterial.HasProperty("_Cull"))
                _outlineMaterial.SetInt("_Cull", (int)CullMode.Off);
            mr.sharedMaterial = _outlineMaterial;
        }

        _outlineGO.transform.SetParent(target, false);
        _outlineGO.transform.localPosition = Vector3.zero;
        _outlineGO.transform.localRotation = Quaternion.identity;
        _outlineGO.transform.localScale = Vector3.one;

        Mesh src = filter.sharedMesh;
        Mesh outlineMesh = new Mesh();
        outlineMesh.name = src.name + "_Outline";
        Vector3[] verts = src.vertices;
        Vector3[] normals = src.normals;
        Vector3[] outVerts = new Vector3[verts.Length];
        float size = src.bounds.size.magnitude;
        float offset = size * outlineWidth;
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 n = normals != null && normals.Length > i ? normals[i] : Vector3.one;
            outVerts[i] = verts[i] + n * offset;
        }
        outlineMesh.vertices = outVerts;
        outlineMesh.normals = src.normals;
        outlineMesh.tangents = src.tangents;
        outlineMesh.uv = src.uv;
        outlineMesh.colors = src.colors;
        outlineMesh.triangles = src.triangles;
        outlineMesh.RecalculateBounds();

        _outlineGO.GetComponent<MeshFilter>().mesh = outlineMesh;
        _outlineGO.SetActive(true);
    }

    public void Hide()
    {
        _currentTarget = null;
        if (_outlineGO != null) _outlineGO.SetActive(false);
    }
}