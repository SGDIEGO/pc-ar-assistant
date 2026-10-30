using UnityEngine;
using UnityEngine.EventSystems;

// Swipe horizontal sobre el banner para cambiar de seccion (Descripcion /
// Ensamblaje / Desensamblaje). Solo reacciona a arrastres predominantemente
// horizontales, asi no interfiere con el scroll vertical del texto.
public class BannerSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ComponentInfoUI target;
    public float minSwipePx = 60f;

    Vector2 _start;
    bool _tracking;
    ScreenOrientation _orientation;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _start = eventData.position;
        _orientation = ComponentInfoUI.GestureOrientation;
        _tracking = true;
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_tracking || target == null) return;
        _tracking = false;
        Vector2 d = ComponentInfoUI.ScreenDeltaToPortrait(eventData.position - _start, _orientation);
        if (Mathf.Abs(d.x) > minSwipePx && Mathf.Abs(d.x) > Mathf.Abs(d.y) * 1.5f)
            target.SelectTab(Mathf.Clamp(target.CurrentTab + (d.x < 0 ? 1 : -1), 0, 2));
    }
}
