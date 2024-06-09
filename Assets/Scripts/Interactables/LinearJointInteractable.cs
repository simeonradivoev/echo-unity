using System;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Interactables
{
    public class LinearJointInteractable : MonoBehaviour
    {
        [SerializeField]
        [Range(0, 2)]
        private int _axis;

        [SerializeField]
        private bool _callbackOnStart = true;

        [Tooltip(
            "Should ON only be considered if the joint is at the far end within the deadzone. If it gets out of the deadzone it will be considered off.")]
        [SerializeField]
        private bool _oneSide;

        [SerializeField]
        private bool _invert;

        [SerializeField]
        private ConfigurableJoint _joint;

        [SerializeField]
        private float _deadzone;

        [SerializeField]
        private MoveEvent _move;

        [SerializeField]
        private UnityEvent _off;

        [SerializeField]
        private UnityEvent _on;

        private bool _isOn;

        private float _lastDiff;

        private void Start()
        {
            _lastDiff = GetDiff();
            _isOn = _lastDiff <= -_joint.linearLimit.limit + _deadzone;

            if (_callbackOnStart)
            {
                if (_isOn)
                {
                    _on.Invoke();
                }
                else
                {
                    _off.Invoke();
                }
                var diffNormalized = Mathf.Clamp01(_lastDiff + _joint.linearLimit.limit);
                _move.Invoke(diffNormalized);
            }
        }

        private void Update()
        {
            var diff = GetDiff();
            var diffNormalized = Mathf.Clamp01(diff + _joint.linearLimit.limit);

            var isOn = diff <= -_joint.linearLimit.limit + _deadzone;
            var isOff = diff >= _joint.linearLimit.limit - _deadzone;

            if (!Mathf.Approximately(diff, _lastDiff))
            {
                _move.Invoke(diffNormalized);

                if (_oneSide)
                {
                    if (!_isOn && isOn)
                    {
                        _on.Invoke();
                        _isOn = true;
                    }
                    else if (_isOn && !isOn)
                    {
                        _off.Invoke();
                        _isOn = false;
                    }
                }
                else
                {
                    if (!_isOn && isOn)
                    {
                        _on.Invoke();
                        _isOn = true;
                    }
                    else if (_isOn && isOff)
                    {
                        _off.Invoke();
                        _isOn = false;
                    }
                }

                _lastDiff = diff;
            }
        }

        private float GetDiff()
        {
            var connectedAnchor = _joint.connectedBody.transform.TransformPoint(_joint.connectedAnchor);
            var anchor = _joint.transform.TransformPoint(_joint.anchor);
            var diffVector = _invert ? connectedAnchor - anchor : anchor - connectedAnchor;
            return _joint.transform.InverseTransformDirection(diffVector)[_axis];
        }

        [Serializable]
        public class MoveEvent : UnityEvent<float>
        {
        }
    }
}