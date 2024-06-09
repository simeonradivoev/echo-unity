using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Interactables
{
    public class ReceptacleConnector : MonoBehaviour
    {
        [SerializeField]
        private string _tag;

        [SerializeField]
        private float _breakingForce;

        [SerializeField]
        private Vector3 _anchor;

        [SerializeField]
        private Rigidbody _rigidBody;

        [SerializeField]
        private Rigidbody _defaultObject;

        [SerializeField]
        private float _maxDistance;

        [SerializeField]
        private float _cooldown = 1;

        [SerializeField]
        private UnityEvent _onAttach;

        [SerializeField]
        private UnityEvent _onDetach;

        private Rigidbody _attachedBody;

        private float _attachTime;

        private FixedJoint _configurableJoint;

        public Rigidbody AttachedRigidbody => _configurableJoint ? _configurableJoint.connectedBody : null;

        public UnityEvent OnAttach => _onAttach;

        public UnityEvent OnDetach => _onDetach;

        private void Start()
        {
            if (_defaultObject)
            {
                Attach(_defaultObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_configurableJoint && _configurableJoint.connectedBody == other.attachedRigidbody)
            {
                Destroy(_configurableJoint);
                _attachedBody = null;
                _onDetach.Invoke();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_attachedBody && !_configurableJoint)
            {
                _onDetach.Invoke();
                _attachedBody = null;
            }

            if (other.CompareTag(_tag) && other.attachedRigidbody && !_configurableJoint && Time.unscaledTime - _attachTime > _cooldown)
            {
                var targetPoint = _rigidBody.transform.TransformPoint(_anchor);
                var distance = Vector3.Distance(other.attachedRigidbody.position, targetPoint);

                if (distance <= _maxDistance)
                {
                    Attach(other.attachedRigidbody);
                }
            }
        }

        private void Attach(Rigidbody other)
        {
            _attachedBody = other;
            other.transform.rotation = _rigidBody.rotation;
            _configurableJoint = _rigidBody.gameObject.AddComponent<FixedJoint>();
            _configurableJoint.breakForce = _breakingForce;
            _configurableJoint.connectedBody = other;
            _configurableJoint.autoConfigureConnectedAnchor = false;
            _configurableJoint.connectedAnchor = Vector3.zero;
            _configurableJoint.anchor = _anchor;
            _attachTime = Time.unscaledTime;
            _onAttach.Invoke();
        }
    }
}