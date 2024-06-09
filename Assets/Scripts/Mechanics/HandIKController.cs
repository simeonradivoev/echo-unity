using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using UnityEcho.Mechanics.Data;
using UnityEcho.Mechanics.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;

namespace UnityEcho.Mechanics
{
    public class HandIKController : MonoBehaviour
    {
        [SerializeField]
        private bool _leftHand;

        [FormerlySerializedAs("_idleRotationOffsets")]
        [SerializeField]
        private float[] _idleSpreadRotationOffsets = new float[5];

        [SerializeField]
        private FingerRotationValues _rotations;

        [SerializeField]
        private InputActionReference _triggerTouch;

        [SerializeField]
        private InputActionReference _gripInput;

        [SerializeField]
        private InputActionReference _triggerInput;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private Vector3 _handRotationOffset;

        [SerializeField]
        private Animator _animator;

        private readonly List<(Vector3, Color)> _debugIntersectionPoints = new();

        private readonly FingerDefinition[] _fingerDefinitions = new FingerDefinition[5];

        // Holds all finger joints on one hand
        private readonly Quaternion[] _fingerRotations = new Quaternion[5 * 3];

        private readonly List<(RaycastHit hit, float angle)> _hits = new();

        private MeshConnectivityBuilder _connectivityBuilder;

        private MeshConnectivityBuilderHelper _connectivityBuilderHelper;

        private Mesh _currentMesh;

        private GrabMoveController _grabMoveController;

        private float _gripInputValue;

        private IKConnector _ikConnector;

        private IKController _ikController;

        private float _indexPointInputValue;

        private NativeHashSet<float3> _intersectionPoints;

        private int _start;

        private float _triggerInputValue;

        private InputDevice Device => _leftHand ? XRController.leftHand : XRController.rightHand;

        private void Start()
        {
            _connectivityBuilderHelper = FindObjectOfType<MeshConnectivityBuilderHelper>();
            _intersectionPoints = new NativeHashSet<float3>(64, Allocator.Persistent);

            _grabMoveController = GetComponent<GrabMoveController>();
            _ikConnector = _animator.GetComponent<IKConnector>();

            _start = (int)(_leftHand ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);

            for (var i = 0; i < _fingerRotations.Length; i++)
            {
                _fingerRotations[i] = Quaternion.identity;
            }

            for (var i = 0; i < 5; i++)
            {
                _fingerDefinitions[i] = _animator.GetBoneTransform((HumanBodyBones)_start + i * 3).GetComponent<FingerDefinition>();
            }

            _ikConnector.AnimatorIK += IkConnectorOnAnimatorIK;

            _triggerTouch.action.Enable();
            _gripInput.action.Enable();
            _triggerInput.action.Enable();

            _triggerTouch.action.performed += TriggerTouchOnPerformed;
            _gripInput.action.performed += GripOnPerformed;
            _triggerInput.action.performed += TriggerOnPerformed;
        }

        private void LateUpdate()
        {
            _debugIntersectionPoints.Clear();

            if (_grabMoveController.HasGrabbed)
            {
                for (var i = 0; i < 5; i++)
                {
                    UpdateFinger(i == 0, _fingerDefinitions[i], (HumanBodyBones)_start + i * 3, _fingerDefinitions[i].Radius);
                }
            }
            else
            {
                for (var i = 0; i < 5; i++)
                {
                    UpdateOffsetRotations(i == 0, _fingerDefinitions[i], (HumanBodyBones)_start + i * 3);
                }
            }
        }

        private void OnDestroy()
        {
            _intersectionPoints.Dispose();
            _ikConnector.AnimatorIK -= IkConnectorOnAnimatorIK;
            _triggerTouch.action.performed -= TriggerTouchOnPerformed;
            _gripInput.action.performed -= GripOnPerformed;
            _triggerInput.action.performed -= TriggerOnPerformed;
        }

        private void OnDrawGizmos()
        {
            foreach (var point in _debugIntersectionPoints)
            {
                Gizmos.color = point.Item2;
                Gizmos.DrawSphere(point.Item1, 0.01f);
            }
        }

