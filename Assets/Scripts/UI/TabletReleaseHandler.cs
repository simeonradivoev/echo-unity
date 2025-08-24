using Tweens;
using UnityEcho.Mechanics;
using UnityEcho.Mechanics.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace UnityEcho.UI
{
    public class TabletReleaseHandler : MonoBehaviour, IReleaseHandler, IGrabHandler
    {
        [SerializeField]
        private PlayerReferences _playerRefs;

        [SerializeField]
        private InputActionReference _openInput;

        [SerializeField]
        private float _velocityThreshold;

        [SerializeField]
        private float _closeDistanceThreshold;

        [SerializeField]
        private bool _startOpen;

        [SerializeField]
        private IKController _controller;

        [SerializeField]
        private float _defaultDistance;

        [Tooltip("At what angle from the torso direction should the UI be re-position in front of the player.")]
        [SerializeField]
        private float _maxRepositionAngle;

        [SerializeField]
        private UnityEvent _onOpen;

        [SerializeField]
        private UnityEvent _onClose;

        private Vector3 _angularVelocity;

        private Canvas _canvas;

        private CanvasGroup _canvasGroup;

        private Collider _collider;

        private Vector3 _lastPickedUpChestDirection;

        private Vector3 _lastPickedUpPosition;

        private Quaternion _lastPickupRotation;

        private TweenInstance _toggleTween;

        private Vector3 _velocity;

        public bool Enabled => _canvas.enabled;

        private void Awake()
        {
            StoreLastPickup();
        }

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _collider = GetComponentInChildren<Collider>();
            _canvas.enabled = _startOpen;
            _collider.enabled = _startOpen;
            _canvasGroup.alpha = _startOpen ? 1 : 0;

            _openInput.action.Enable();
            _openInput.action.performed += OnOpenPerformed;
        }

        public void Update()
        {
            if (!Enabled)
            {
                return;
            }

            transform.localPosition += _velocity * Time.deltaTime;
            transform.localRotation *= Quaternion.Euler(_angularVelocity * Mathf.Rad2Deg * Time.deltaTime);

            if ((transform.position - _playerRefs.Head.position).magnitude >= _closeDistanceThreshold && _toggleTween == null)
            {
                SetEnabled(false, 0.6f);
            }
        }

        private void OnDestroy()
        {
            _openInput.action.performed -= OnOpenPerformed;
        }

        #region Implementation of IGrabHandler

        public void OnGrab(GrabContext context)
        {
            StoreLastPickup();
        }

        #endregion

        public void SetEnabled(bool enabled)
        {
            SetEnabled(enabled, 0.3f);
        }

        public void SetEnabled(bool enabled, float duration)
        {
            _toggleTween?.Cancel();
            if (enabled)
            {
                _onOpen.Invoke();
            }
            else
            {
                _onClose.Invoke();
            }
            _toggleTween = gameObject.AddTween(
                new FloatTween
                {
                    to = enabled ? 1 : 0,
                    from = _canvasGroup.alpha,
                    onStart = t =>
                    {
                        _canvas.enabled = true;
                        _collider.enabled = false;
                        if (enabled)
                        {
                            _velocity = Vector3.zero;
                            _angularVelocity = Vector3.zero;
                        }
                    },
                    onUpdate = (t, v) => { _canvasGroup.alpha = v; },
                    onEnd = t =>
                    {
                        _canvas.enabled = enabled;
                        _collider.enabled = enabled;
                        _toggleTween = null;
                    },
                    duration = duration,
                    easeType = EaseType.QuadOut
                });
        }

        private void Reposition()
        {
            var neck = _controller.Animator.GetBoneTransform(HumanBodyBones.Neck);

            if ((transform.position - _playerRefs.Head.position).magnitude >= _closeDistanceThreshold)
            {
                // too far away we need to force it closer
                transform.position = neck.position + _controller.TorsoDirection * _defaultDistance;
                transform.rotation = Quaternion.LookRotation(_controller.TorsoDirection, Vector3.up);

                StoreLastPickup();
            }
            else
            {
                var localTorsoDirection = transform.parent.InverseTransformDirection(_controller.TorsoDirection);
                var angle = Vector3.Angle(localTorsoDirection, _lastPickedUpChestDirection);

                // We can just rotate by the offset, we don't need an entirely new position
                if (angle > _maxRepositionAngle)
                {
                    var rotationOffset = Quaternion.LookRotation(localTorsoDirection, Vector3.up) *
                                         Quaternion.Inverse(Quaternion.LookRotation(_lastPickedUpChestDirection, Vector3.up));

                    var localHeadPosition = transform.parent.InverseTransformPoint(_playerRefs.Head.position);
                    transform.localPosition = localHeadPosition + rotationOffset * _lastPickedUpPosition;
                    transform.localRotation = rotationOffset * _lastPickupRotation;

                    StoreLastPickup();
                }
            }
        }

        public void Show()
        {
            if (!Enabled)
            {
                transform.localPosition = transform.parent.InverseTransformPoint(_playerRefs.Head.position) + _lastPickedUpPosition;
                transform.localRotation = _lastPickupRotation;

                Reposition();
            }
            SetEnabled(!Enabled);
        }

        private void OnOpenPerformed(InputAction.CallbackContext obj)
        {
            if (!Enabled)
            {
                Show();
            }
            else
            {
                SetEnabled(!Enabled);
            }
        }

        #region Implementation of IReleaseHandler

        public void OnRelease(ReleaseContext context)
        {
            if (context.Velocity.magnitude >= _velocityThreshold)
            {
                _velocity = context.Velocity;
                _angularVelocity = context.AngularVelocity;
            }
            else
            {
                StoreLastPickup();
            }
        }

        private void StoreLastPickup()
        {
            _lastPickedUpPosition = transform.localPosition - transform.parent.InverseTransformPoint(_playerRefs.Head.position);
            _lastPickupRotation = transform.localRotation;
            _lastPickedUpChestDirection = transform.parent.InverseTransformDirection(_controller.TorsoDirection);
        }

        #endregion
    }
}