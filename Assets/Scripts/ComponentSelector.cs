using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ComponentSelector : MonoBehaviour
{
    public Camera arCamera;
    public IComponentInfoReceiver infoUI;
    public LayerMask selectionMask = ~0;
    public Transform draggableModel;
    public float dragThreshold = 12f;

    const float TapMaxMovement = 30f;

    SelectionOutline _outline;
    Vector2 _touchStart;
    Vector2 _lastDragScreen;
    bool _touching;
    bool _dragging;
    bool _dragOnModel;

    void Start()
    {
        if (arCamera == null)
            arCamera = GetComponent<Camera>();
        if (infoUI == null)
        {
            infoUI = Object.FindFirstObjectByType<InfoLabel3D>();
            if (infoUI == null)
                infoUI = Object.FindFirstObjectByType<ComponentInfoUI>();
        }
        if (draggableModel == null)
        {
            GameObject model = GameObject.Find("PC_Model");
            if (model != null) draggableModel = model.transform;
        }
    }

    void Update()
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null) return;

        var primary = touch.primaryTouch;
        if (primary.press.wasPressedThisFrame)
        {
            _touching = true;
            _touchStart = primary.position.ReadValue();
            _dragging = false;
            _dragOnModel = IsOverModel(_touchStart);
        }
        else if (_touching && primary.press.isPressed)
        {
            Vector2 pos = primary.position.ReadValue();

            int presses = 0;
            for (int i = 0; i < touch.touches.Count; i++)
                if (touch.touches[i].press.isPressed) presses++;
            if (presses >= 2)
            {
                _dragging = false;
                _dragOnModel = false;
            }
            else if (_dragOnModel && !_dragging && Vector2.Distance(pos, _touchStart) > dragThreshold)
            {
                _dragging = true;
                _lastDragScreen = pos;
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
            _touching = false;
            _dragging = false;
            _dragOnModel = false;
        }
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

        Ray ray = arCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, selectionMask))
        {
            ComponentInfo info = hit.collider.GetComponentInParent<ComponentInfo>();
            if (info != null)
            {
                infoUI.ShowComponent(info);
                ShowOutline(info);
                return;
            }
        }

        infoUI.ShowMainMenu();
        HideOutline();
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