        private void TriggerTouchOnPerformed(InputAction.CallbackContext obj)
        {
            if (Device == obj.control.device)
            {
                _indexPointInputValue = obj.ReadValue<float>();
            }
        }

        private void GripOnPerformed(InputAction.CallbackContext obj)
        {
            if (Device == obj.control.device)
            {
                _gripInputValue = obj.ReadValue<float>();
            }
        }

        private void TriggerOnPerformed(InputAction.CallbackContext obj)
        {
            if (Device == obj.control.device)
            {
                _triggerInputValue = obj.ReadValue<float>();
            }
        }

        private void IkConnectorOnAnimatorIK(int obj)
        {
            for (var i = 0; i < _fingerRotations.Length; i++)
            {
                _animator.SetBoneLocalRotation((HumanBodyBones)_start + i, _fingerRotations[i]);
            }
        }

        private void UpdateFinger(bool isThumb, FingerDefinition fingerDefinition, HumanBodyBones fingerBase, float fingerLength)
        {
            var fingerIndex = ((int)fingerBase - _start) / 3;

            var curlAxis = fingerDefinition.FingerDiskDirection;
            var upAxis = fingerDefinition.FingerUpDirection;
            var rot = _rotations.GrabFingerHitRotations[fingerIndex];
            var fingerTransform = _animator.GetBoneTransform(fingerBase);
            var handTransform = fingerTransform.parent;
            var handFingerTransform = handTransform.localToWorldMatrix;
            var handFingerTransformInverse = handFingerTransform.inverse;
            var indexPositionLocal = fingerTransform.localPosition;
            //var plane = new Plane(indexTransform.forward, indexPosition);

            var angleDelta = Mathf.PI * 2f / 32f;
            var spreadRot = Quaternion.AngleAxis(fingerDefinition.GrabSpread, upAxis);

            var maxCurlAngle = fingerDefinition.MaxCurlAngle;
            var curlAngleRange = fingerDefinition.CurlAngleRange;
            _hits.Clear();

            if (_grabMoveController.GrabData.GrabHit.Collider is MeshCollider meshCollider)
            {
                if (_currentMesh != meshCollider.sharedMesh)
                {
                    _connectivityBuilder = _connectivityBuilderHelper.GetOrBuild(meshCollider.sharedMesh);
                    _currentMesh = meshCollider.sharedMesh;
                }

                var colliderTransform = meshCollider.transform;
                var fingerWorldPos = fingerTransform.position;
                var normalRotation = spreadRot * Quaternion.AngleAxis(rot.x, curlAxis);
                var fingerPlane = new Plane(handFingerTransform.MultiplyVector(normalRotation * curlAxis), fingerWorldPos);
                _intersectionPoints.Clear();

                if (_grabMoveController.GrabData.GrabHit.Triangle >= 0)
                {
                    var job = new OptimizedDiskMeshIntersection
                    {
                        intersectionPoints = _intersectionPoints,
                        ConnectivityBuilder = _connectivityBuilder,
                        diskCenter = fingerWorldPos,
                        diskNormal = fingerPlane.normal,
                        diskRadius = fingerLength,
                        StartTriangle = _grabMoveController.GrabData.GrabHit.Triangle,
                        colliderTransform = meshCollider.transform.localToWorldMatrix
                    };
                    job.Run();
                }

                if (!_intersectionPoints.IsEmpty)
                {
                    var highestAngle = 0f;
                    var highestPoint = float3.zero;

                    foreach (var worldPoint in _intersectionPoints)
                    {
                        var hitDirLocal = Vector3.Normalize(handFingerTransformInverse.MultiplyPoint(worldPoint) - fingerTransform.localPosition);
                        float hitAngle;
                        if (isThumb)
                        {
                            var thumbAngleDir = _leftHand ? Vector3.left : Vector3.right;
                            var axis = _leftHand ? Vector3.forward : Vector3.forward;
                            hitAngle = Vector3.SignedAngle(hitDirLocal, thumbAngleDir, axis);
                        }
                        else
                        {
                            var to = Vector3.Cross(fingerDefinition.FingerDiskDirection, fingerDefinition.FingerUpDirection);
                            var rotatedTo = spreadRot * Quaternion.AngleAxis(maxCurlAngle, curlAxis) * to;
                            var fromNormalized = Vector3.Normalize(hitDirLocal);
                            var rotatedAxis = spreadRot * -fingerDefinition.FingerDiskDirection;
                            hitAngle = Vector3.SignedAngle(rotatedTo, fromNormalized, rotatedAxis);

                            Debug.DrawLine(
                                fingerTransform.position,
                                handFingerTransformInverse.MultiplyPoint(fingerTransform.localPosition + rotatedAxis),
                                Color.green);
                            Debug.DrawLine(
                                fingerTransform.position,
                                handFingerTransformInverse.MultiplyPoint(fingerTransform.localPosition + fromNormalized),
                                Color.cyan);
                            Debug.DrawLine(
                                fingerTransform.position,
                                handFingerTransformInverse.MultiplyPoint(fingerTransform.localPosition + rotatedTo),
                                Color.yellow);
                        }

                        if (hitAngle > highestAngle && hitAngle <= curlAngleRange)
                        {
                            highestAngle = hitAngle;
                            highestPoint = worldPoint;
                        }
                    }

                    foreach (var worldPoint in _intersectionPoints)
                    {
                        if (math.distance(worldPoint, highestPoint) < 0.001f)
                        {
                            _debugIntersectionPoints.Add((worldPoint, Color.red));
                        }
                        else
                        {
                            _debugIntersectionPoints.Add((worldPoint, Color.yellow));
                        }
                    }

                    _debugIntersectionPoints.Add((fingerWorldPos, Color.magenta));

                    var clampedAngle = Mathf.Clamp(highestAngle, 0, curlAngleRange);
                    // We compare intersection points with the finger curled to the max so we need to shift it up so it starts from there and not from the neutral extended finger.
                    var shiftedCurlAngle = clampedAngle - maxCurlAngle;
                    SetFingerRotationSmooth(
                        fingerBase,
                        spreadRot * Quaternion.AngleAxis(shiftedCurlAngle + rot.x, -curlAxis),
                        _speed * Time.deltaTime);
                    SetFingerRotationSmooth(fingerBase + 1, Quaternion.AngleAxis(rot.y, curlAxis), _speed * Time.deltaTime);
                    SetFingerRotationSmooth(fingerBase + 2, Quaternion.AngleAxis(rot.z, curlAxis), _speed * Time.deltaTime);
                    Debug.DrawLine(fingerTransform.position, highestPoint, Color.blue);
                    return;
                }
            }

            UpdateOffsetRotations(isThumb, fingerDefinition, fingerBase);
        }

