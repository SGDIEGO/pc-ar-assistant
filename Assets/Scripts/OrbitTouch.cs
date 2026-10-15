using UnityEngine;
using UnityEngine.InputSystem;

public class OrbitTouch : MonoBehaviour
{
    public float rotateSpeed = 0.35f;

    [Tooltip("Factor minimo de zoom respecto a la escala con la que se reconstruyo el modelo.")]
    public float minScaleFactor = 0.5f;
    [Tooltip("Factor maximo de zoom respecto a la escala con la que se reconstruyo el modelo.")]
    public float maxScaleFactor = 3f;

    Vector2 _prevPos0;
    float _prevPinchDist;
    bool _twoFingers;

    // Escala tal y como la dejo SceneBuilder. Los limites se aplican como factores sobre esta,
    // no como valores absolutos: el modelo se reconstruye con un localScale de ~0.06 y un clamp
    // absoluto (0.4 / 2.5) hacia que el primer pinching lo encogia 6.6x y luego hacia que crezca.
    float _baseScale;

    void Awake()
    {
        _baseScale = Mathf.Abs(transform.localScale.x);
        if (_baseScale < 1e-6f) _baseScale = 1f;
    }

    void OnDisable()
    {
        _twoFingers = false;
        _prevPinchDist = 0f;
    }

    void Update()
    {
        Touchscreen touch = Touchscreen.current;
        if (touch == null || touch.touches.Count < 2)
        {
            // Sin este reset el estado del pinching anterior se queda colgado y el proximo
            // gesto de dos dedos se compara contra una distancia de pinza vieja.
            _twoFingers = false;
            return;
        }

        var t0 = touch.touches[0];
        var t1 = touch.touches[1];

        if (!t0.press.isPressed || !t1.press.isPressed)
        {
            _twoFingers = false;
            return;
        }

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
        if (_prevPinchDist > 1e-3f && pinchDist > 1e-3f)
        {
            float s = pinchDist / _prevPinchDist;
            float min = _baseScale * Mathf.Max(0.01f, minScaleFactor);
            float max = _baseScale * Mathf.Max(minScaleFactor, maxScaleFactor);
            float cl = Mathf.Clamp(transform.localScale.x * s, min, max);
            transform.localScale = new Vector3(cl, cl, cl);
        }

        transform.Rotate(0f, (pos0.x - _prevPos0.x) * rotateSpeed, 0f, Space.Self);

        _prevPos0 = pos0;
        _prevPinchDist = pinchDist;
    }
}