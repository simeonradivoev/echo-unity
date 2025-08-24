using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Handles the bulk of the IK logic on the player.
    /// </summary>
    public class IKController : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _body;

        [SerializeField]
        private Transform _headSpace;

        [SerializeField]
        private GrabMoveController _leftHand;

        [SerializeField]
        private Transform _leftHandControllerReference;

        [SerializeField]
        private GrabMoveController _rightHand;

        [SerializeField]
        private Transform _rightHandControllerReference;

        [SerializeField]
        private Transform _head;

        [SerializeField]
        private Transform _headBone;

        [SerializeField]
        [Range(0, 1)]
        private float _headWeight;

        [Range(0, 1)]
        [SerializeField]
        private float _bodyWeight;

        [SerializeField]
        private float _handsWeight;

        [SerializeField]
        private AnimationCurve _handDistanceWeightCurve;

        [SerializeField]
        private Vector3 _leftHandCollisionRotationOffset;

        [SerializeField]
        private Vector3 _rightHandCollisionRotationOffset;

        [SerializeField]
        private Vector2 _maxPitchRange;

        [SerializeField]
        private float _shoulderExtension;

        [SerializeField]
        private AnimationCurve _elbowRotationMap;

        [SerializeField]
        private float _grabEaseDuration = 0.1f;

        [SerializeField]
        private Transform _leftFingerPointer;

        [SerializeField]
        private Transform _rightFingerPointer;

        [SerializeField]
        private float _headLookOffset;

        private readonly Vector3[] _fingerOffset = new Vector3[2];

        private readonly Vector3[] _handPositions = new Vector3[2];

        private readonly Quaternion[] _handRotations = new Quaternion[2] { Quaternion.identity, Quaternion.identity };

        private readonly Quaternion[] _lastGrabRotation = new Quaternion[2] { Quaternion.identity, Quaternion.identity };

        private readonly Matrix4x4[] _lastGrabSpace = new Matrix4x4[2] { Matrix4x4.identity, Matrix4x4.identity };

        private readonly List<XRNodeState> _nodes = new();

        private bool2 _connected;

        private float _elbowSwivelPercentSmooth;

        private float _lastSwivelAngleVel;

        private ShoulderPlaneDefinition _shoulderPlane;

        public Vector3 TorsoDirection { get; private set; } = Vector3.forward;

        public Animator Animator { get; private set; }

        public Rigidbody Body => _body;

        private void Start()
        {
            Animator = GetComponent<Animator>();
            _shoulderPlane = GetComponentInChildren<ShoulderPlaneDefinition>();
        }

        private void Update()
        {
            var headDirection = Vector3.ProjectOnPlane(_head.forward, _body.rotation * Vector3.up).normalized;
            var leftArmDir = _leftHand.transform.position - _head.position;
            leftArmDir.y = 0;
            if (!_connected[0])
            {
                leftArmDir = Vector3.left;
            }

            var leftArmDistance = Vector3.Magnitude(leftArmDir);
            if (leftArmDistance > 0)
            {
                leftArmDir /= leftArmDistance;
            }

            var rightArmDir = _rightHand.transform.position - _head.position;
            rightArmDir.y = 0;
            if (!_connected[1])
            {
                rightArmDir = Vector3.right;
            }
            var rightArmDistance = Vector3.Magnitude(rightArmDir);
            if (rightArmDistance > 0)
            {
                rightArmDir /= rightArmDistance;
            }

            TorsoDirection = Vector3.Normalize(
                headDirection +
                leftArmDir * _handDistanceWeightCurve.Evaluate(leftArmDistance) * _handsWeight +
                rightArmDir * _handDistanceWeightCurve.Evaluate(rightArmDistance) * _handsWeight);

            GetRawValues();

            // Need to be done in late update otherwise it stutters
            //var headBoneOffset = _head.position - _headBone.position;
            //transform.position += headBoneOffset;
            //transform.rotation = Quaternion.LookRotation(TorsoDirection, _body.rotation * Vector3.up);

            UpdateHandIk(AvatarIKGoal.LeftHand, _leftHand, _leftHandControllerReference, Quaternion.Euler(_leftHandCollisionRotationOffset));
            UpdateHandIk(AvatarIKGoal.RightHand, _rightHand, _rightHandControllerReference, Quaternion.Euler(_rightHandCollisionRotationOffset));
        }

        private void OnAnimatorIK(int layerIndex)
        {
            var leftWeight = _connected[0] ? 1 : 0;
            Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftWeight);
            Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftWeight);

            var rightWeight = _connected[1] ? 1 : 0;
            Animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightWeight);
            Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightWeight);

            Animator.SetIKPosition(AvatarIKGoal.LeftHand, _handPositions[0]);
            Animator.SetIKPosition(AvatarIKGoal.RightHand, _handPositions[1]);

            Animator.SetIKRotation(AvatarIKGoal.LeftHand, _handRotations[0]);
            Animator.SetIKRotation(AvatarIKGoal.RightHand, _handRotations[1]);

            Animator.SetLookAtPosition(_head.position + TorsoDirection * _headLookOffset);
            Animator.SetLookAtWeight(_headWeight, _bodyWeight);

            UpdateShoulderPos(HumanBodyBones.LeftShoulder, HumanBodyBones.LeftUpperArm, _handPositions[0], out var leftArmHeight);
            UpdateShoulderPos(HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, _handPositions[1], out var rightArmHeight);

            UpdateElbowSwivel(HumanBodyBones.LeftShoulder, AvatarIKHint.LeftElbow, Vector3.forward, Vector3.left, _leftHand, leftArmHeight);
            UpdateElbowSwivel(HumanBodyBones.RightShoulder, AvatarIKHint.RightElbow, Vector3.back, Vector3.right, _rightHand, rightArmHeight);
        }

        private void UpdateElbowSwivel(
            HumanBodyBones shoulder,
            AvatarIKHint hint,
            Vector3 rotationDirection,
            Vector3 side,
            GrabMoveController hand,
            float armHeight)
        {
            var headSpace = Matrix4x4.TRS(_head.position, Quaternion.LookRotation(TorsoDirection, _headSpace.up), Vector3.one);

            var headPlane = new Plane(headSpace.MultiplyVector(Vector3.forward), _head.position);
            var distanceToChest = Mathf.Abs(headPlane.GetDistanceToPoint(hand.transform.position));
            var shoulderTransform = Animator.GetBoneTransform(shoulder);
            var dir = hand.transform.position - shoulderTransform.position;
            var distance = Mathf.Clamp01(distanceToChest / 0.4f);
            var middleOfArm = shoulderTransform.position + dir / 2f;
            var up = headSpace.MultiplyVector(Vector3.up);
            var elbowSwivelPercent = Quaternion.Angle(
                Quaternion.LookRotation(hand.transform.TransformDirection(hand.GrabDirection), up),
                Quaternion.LookRotation(headSpace.MultiplyVector(Vector3.down), up));
            _elbowSwivelPercentSmooth = Mathf.SmoothDampAngle(_elbowSwivelPercentSmooth, elbowSwivelPercent, ref _lastSwivelAngleVel, Time.deltaTime);
            var elbowSwivelAngle = _elbowRotationMap.Evaluate(_elbowSwivelPercentSmooth);
            var elbowDir = headSpace.MultiplyVector(Quaternion.AngleAxis(elbowSwivelAngle, rotationDirection) * Vector3.down);
            var hintPosition = Vector3.Slerp(
                shoulderTransform.position + headSpace.MultiplyVector(Vector3.down * 0.4f + Vector3.back * 0.4f + side * 0.4f),
                middleOfArm + elbowDir,
                Mathf.Pow(distance, 2));
            Animator.SetIKHintPosition(hint, hintPosition);
            Animator.SetIKHintPositionWeight(hint, 1 - armHeight);
        }

        private void UpdateShoulderPos(HumanBodyBones shoulder, HumanBodyBones arm, Vector3 handPos, out float armHeight)
        {
            var shoulderBone = Animator.GetBoneTransform(shoulder);
            var upperArmBone = Animator.GetBoneTransform(arm);

            // Move the shoulders back so we don't glitch out when arms are close to body and sometimes behind the shoulders.
            var handDir = Vector3.Normalize(handPos - (shoulderBone.position - TorsoDirection * 0.4f));
            var shoulderPlane = _shoulderPlane.transform.right;
            var projectedHandDir = Vector3.Normalize(Vector3.ProjectOnPlane(handDir, shoulderPlane));
            var projectedIdleHandDir = Vector3.Normalize(Vector3.ProjectOnPlane(TorsoDirection, shoulderPlane));

            var angle = Vector3.SignedAngle(projectedHandDir, projectedIdleHandDir, shoulderPlane);
            armHeight = Mathf.Pow(Mathf.Clamp01(angle / 45), 2);

            var projectedPos = upperArmBone.position + _shoulderPlane.transform.up * armHeight * _shoulderExtension;

            var dir = Vector3.Normalize(shoulderBone.InverseTransformPoint(upperArmBone.position));
            var desiredDir = Vector3.Normalize(shoulderBone.InverseTransformPoint(projectedPos));

            var rotationDelta = Quaternion.FromToRotation(dir, desiredDir);

            Animator.SetBoneLocalRotation(shoulder, shoulderBone.localRotation * rotationDelta);
        }

        private void UpdateHandIk(AvatarIKGoal goal, GrabMoveController hand, Transform reference, Quaternion rotationOffset)
        {
            CalculateHand(
                goal - AvatarIKGoal.LeftHand,
                hand,
                rotationOffset,
                out var handRotation,
                out var handSpace,
                out var rawHandSpace,
                out var rawRotation);

            if (hand.HasGrabbed)
            {
                _lastGrabSpace[goal - AvatarIKGoal.LeftHand] = handSpace;
                _lastGrabRotation[goal - AvatarIKGoal.LeftHand] = handRotation;
            }

            var desiredPosition = handSpace.MultiplyPoint(-(Quaternion.Inverse(reference.localRotation) * reference.localPosition));
            var rawPosition = rawHandSpace.MultiplyPoint(-(Quaternion.Inverse(reference.localRotation) * reference.localPosition));
            var lastPosition = _lastGrabSpace[goal - AvatarIKGoal.LeftHand]
                .MultiplyPoint(-(Quaternion.Inverse(reference.localRotation) * reference.localPosition));

            var grabEasingTime = Mathf.Pow(Mathf.Clamp01((Time.time - hand.GrabData.GrabHitTime) / _grabEaseDuration), 2);
            var releaseEasingTime = Mathf.Pow(Mathf.Clamp01((Time.time - hand.GrabData.ReleaseTime) / _grabEaseDuration), 2);

            var goalIndex = goal - AvatarIKGoal.LeftHand;
            _handPositions[goalIndex] = Vector3.Lerp(lastPosition, Vector3.Lerp(rawPosition, desiredPosition, grabEasingTime), releaseEasingTime);
            _handRotations[goalIndex] = Quaternion.Lerp(
                                            _lastGrabRotation[goal - AvatarIKGoal.LeftHand],
                                            Quaternion.Lerp(rawRotation, handRotation, grabEasingTime),
                                            releaseEasingTime) *
                                        Quaternion.Inverse(reference.localRotation);
        }

        private void GetRawValues()
        {
            _connected = false;

            InputTracking.GetNodeStates(_nodes);
            foreach (var node in _nodes)
            {
                if (node.nodeType == XRNode.RightHand || node.nodeType == XRNode.LeftHand)
                {
                    _connected[node.nodeType - XRNode.LeftHand] = node.tracked;
                }
            }
        }

        private void CalculateHand(
            int handIndex,
            GrabMoveController hand,
            Quaternion rotationOffset,
            out Quaternion rotation,
            out Matrix4x4 space,
            out Matrix4x4 rawSpace,
            out Quaternion rawRotation)
        {
            rawRotation = hand.RawRotation;
            rawSpace = Matrix4x4.TRS(hand.RawClampedWorldPosition, rawRotation, Vector3.one);

            if (hand.GrabData.GrabHit.Collider && hand.HasGrabbed)
            {
                var worldGrabNormal = hand.GrabData.GrabHit.Collider.transform.TransformDirection(hand.GrabData.GrabLocalNormal);
                var worldGrabForward = hand.GrabData.GrabHit.Collider.transform.TransformDirection(hand.GrabData.GrabLocalForward);

                var desiredGrabDir = rawRotation * hand.GrabDirection;
                var desiredGrabForward = hand.GrabData.GrabHit.Collider.transform.TransformDirection(rawRotation * hand.ForwardDirection);

                var yawAxis = worldGrabNormal;
                var yawDelta = Vector3.SignedAngle(
                    Vector3.ProjectOnPlane(worldGrabForward, yawAxis),
                    Vector3.ProjectOnPlane(desiredGrabForward, yawAxis),
                    yawAxis);
                var pitchAxis = rawRotation * hand.ForwardDirection;
                var pitchDelta = Vector3.SignedAngle(
                    Vector3.ProjectOnPlane(-worldGrabNormal, pitchAxis),
                    Vector3.ProjectOnPlane(desiredGrabDir, pitchAxis),
                    pitchAxis);

                rotation = Quaternion.LookRotation(worldGrabForward, worldGrabNormal) * rotationOffset;

                if (hand.DynamicGrabJoint)
                {
                    space = Matrix4x4.TRS(
                        hand.DynamicGrabJoint.connectedBody.transform.TransformPoint(hand.DynamicGrabJoint.connectedAnchor),
                        rotation,
                        Vector3.one);
                }
                else
                {
                    space = Matrix4x4.TRS(
                        hand.GrabData.GrabHit.Collider.transform.TransformPoint(hand.GrabData.GrabHit.LocalPosition),
                        rotation,
                        Vector3.one);
                }
            }
            else
            {
                // we want raw rotation since hand rigidbody 
                rotation = rawRotation;
                space = rawSpace;
            }
        }
    }
}