        private void UpdateOffsetRotations(bool isThumb, FingerDefinition fingerDefinition, HumanBodyBones fingerBase)
        {
            var fingerIndex = ((int)fingerBase - _start) / 3;

            var isIndex = fingerBase is HumanBodyBones.LeftIndexProximal or HumanBodyBones.RightIndexProximal;
            var rotOffsets = _rotations.IdleFingerRotations[fingerIndex];
            var pointValue = _indexPointInputValue;
            var grabValue = isIndex ? _triggerInputValue : _gripInputValue;
            if (isIndex)
            {
                rotOffsets = Vector3.Lerp(_rotations.IndexPointRotations, rotOffsets, pointValue);
            }
            var hadGrab = _grabMoveController.HasGrabbed;
            if (hadGrab)
            {
                rotOffsets = _rotations.GrabFingerRotations[fingerIndex];
            }
            else
            {
                rotOffsets = Vector3.Lerp(rotOffsets, _rotations.FistFingerRotations[fingerIndex], grabValue);
            }

            var curlAxis = fingerDefinition.FingerDiskDirection;
            var upAxis = fingerDefinition.FingerUpDirection;
            var fingerRot = Quaternion.AngleAxis(_idleSpreadRotationOffsets[(int)(fingerBase - _start) / 3], upAxis);
            SetFingerRotationSmooth(fingerBase, fingerRot * Quaternion.AngleAxis(rotOffsets.x, curlAxis), _speed * Time.deltaTime);
            SetFingerRotationSmooth(fingerBase + 1, Quaternion.AngleAxis(rotOffsets.y, curlAxis), _speed * Time.deltaTime);
            SetFingerRotationSmooth(fingerBase + 2, Quaternion.AngleAxis(rotOffsets.z, curlAxis), _speed * Time.deltaTime);
        }

