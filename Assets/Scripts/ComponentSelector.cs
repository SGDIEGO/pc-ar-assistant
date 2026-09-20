using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ComponentSelector : MonoBehaviour
{
    public Camera arCamera;
    public IComponentInfoReceiver infoUI;
    public LayerMask selectionMask = ~0;

    const float TapMaxMovement = 30f;

    Vector2 _touchStart;
    bool _touching;

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
        }
        else if (_touching && primary.press.wasReleasedThisFrame)
        {
            Vector2 end = primary.position.ReadValue();
            float moved = Vector2.Distance(_touchStart, end);
            if (moved <= TapMaxMovement)
                HandleTap(end);
            _touching = false;
        }
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
                return;
            }
        }

        infoUI.ShowMainMenu();
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