using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Interactables
{
    public class HingeJoinInteractable : MonoBehaviour
    {
        [SerializeField]
        private bool _defaultState;

        [SerializeField]
        private bool _callbackOnStart = true;

        [SerializeField]
        private bool _invert;

        [SerializeField]
        private HingeJoint _joint;

        [SerializeField]
        private float _endDeadzones;

        [SerializeField]
        private MoveEvent _move;

        [SerializeField]
        private bool _interactible = true;

        [SerializeField]
        private UnityEvent _off;

        [SerializeField]
        private UnityEvent _on;

        private bool _isOn;

        private float _lastAngle;

        private bool _lastInteractible;

        public bool Iteractible
        {
            get => _interactible;
            set => _interactible = value;
        }

        private void Awake()
        {
            var rigidBody = _joint.GetComponent<Rigidbody>();
            _isOn = _defaultState;

            if (_defaultState)
            {
                rigidBody.MoveRotation(
                    _joint.connectedBody.rotation * rigidBody.transform.localRotation * Quaternion.AngleAxis(_joint.limits.max, _joint.axis));
                if (_callbackOnStart)
                {
                    _on.Invoke();
                }
            }
            else
            {
                rigidBody.MoveRotation(
                    _joint.connectedBody.rotation * rigidBody.transform.localRotation * Quaternion.AngleAxis(_joint.limits.min, _joint.axis));
                if (_callbackOnStart)
                {
                    _off.Invoke();
                }
            }

            _lastAngle = _joint.angle;

            if (_callbackOnStart)
            {
                UpdateNormalized(_lastAngle);
            }
        }

        private void Reset()
        {
            _joint = GetComponentInChildren<HingeJoint>();
            _lastInteractible = _interactible;
        }

        private void Update()
        {
            if (!Mathf.Approximately(_lastAngle, _joint.angle) || _lastInteractible != _interactible)
            {
                UpdateLastAngle(_joint.angle);
                UpdateNormalized(_lastAngle);

                _lastInteractible = _interactible;
            }
        }

        private void UpdateLastAngle(float angle)
        {
            var min = _joint.limits.min + _endDeadzones;
            var max = _joint.limits.max - _endDeadzones;

            var minReached = _invert ? angle > max : angle < min;
            var maxReached = _invert ? angle < min : angle > max;

            if ((!_interactible || minReached) && _isOn)
            {
                _off.Invoke();
                _isOn = false;
            }
            else if (_interactible && maxReached && !_isOn)
            {
                _on.Invoke();
                _isOn = true;
            }

            _lastAngle = angle;
        }

        private void UpdateNormalized(float angle)
        {
            var range = Mathf.Abs(Mathf.DeltaAngle(_joint.limits.max, _joint.limits.min));
            var deltaAngle = angle - _joint.limits.min;

            var normalized = Mathf.Clamp01((deltaAngle - _endDeadzones) / (range - _endDeadzones * 2));
            if (_invert)
            {
                normalized = 1 - normalized;
            }
            _move.Invoke(normalized);
        }

        [Serializable]
        public class MoveEvent : UnityEvent<float>
        {
        }
    }
}