        private void SetFingerRotationSmooth(HumanBodyBones bone, Quaternion rotation, float speed)
        {
            _fingerRotations[(int)bone - _start] = Quaternion.RotateTowards(_fingerRotations[(int)bone - _start], rotation, speed);
        }

        [BurstCompile]
        private struct OptimizedDiskMeshIntersection : IJob
        {
            [ReadOnly]
            public MeshConnectivityBuilder ConnectivityBuilder;

            [ReadOnly]
            public int StartTriangle;

            [ReadOnly]
            public float3 diskCenter;

            [ReadOnly]
            public float3 diskNormal;

            [ReadOnly]
            public float diskRadius;

            public NativeHashSet<float3> intersectionPoints;

            public float4x4 colliderTransform;

            private bool CheckEdge(Plane trianglePlane, int3 triangle, MeshConnectivityBuilder.Edge edge)
            {
                var start = math.mul(colliderTransform, new float4(ConnectivityBuilder.VerticesRaw[triangle[edge.Start]], 1)).xyz;
                var end = math.mul(colliderTransform, new float4(ConnectivityBuilder.VerticesRaw[triangle[(edge.Start + 1) % 3]], 1)).xyz;

                var dir = end - start;
                var dirLength = math.length(dir);
                dir /= dirLength;

                var distance = -math.dot(diskNormal, diskCenter);
                var a = math.dot(dir, diskNormal);
                var num = -math.dot(start, diskNormal) - distance;
                if (Mathf.Approximately(a, 0.0f))
                {
                    return false;
                }
                var enter = num / a;

                if (enter <= 0.0 || enter > dirLength)
                {
                    return false;
                }

                var intersection = start + dir * enter;
                var dirFromOrigin = intersection - diskCenter;
                var dirFromOriginLength = math.length(dirFromOrigin);
                if (dirFromOriginLength > diskRadius)
                {
                    intersectionPoints.Add(diskCenter + dirFromOrigin / dirFromOriginLength * diskRadius);
                    return false;
                }

                intersectionPoints.Add(intersection);
                return true;
            }

            #region Implementation of IJob

            public void Execute()
            {
                var open = new NativeQueue<int>(Allocator.Temp);
                var closed = new NativeHashSet<int>(64, Allocator.Temp);

                open.Enqueue(StartTriangle);

                while (open.TryDequeue(out var head))
                {
                    closed.Add(head);
                    var triangle = ConnectivityBuilder.Triangles[head];
                    var triangleVertices = ConnectivityBuilder.TrianglesRaw[head];
                    var a = math.mul(colliderTransform, new float4(ConnectivityBuilder.VerticesRaw[triangleVertices[0]], 1)).xyz;
                    var b = math.mul(colliderTransform, new float4(ConnectivityBuilder.VerticesRaw[triangleVertices[1]], 1)).xyz;
                    var c = math.mul(colliderTransform, new float4(ConnectivityBuilder.VerticesRaw[triangleVertices[2]], 1)).xyz;
                    var trianglePlane = new Plane(a, b, c);

                    bool3 searchFurther = default;

                    for (var i = 0; i < 3; i++)
                    {
                        searchFurther[i] = CheckEdge(
                            trianglePlane,
                            triangleVertices,
                            new MeshConnectivityBuilder.Edge { Triangle = head, Start = i });
                    }

                    for (var i = 0; i < 3; i++)
                    {
                        var currentEdge = i switch
                        {
                            0 => triangle.Edge0,
                            1 => triangle.Edge1,
                            _ => triangle.Edge2
                        };

                        // if no intersections yet, that means starting triangle was not valid, walk until we find one.
                        // This increases the chances we find a finger intersection
                        if (searchFurther[i] || intersectionPoints.Count <= 0)
                        {
                            if (currentEdge.IsValid && closed.Add(currentEdge.Triangle))
                            {
                                open.Enqueue(currentEdge.Triangle);
                            }
                        }
                    }
                }
            }

            #endregion
        }
    }
}