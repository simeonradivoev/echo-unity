using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Having infinite mass on the grab joint actually makes objects that the player holds rotate quite fast.
    /// Rotates the player around either the head or if there is a grabbed point, it rotates around it.
    /// </summary>
    public class TurnController : MonoBehaviour
    {
        [SerializeField]
        private PlayerReferences _references;

        [SerializeField]
        private InputActionReference _turnAction;

        private GrabMoveController[] _grabControllers;

        private Rigidbody _rigidbody;

        private bool _turned;

        private void Reset()
        {
            _references = GetComponent<PlayerReferences>();
        }

        private void Start()
        {
            _turnAction.action.Enable();
            _rigidbody = GetComponent<Rigidbody>();
            _grabControllers = GetComponentsInChildren<GrabMoveController>();
        }

        private void FixedUpdate()
        {
            var turnValue = _turnAction.action.ReadValue<float>();
            if (turnValue > 0.5f || turnValue < -0.5f)
            {
                if (_turned)
                {
                    return;
                }

                var desiredRot = Quaternion.identity;
                if (turnValue > 0.5f)
                {
                    desiredRot = Quaternion.AngleAxis(45, Vector3.up);
                }
                else
                {
                    desiredRot = Quaternion.AngleAxis(-45, Vector3.up);
                }

                var head = _references.Head;
                var headCollider = _references.HeadCollider;

                var rigidbodyPos = _rigidbody.position;
                // we first rotate around head collider since that is safe
                var headCenterOffset = head.position - rigidbodyPos;
                var headCenterRotatedOffset = headCenterOffset - desiredRot * headCenterOffset;
                _references.HeadSpace.transform.localRotation *= desiredRot;
                _rigidbody.position += headCenterRotatedOffset;

                var hadOffCenter = CalculateCenter(out var offCenter);
                if (hadOffCenter)
                {
                    // then we calculate the offset relative to the grab center and do sweep test so we don't end up in a wall
                    var relativePosition = offCenter - head.position;
                    var headOffset = relativePosition - desiredRot * relativePosition;
                    var headOffsetMagnitude = headOffset.magnitude;
                    if (headOffsetMagnitude > 0)
                    {
                        var offset = headOffset;

                        if (Physics.Raycast(head.position, headOffset, out var hit, headOffsetMagnitude + headCollider.radius))
                        {
                            offset = Vector3.ClampMagnitude(headOffset, Mathf.Max(0, hit.distance - headCollider.radius));

                            foreach (var grabController in _grabControllers)
                            {
                                if (grabController.StaticGrabJoint)
                                {
                                    var joint = grabController.StaticGrabJoint.GetComponent<ConfigurableJoint>();
                                    grabController.StaticGrabCooldown += Time.fixedDeltaTime * 3;
                                }
                            }
                        }
                        else
                        {
                            offset = headOffset;
                        }

                        _rigidbody.position += offset;
                    }
                }

                Physics.SyncTransforms();

                _turned = true;
            }
            else
            {
                _turned = false;
            }
        }

        private bool CalculateCenter(out Vector3 center)
        {
            var averagePoint = Vector3.zero;
            var pointCount = 0;

            foreach (var grabController in _grabControllers)
            {
                // we only check static grabs as other types can cause the player to rotate through the level
                if (grabController.StaticGrabJoint)
                {
                    averagePoint += grabController.StaticGrabJoint.transform.position;
                    pointCount++;
                }
            }

            if (pointCount <= 0)
            {
                center = _references.Head.position;
                return false;
            }

            center = averagePoint / pointCount;
            return true;
        }
    }
}