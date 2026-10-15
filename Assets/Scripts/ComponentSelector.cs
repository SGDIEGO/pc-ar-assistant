using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ComponentSelector : MonoBehaviour
{
    public Camera arCamera;
    public IComponentInfoReceiver infoUI;
    public ComponentPreview3D preview;
    public LayerMask selectionMask = ~0;
    public Transform draggableModel;
    public float dragThreshold = 12f;

    [Tooltip("Segundos que la caja queda visible tras soltar el dedo.")]
    public float outlineHideDelay = 1.5f;

    const float TapMaxMovement = 30f;

    SelectionOutline _outline;
    Vector2 _touchStart;
    Vector2 _lastDragScreen;
    bool _touching;
    bool _dragging;
    bool _dragOnModel;
    float _hideOutlineAt = -1f;

    OrbitTouch _orbit;
    Transform _previewDrag;
    Vector2 _prevPreviewPos;
    Vector2 _prevPreviewMid;
    bool _previewTwoFingers;
    float _nextResolve = -1f;

    void Start()
    {
        ResolveRefs();
    }

    // El contenido del ImageTarget (PC_Model, PreviewShowcase, AR_InfoPanel3D) cuelga del
    // target de Vuforia, que lo mantiene DESACTIVADO hasta que detecta el marcador. Ademas
    // infoUI es de tipo interfaz y Unity no lo serializa en la escena, y GameObject.Find /
    // FindFirstObjectByType solo ven objetos ACTIVOS. Por eso en Start() se quedaban a null
    // y el toque abortaba en el "if (arCamera == null || infoUI == null) return;".
    // Se buscan tambien los inactivos y se reintenta hasta que Vuforia active el contenido.
    void ResolveRefs()
    {
        if (arCamera == null)
            arCamera = GetComponent<Camera>();

        if (infoUI == null)
        {
            InfoLabel3D[] labels = Object.FindObjectsByType<InfoLabel3D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (labels.Length > 0) infoUI = labels[0];
        }
        if (infoUI == null)
        {
            ComponentInfoUI[] uis = Object.FindObjectsByType<ComponentInfoUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (uis.Length > 0) infoUI = uis[0];
        }

        if (draggableModel == null)
        {
            // OrbitTouch lo anade SceneBuilder sobre PC_Model; sirve de ancla fiable.
            OrbitTouch[] orbits = Object.FindObjectsByType<OrbitTouch>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (orbits.Length > 0) draggableModel = orbits[0].transform;
        }

        if (preview == null)
        {
            ComponentPreview3D[] previews = Object.FindObjectsByType<ComponentPreview3D>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (previews.Length > 0) preview = previews[0];
        }

        if (_orbit == null && draggableModel != null)
            _orbit = draggableModel.GetComponent<OrbitTouch>();

        Log("refs: cam=" + Ok(arCamera) + " ui=" + OkInterface(infoUI)
            + " preview=" + Ok(preview) + " model=" + Ok(draggableModel));
    }

    static string Ok(Object o) { return o == null ? "NULL" : "ok"; }
    static string OkInterface(object o) { return o == null ? "NULL" : "ok"; }
    static void Log(string msg) { Debug.Log("[Selector] " + msg); }

    void Update()
    {
        // Reintentar resolver referencias hasta que Vuforia active el contenido.
        // Limitar a 1 intento por segundo para no saturar.
        if (arCamera == null || infoUI == null || preview == null || draggableModel == null)
        {
            if (Time.time >= _nextResolve)
            {
                _nextResolve = Time.time + 1f;
                ResolveRefs();
            }
        }

        Touchscreen touch = Touchscreen.current;
        if (touch == null) return;

        // Auto-oculta solo la caja tras soltar; el panel conserva la info.
        if (_hideOutlineAt > 0f && Time.time >= _hideOutlineAt)
        {
            _hideOutlineAt = -1f;
            HideOutline();
        }

        var primary = touch.primaryTouch;
        if (primary.press.wasPressedThisFrame)
        {
            _touching = true;
            _touchStart = primary.position.ReadValue();
            _dragging = false;
            _dragOnModel = IsOverModel(_touchStart);
            if (IsPointerOverUI(_touchStart)) _dragOnModel = false;
            _hideOutlineAt = -1f;
            _previewDrag = null;
            _previewTwoFingers = false;
            _prevPreviewPos = _touchStart;
            string previewLabel = PreviewLabelAt(_touchStart);
            if (previewLabel != null)
            {
                // El gesto empieza sobre la pieza flotante: la placa se queda quieta.
                _previewDrag = PreviewInstance(previewLabel);
                _dragOnModel = false;
                if (_orbit != null) _orbit.enabled = false;
                ShowComponentByLabel(previewLabel);
            }
            else
            {
                PressPreview(_touchStart);
            }
        }
        else if (_touching && primary.press.isPressed)
        {
            Vector2 pos = primary.position.ReadValue();

            int presses = 0;
            for (int i = 0; i < touch.touches.Count; i++)
                if (touch.touches[i].press.isPressed) presses++;
            if (_previewDrag != null)
            {
                // Gesto sobre la pieza flotante: 1 dedo rota, 2 dedos trasladan.
                if (presses >= 2 && touch.touches.Count >= 2)
                {
                    Vector2 mid = (touch.touches[0].position.ReadValue()
                        + touch.touches[1].position.ReadValue()) * 0.5f;
                    if (!_previewTwoFingers)
                    {
                        _previewTwoFingers = true;
                        _prevPreviewMid = mid;
                    }
                    else
                    {
                        TranslatePreviewTo(mid);
                        _prevPreviewMid = mid;
                    }
                }
                else
                {
                    if (_previewTwoFingers) _prevPreviewPos = pos;
                    _previewTwoFingers = false;
                    RotatePreviewTo(pos);
                    _prevPreviewPos = pos;
                }
            }
            else if (presses >= 2)
            {
                _dragging = false;
                _dragOnModel = false;
                _hideOutlineAt = -1f;
                HideOutline();
                if (preview != null) preview.HideAll();
            }
            else if (_dragOnModel && !_dragging && Vector2.Distance(pos, _touchStart) > dragThreshold)
            {
                _dragging = true;
                _lastDragScreen = pos;
                _hideOutlineAt = -1f;
                HideOutline();
                if (preview != null) preview.HideAll();
            }
            else if (_dragging)
            {
                DragModelTo(pos);
                _lastDragScreen = pos;
            }
        }
        else if (_touching && primary.press.wasReleasedThisFrame)
        {
            Vector2 end = primary.position.ReadValue();
            float moved = Vector2.Distance(_touchStart, end);
            if (!_dragging && moved <= TapMaxMovement)
                HandleTap(end);
            if (!_dragging)
                _hideOutlineAt = Time.time + outlineHideDelay;
            _touching = false;
            _dragging = false;
            _dragOnModel = false;
            _previewDrag = null;
            _previewTwoFingers = false;
            if (_orbit != null) _orbit.enabled = true;
        }
    }

    // Etiqueta del preview bajo el dedo ("CPU"/"RAM"/"SSD") o null.
    string PreviewLabelAt(Vector2 screenPos)
    {
        if (arCamera == null || preview == null) return null;
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, selectionMask);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            Transform t = hit.collider.transform;
            if (preview.cpuModel != null && preview.cpuModel.activeInHierarchy && t.IsChildOf(preview.cpuModel.transform))
                return "CPU";
            if (preview.ramModel != null && preview.ramModel.activeInHierarchy && t.IsChildOf(preview.ramModel.transform))
                return "RAM";
            if (preview.ssdModel != null && preview.ssdModel.activeInHierarchy && t.IsChildOf(preview.ssdModel.transform))
                return "SSD";
        }
        return null;
    }

    Transform PreviewInstance(string label)
    {
        if (preview == null) return null;
        if (label == "CPU") return preview.cpuModel != null ? preview.cpuModel.transform : null;
        if (label == "RAM") return preview.ramModel != null ? preview.ramModel.transform : null;
        if (label == "SSD") return preview.ssdModel != null ? preview.ssdModel.transform : null;
        return null;
    }

    ComponentInfo InfoForLabel(string label)
    {
        GameObject go = GameObject.Find(label + "_Assembly");
        return go != null ? go.GetComponent<ComponentInfo>() : null;
    }

    void ShowComponentByLabel(string label)
    {
        if (infoUI == null) return;
        ComponentInfo info = InfoForLabel(label);
        if (info != null)
        {
            infoUI.ShowComponent(info);
            ShowOutline(info);
        }
        if (preview != null) preview.Show(label);
    }

    void RotatePreviewTo(Vector2 screenPos)
    {
        if (_previewDrag == null || arCamera == null || preview == null) return;
        Vector2 d = screenPos - _prevPreviewPos;
        Vector3 boardNormalWorld = _previewDrag.parent.TransformDirection(preview.spinAxis).normalized;
        _previewDrag.Rotate(boardNormalWorld, d.x * preview.rotateDegreesPerPixel, Space.World);
        _previewDrag.Rotate(arCamera.transform.right, -d.y * preview.rotateDegreesPerPixel, Space.World);
    }

    void TranslatePreviewTo(Vector2 screenPos)
    {
        if (_previewDrag == null || arCamera == null || preview == null) return;
        Transform anchor = _previewDrag.parent;
        Vector3 boardNormalWorld = anchor.TransformDirection(preview.spinAxis).normalized;
        Plane plane = new Plane(boardNormalWorld, _previewDrag.position);
        if (PlaneRay(_previewDrag.position, plane, _prevPreviewMid, out Vector3 prevWorld)
            && PlaneRay(_previewDrag.position, plane, screenPos, out Vector3 newWorld))
        {
            Vector3 target = _previewDrag.position + (newWorld - prevWorld);
            Vector3 local = anchor.InverseTransformPoint(target);
            Vector3 axial = preview.spinAxis.normalized * Vector3.Dot(local, preview.spinAxis.normalized);
            Vector3 lateral = local - axial;
            if (lateral.magnitude > preview.maxRadius)
                lateral = lateral.normalized * preview.maxRadius;
            float a = Vector3.Dot(local, preview.spinAxis.normalized);
            a = Mathf.Clamp(a, preview.minAxial, preview.maxRadius);
            _previewDrag.position = anchor.TransformPoint(lateral + preview.spinAxis.normalized * a);
        }
    }

    // Vista previa al apoyar el dedo: la caja aparece de inmediato y se apagara
    // sola tras soltar (ver temporizador en Update). El panel conserva la info.
    void PressPreview(Vector2 screenPos)
    {
        Log("tap down at " + screenPos);
        if (IsPointerOverUI(screenPos)) { Log("tap -> over UI"); return; }
        if (arCamera == null || infoUI == null) { Log("tap -> missing refs (cam=" + Ok(arCamera) + " ui=" + OkInterface(infoUI) + ")"); return; }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, selectionMask))
        {
            ComponentInfo info = hit.collider.GetComponentInParent<ComponentInfo>();
            if (info != null)
            {
                Log("tap -> hit " + hit.collider.name + " info=" + info.buttonLabel);
                // Mostrar PRIMERO el modelo 3D, para que aparezca aunque lo siguiente falle.
                if (preview != null) { preview.Show(info.buttonLabel); Log("preview.Show(" + info.buttonLabel + ") ok"); }
                infoUI.ShowComponent(info);
                ShowOutline(info);
                return;
            }
            Log("tap -> hit " + hit.collider.name + " SIN ComponentInfo");
        }
        else
        {
            Log("tap -> NO HIT");
        }

        infoUI.ShowMainMenu();
        HideOutline();
        if (preview != null) preview.HideAll();
    }

    bool IsOverModel(Vector2 screenPos)
    {
        if (draggableModel == null || arCamera == null) return false;
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f, selectionMask)) return false;
        Transform t = hit.collider.transform;
        while (t != null)
        {
            if (t == draggableModel) return true;
            t = t.parent;
        }
        return false;
    }

    void DragModelTo(Vector2 screenPos)
    {
        if (draggableModel == null || arCamera == null) return;
        Vector3 modelPos = draggableModel.position;
        Plane plane = new Plane(arCamera.transform.forward, modelPos);

        if (PlaneRay(modelPos, plane, _lastDragScreen, out Vector3 prevWorld)
            && PlaneRay(modelPos, plane, screenPos, out Vector3 newWorld))
        {
            draggableModel.position += newWorld - prevWorld;
        }
    }

    bool PlaneRay(Vector3 planePoint, Plane plane, Vector2 screenPos, out Vector3 world)
    {
        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (plane.Raycast(ray, out float enter))
        {
            world = ray.GetPoint(enter);
            return true;
        }
        world = planePoint;
        return false;
    }

    void HandleTap(Vector2 screenPos)
    {
        if (IsPointerOverUI(screenPos)) return;
        if (arCamera == null || infoUI == null) return;

        string previewLabel = PreviewLabelAt(screenPos);
        if (previewLabel != null)
        {
            ShowComponentByLabel(previewLabel);
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, selectionMask))
        {
            ComponentInfo info = hit.collider.GetComponentInParent<ComponentInfo>();
            if (info != null)
            {
                // Mostrar PRIMERO el modelo 3D.
                if (preview != null) preview.Show(info.buttonLabel);
                infoUI.ShowComponent(info);
                ShowOutline(info);
                return;
            }
        }

        infoUI.ShowMainMenu();
        HideOutline();
        if (preview != null) preview.HideAll();
    }

    void ShowOutline(ComponentInfo info)
    {
        if (_outline == null)
        {
            GameObject go = new GameObject("SelectionOutline");
            go.hideFlags = HideFlags.DontSave;
            _outline = go.AddComponent<SelectionOutline>();
        }
        _outline.Show(info);
    }

    void HideOutline()
    {
        if (_outline != null) _outline.Hide();
    }

    bool IsPointerOverUI(Vector2 screenPos)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        if (es.IsPointerOverGameObject()) return true;

        Touchscreen touch = Touchscreen.current;
        if (touch == null) return false;
        for (int i = 0; i < touch.touches.Count; i++)
        {
            var t = touch.touches[i];
            if (!t.press.isPressed) continue;
            int id = t.touchId.ReadValue();
            if (id >= 0 && es.IsPointerOverGameObject(id)) return true;
        }

        return false;
    }
}