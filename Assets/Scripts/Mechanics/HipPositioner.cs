using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Handles the hip joint.
    /// It moves the rigibody anchor the spring is attached to, needed to avoid issues with the joint resetting rotation references (internal unity
    /// behavioir)
    /// </summary>
    public class HipPositioner : MonoBehaviour
    {
        [SerializeField]
        private PlayerReferences _references;

        [SerializeField]
        private IKController _ikController;

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
            // Add easing to avoid physics glitches
            var targetRotation = Quaternion.RotateTowards(
                _headAnchor.rotation,
                Quaternion.LookRotation(_ikController.TorsoDirection, _references.HeadSpace.up),
                Time.deltaTime * _headAnchorRotationSpeed);

            _headAnchor.Move(_references.Head.position, targetRotation);
            _hipsJoint.anchor = _hipsAcnhorJointOffset;
        }
    }
}