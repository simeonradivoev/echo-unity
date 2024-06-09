using Tweens;
using UnityEngine;

namespace UnityEcho.UI
{
    public class AngleHideUI : MonoBehaviour
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private float _maxAngle;

        [SerializeField]
        private float _angleLeaway;

        [SerializeField]
        private float _maxFov;

        private Canvas _canvas;

        private CanvasGroup _canvasGroup;

        private bool _lastVisible;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvas = GetComponent<Canvas>();
            _canvasGroup.alpha = 0;
            _canvas.enabled = false;
        }

        private void Update()
        {
            var rotationAngle = Vector3.Angle(_camera.transform.forward, transform.forward);
            var viewAngle = Vector3.Angle(_camera.transform.forward, Vector3.Normalize(transform.position - _camera.transform.position));
            var rotationVisible = _lastVisible ? rotationAngle <= _maxAngle + _angleLeaway : rotationAngle <= _maxAngle;
            var viewVisible = _lastVisible ? viewAngle <= _maxFov + _angleLeaway : viewAngle <= _maxFov;
            var visible = rotationVisible && viewVisible;
            if (_lastVisible != visible)
            {
                _lastVisible = visible;
                _canvasGroup.gameObject.CancelTweens();
                _canvasGroup.gameObject.AddTween(
                    new FloatTween
                    {
                        to = visible ? 1 : 0,
                        from = _canvasGroup.alpha,
                        duration = 0.2f,
                        easeType = EaseType.QuadOut,
                        onStart = t => _canvas.enabled = true,
                        onEnd = t => _canvas.enabled = visible,
                        onUpdate = (t, v) => _canvasGroup.alpha = v
                    });
            }
        }
    }
}