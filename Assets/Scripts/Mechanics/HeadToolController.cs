using UnityEngine;
using UnityEngine.Events;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Controls the flashlight on the side of the head.
    /// </summary>
    public class HeadToolController : MonoBehaviour
    {
        [SerializeField]
        private float _buttonPressDistance;

        [SerializeField]
        private float _buttonPressAngle = 45f;

        [SerializeField]
        private float _buttonReleaseDistance;

        [SerializeField]
        private float _buttonPressRadius;

        [SerializeField]
        private float _buttonReleaseRadius;

        [SerializeField]
        private float _activateCooldown;

        [SerializeField]
        private UnityEvent _onToolActivate;

        [SerializeField]
        private UnityEvent _onToolDeActivate;

        private bool _active;

        private bool _insideButton;

        private float _lastActivate;

        private void Start()
        {
            HandIKController.OnMove += IKControllerOnOnMove;
        }

        private void OnDestroy()
        {
            HandIKController.OnMove -= IKControllerOnOnMove;
        }

        private void IKControllerOnOnMove(InputDevice device, Vector3 position, Quaternion rotation, float extension)
        {
            var plane = new Plane(transform.forward, transform.position);
            var fingerForward = rotation * Vector3.forward;
            var angle = Vector3.Angle(fingerForward, transform.up);
            if (!plane.GetSide(position))
            {
                return;
            }
            var closestPoint = plane.ClosestPointOnPlane(position);
            var radiusToCenter = Vector3.Distance(closestPoint, transform.position);
            var distanceToPlane = plane.GetDistanceToPoint(position);
            var distance = _insideButton ? _buttonReleaseDistance : _buttonPressDistance;
            var radius = _insideButton ? _buttonReleaseRadius : _buttonPressRadius;
            var newInside = distanceToPlane <= distance && radiusToCenter <= radius && extension > 0.5f && angle <= _buttonPressAngle;
            if (!_insideButton && newInside)
            {
                _insideButton = true;
                if (Time.unscaledTime - _lastActivate > _activateCooldown)
                {
                    _active = !_active;
                    if (_active)
                    {
                        _onToolActivate.Invoke();
                    }
                    else
                    {
                        _onToolDeActivate.Invoke();
                    }

                    _lastActivate = Time.unscaledTime;
                }
            }
            else if (_insideButton != newInside)
            {
                _insideButton = false;
            }
        }
    }
}