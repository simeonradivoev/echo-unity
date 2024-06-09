using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;
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
        private float _buttonReleaseDistance;

        [SerializeField]
        private float _buttonPressRadius;

        [SerializeField]
        private float _buttonReleaseRadius;

        [SerializeField]
        private Transform[] _buttons;

        [SerializeField]
        private float _activateCooldown;

        [SerializeField]
        private UnityEvent _onRightToolActivate;

        [SerializeField]
        private UnityEvent _onRightToolDeActivate;

        private readonly bool[] _active = new bool[2];

        private readonly bool[] _insideButton = new bool[2];

        private readonly float[] _lastActivate = new float[2];

        private void Start()
        {
            IKController.OnMove += IKControllerOnOnMove;
        }

        private void OnDestroy()
        {
            IKController.OnMove -= IKControllerOnOnMove;
        }

        private void HandleButton(int buttonIndex, Vector3 fingerPosition, UnityEvent activateEvent, UnityEvent deactivateEvent)
        {
            var button = _buttons[buttonIndex];
            ref var inside = ref _insideButton[buttonIndex];
            ref var isActive = ref _active[buttonIndex];
            ref var lastActivate = ref _lastActivate[buttonIndex];

            var plane = new Plane(button.forward, button.position);
            var closestPoint = plane.ClosestPointOnPlane(fingerPosition);
            var radiusToCenter = Vector3.Distance(closestPoint, button.position);
            var distanceToPlane = plane.GetDistanceToPoint(fingerPosition);
            var distance = inside ? _buttonReleaseDistance : _buttonPressDistance;
            var radius = inside ? _buttonReleaseRadius : _buttonPressRadius;
            var newInside = distanceToPlane <= distance && radiusToCenter <= radius;
            if (!inside && newInside)
            {
                inside = true;
                if (Time.unscaledTime - lastActivate > _activateCooldown)
                {
                    isActive = !isActive;
                    if (isActive)
                    {
                        activateEvent?.Invoke();
                    }
                    else
                    {
                        deactivateEvent?.Invoke();
                    }

                    lastActivate = Time.unscaledTime;
                }
            }
            else if (inside != newInside)
            {
                inside = false;
            }
        }

        private void IKControllerOnOnMove(InputDevice device, Vector3 position, Quaternion rotation)
        {
            if (device == XRController.leftHand)
            {
                HandleButton(0, position, null, null);
            }
            else if (device == XRController.rightHand)
            {
                HandleButton(1, position, _onRightToolActivate, _onRightToolDeActivate);
            }
        }
    }
}