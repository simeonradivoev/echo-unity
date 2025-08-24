using UnityEngine;

namespace UnityEcho.Mechanics
{
    public class HipPositioner : MonoBehaviour
    {
        [SerializeField]
        private PlayerReferences _references;

        [SerializeField]
        private Rigidbody _hipRigidbody;

        [SerializeField]
        private Vector3 _hipsAcnhorJointOffset;

        [SerializeField]
        private Rigidbody _headAnchor;

        [SerializeField]
        private float _headAnchorRotationSpeed;

        private ConfigurableJoint _hipsJoint;

        private void Awake()
        {
            _hipsJoint = _hipRigidbody.gameObject.GetComponent<ConfigurableJoint>();
        }

        private void FixedUpdate()
        {
            _headAnchor.Move(
                _references.Head.position,
                Quaternion.RotateTowards(_headAnchor.rotation, _references.HeadSpace.rotation, Time.deltaTime * _headAnchorRotationSpeed));
            _hipsJoint.anchor = _hipsAcnhorJointOffset;
        }
    }
}