using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitTouch : MonoBehaviour
{
    public float rotateSpeed = 0.35f;
    public float minScale = 0.25f;
    public float maxScale = 3f;

    Vector2 _prevPos0;
    float _prevPinchDist;
    bool _twoFingers;

    void Update()
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null || touch.touches.Count < 2) return;

        var t0 = touch.touches[0];
        var t1 = touch.touches[1];

        if (t0.press.isPressed && t1.press.isPressed)
        {
            if (!_twoFingers)
            {
                _twoFingers = true;
                _prevPos0 = t0.position.ReadValue();
                _prevPinchDist = Vector2.Distance(t0.position.ReadValue(), t1.position.ReadValue());
                return;
            }

            Vector2 pos0 = t0.position.ReadValue();
            Vector2 pos1 = t1.position.ReadValue();

            float pinchDist = Vector2.Distance(pos0, pos1);
            if (_prevPinchDist > 1e-3f)
            {
                float s = pinchDist / _prevPinchDist;
                Vector3 sc = transform.localScale * s;
                float cl = Mathf.Clamp(sc.x, minScale, maxScale);
                transform.localScale = new Vector3(cl, cl, cl);
            }

            transform.Rotate(0f, (pos0.x - _prevPos0.x) * rotateSpeed, 0f, Space.Self);

            _prevPos0 = pos0;
            _prevPinchDist = pinchDist;
        }
        else
        {
            _twoFingers = false;
        }
    }
}