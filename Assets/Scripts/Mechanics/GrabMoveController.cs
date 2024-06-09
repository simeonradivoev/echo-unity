using System;
using System.Collections.Generic;
using System.Linq;
using UnityEcho.Mechanics.Data;
using UnityEcho.Mechanics.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Handles the physics side and raycast checking for grabbing.
    /// <remarks>
    /// Needs to be ran before other scripts
    /// </remarks>
    /// </summary>
    public class GrabMoveController : MonoBehaviour
    {
        [SerializeField]
        private bool _leftHand;

        [SerializeField]
        private LayerMask _penetrationMask;

        /// <summary>
        /// What layers can actually be grabbed.
        /// </summary>
        [SerializeField]
        private LayerMask _grabMask;

        /// <summary>
        /// This is the leeway the hand has to grab stuff. Imagine it as a sphere around the hand so you don't have to perfectly have the hand alighned.
        /// </summary>
        [SerializeField]
        private float _grabRadius = 0.1f;

        /// <summary>
        /// How far into an object are we still allowed to grab it.
        /// </summary>
        [SerializeField]
        private float _grabMaxDepth = 0.2f;

        /// <summary>
        /// This needs to be low so the player can be pushed.
        /// </summary>
        [SerializeField]
        private float _grabbedMassScale = 2;

        /// <summary>
        /// This is for anchored objects like levers, This need to be higher then <see cref="_grabbedMassScale"/> but not by much, just high enough to make lever
        /// pushing more responsive.
        /// </summary>
        [SerializeField]
        private float _grabbedAnchoredMassScale = 10;

        /// <summary>
        /// This needs to be a high value to simulate infinite mass.
        /// This is to prevent objects the player is moving around to also move the player around as it should be in reality, but we don't want that.
        /// </summary>
        [SerializeField]
        private float _idleMassScale = 10000;

        /// <summary>
        /// This is the direction facing the palm inwards
        /// </summary>
        [SerializeField]
        private Vector3 _grabDirection = new(1, 0, 0);

        [SerializeField]
        private Vector3 _forwardDirection = new(0, 0, 1);

        /// <summary>
        /// Don't move the player unless a hand is moving fast enough. This is to allow player to stay in place instead of constantly being pushed around when
        /// releasing grabbed points.
        /// </summary>
        [SerializeField]
        private float _releaseVelocityDampenThreshold = 0.1f;

        /// <summary>
        /// Same as <see cref="_releaseVelocityDampenThreshold"/> but for dynamic objects.
        /// </summary>
        [SerializeField]
        private float _dynamicReleaseVelocityDampenThreshold = 0.05f;

        /// <summary>
        /// When you press the grip button how long after that should a target for grabbing be searched for.
        /// This is used allow for easier grabbing of objects and surfaces.
        /// </summary>
        [SerializeField]
        private float _grabGraceDuration = 0.2f;

        /// <summary>
        /// This joint directly connects the attached static spot and the player body without going through the intermediate hand joint.
        /// </summary>
        [Header("References")]
        [SerializeField]
        private GameObject _staticGrabJointPrefab;

        /// <summary>
        /// Used for dynamic objects that player can move around and be pushed by if they collide with the world.
        /// </summary>
        [SerializeField]
        private GameObject _dynamicGrabJointPrefab;

        /// <summary>
        /// The head transform that is actually moved by the XR head node.
        /// </summary>
        [SerializeField]
        [Header("References")]
        private Transform _head;

        [SerializeField]
        private Transform _headSpace;

        /// <summary>
        /// The root rigidbody of the player.
        /// </summary>
        [SerializeField]
        private Rigidbody _body;

        [SerializeField]
        private HeadFollower _headFollower;

        [SerializeField]
        private InputActionReference _grabActionRef;

        [SerializeField]
        private UnityEvent _onGrab;

        private readonly HashSet<Rigidbody> _dynamicGrabs = new();

        private readonly Collider[] _fallbackOverlapColliders = new Collider[16];

        private readonly List<XRNodeState> _nodes = new();

        private readonly RaycastHit[] _penetrationHits = new RaycastHit[4];

        private GrabbedCollisionEventsConnector _bodyCollisions;

        private bool _connected;

        private MeshConnectivityBuilderHelper _connectivityBuilderHelper;

        private float _grabGraceTime;

        /// <summary>
        /// This is the rigibvody used to attack body to hand and hand to grabbed dynamic object. This has no collider maybe used as intermediate
        /// </summary>
        private Rigidbody _handRigidbody;

        /// <summary>
        /// Raw angular velocity of controller
        /// </summary>
        private Vector3 _rawAngularVelocity;

        /// <summary>
        /// Raw position of controller. This is in local space relative to the origin.
        /// </summary>
        private Vector3 _rawPosition;

        /// <summary>
        /// Raw rotation of controller. This is in local space relative to the origin.
        /// </summary>
        private Quaternion _rawRotation = Quaternion.identity;

        /// <summary>
        /// Raw velocity of controller. This is in local space relative to the origin.
        /// </summary>
        private Vector3 _rawVelocity;

        public float StaticGrabCooldown { get; set; }

        public Vector3 RawClampedWorldPosition { get; set; }

        public GrabDataContainer GrabData { get; } = new();

        public ConfigurableJoint DynamicGrabJoint { get; private set; }

        public GameObject StaticGrabJoint { get; private set; }

        public GameObject VirtualGrab { get; private set; }

        public bool HasGrabbed => StaticGrabJoint || DynamicGrabJoint || VirtualGrab;

        public Transform Head => _head;

        public InputDevice InputDevice => _leftHand ? XRController.leftHand : XRController.rightHand;

        public XRNode HandNode => _leftHand ? XRNode.LeftHand : XRNode.RightHand;

        /// <summary>
        /// This is the direction facing the palm inwards
        /// </summary>
        public Vector3 GrabDirection => _grabDirection;

        /// <summary>
        /// This is the direction the fingers are facing towards
        /// </summary>
        public Vector3 ForwardDirection => _forwardDirection;

        public Quaternion RawRotation => _headSpace.rotation * _rawRotation;

        public Joint HeadToDynamicJoint { get; private set; }

        /// <summary>
        /// Raw position of controller. This is in local space relative to the origin.
        /// </summary>
        public Vector3 RawPosition => _rawPosition;

        private void Start()
        {
            _connectivityBuilderHelper = FindObjectOfType<MeshConnectivityBuilderHelper>();
            _grabActionRef.action.Enable();
            _grabActionRef.action.started += GrabActionStarted;
            _grabActionRef.action.performed += GrabActionCanceledOrPerformed;
            _grabActionRef.action.canceled += GrabActionCanceledOrPerformed;
            _handRigidbody = GetComponent<Rigidbody>();
            HeadToDynamicJoint = GetComponent<Joint>();
            HeadToDynamicJoint.connectedMassScale = _idleMassScale;
            _bodyCollisions = _body.GetComponent<GrabbedCollisionEventsConnector>();
            _rawPosition = (_leftHand ? Vector3.left : Vector3.right) * 0.5f;
            RawClampedWorldPosition = transform.TransformPoint(_rawPosition);
        }

        private void Update()
        {
            GetRawValues();
            UpdateRawValues();

            _grabGraceTime = Mathf.Max(0, _grabGraceTime - Time.deltaTime);

            if (_grabGraceTime > 0)
            {
                ManageGrabbing();
            }

            UpdateGrabbedObjects();
        }

        private void FixedUpdate()
        {
            UpdatePenetration(out GrabData.HadPenetrationHit, out GrabData.PenetrationHit);

            UpdateHandAnchor();

            FixedUpdateGrabbedObjects();
        }

        private void OnDestroy()
        {
            _grabActionRef.action.started -= GrabActionStarted;
            _grabActionRef.action.performed -= GrabActionCanceledOrPerformed;
            _grabActionRef.action.canceled -= GrabActionCanceledOrPerformed;
        }

        private void OnDrawGizmos()
        {
            var rawHandPos = _headSpace.TransformPoint(_rawPosition);
            var rawHandRotation = _headSpace.rotation * _rawRotation;
            var dir = rawHandRotation * _grabDirection;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rawHandPos, _grabRadius);
        }

        private void UpdateGrabbedObjects()
        {
            if (VirtualGrab)
            {
                var rotationOffset = _headSpace.rotation * _rawRotation * GrabData.GrabStartHandRotation;
                var rotation = rotationOffset * GrabData.GrabObjectStartRotation;
                var constrainedHandPos = GrabData.HadPenetrationHit ? GrabData.PenetrationHit.point : _headSpace.TransformPoint(_rawPosition);
                var position = GrabData.GrabHit.Collider.transform.TransformPoint(
                    GrabData.GrabHit.Collider.transform.InverseTransformPoint(constrainedHandPos) - GrabData.GrabHit.LocalPosition);

                if (VirtualGrab.TryGetComponent(out Rigidbody rigidbody))
                {
                    rigidbody.Move(position, rotation);
                }
                else
                {
                    VirtualGrab.transform.SetPositionAndRotation(position, rotation);
                }
            }
        }

        private void FixedUpdateGrabbedObjects()
        {
            GrabData.PlayerBumpImpulse = Vector3.zero;
            GrabData.PlayerBumpPos = Vector3.zero;
            var playerBumpCount = 0;

            // Don't let the player try too hard to go through a wall
            foreach (var collision in _bodyCollisions.Collisions)
            {
                GrabData.PlayerBumpImpulse += collision.impulse;
                playerBumpCount += collision.contacts.Length;
                for (var i = 0; i < collision.contacts.Length; i++)
                {
                    GrabData.PlayerBumpPos += collision.contacts[i].point;
                }
            }

            if (playerBumpCount > 0)
            {
                GrabData.PlayerBumpPos /= playerBumpCount;
            }

            if (StaticGrabJoint)
            {
                var staticGrabJoint = StaticGrabJoint.GetComponent<ConfigurableJoint>();
                staticGrabJoint.connectedAnchor = _headSpace.rotation * _rawPosition;

                var free = StaticGrabCooldown > 0 || playerBumpCount > 0;
                // We generally want locked hinge since that has the most responsiveness.
                // We only want free when collisions happen so we don't glitch out the physics system.
                staticGrabJoint.zMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
                staticGrabJoint.yMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
                staticGrabJoint.xMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;

                if (staticGrabJoint.connectedBody == null && StaticGrabCooldown <= 0)
                {
                    var parent = staticGrabJoint.transform.parent;
                    DestroyImmediate(StaticGrabJoint);

                    // Static grabbing
                    StaticGrabJoint = Instantiate(_staticGrabJointPrefab, GrabData.GrabHit.Position, Quaternion.identity, parent);
                    StaticGrabJoint.name = $"{HandNode} Static Grab Joint";
                    var joint = StaticGrabJoint.GetComponent<ConfigurableJoint>();
                    joint.anchor = StaticGrabJoint.transform.InverseTransformPoint(_headSpace.transform.TransformPoint(_rawPosition));
                    joint.connectedAnchor = _body.transform.InverseTransformPoint(_headSpace.transform.TransformPoint(_rawPosition));
                    joint.connectedBody = _body;
                    // makes it a lot nicer if we stop the player immediately;
                    _body.velocity = Vector3.zero;
                    GrabData.GrabObjectStartRotation = GrabData.GrabHit.Collider.transform.rotation;
                }
            }
            else if (DynamicGrabJoint)
            {
                var collisions = 0;
                GrabData.AveragePos = Vector3.zero;
                var hadKinematicBody = false;
                foreach (var grab in _dynamicGrabs)
                {
                    var connector = grab.GetComponent<GrabbedCollisionEventsConnector>();
                    foreach (var collision in connector.Collisions)
                    {
                        GrabData.PlayerBumpImpulse += collision.impulse;
                        collisions += collision.contacts.Length;
                        for (var i = 0; i < collision.contacts.Length; i++)
                        {
                            GrabData.AveragePos += collision.contacts[i].point;
                        }
                    }
                    hadKinematicBody |= grab.isKinematic;
                }

                var grabObject = DynamicGrabJoint.connectedBody.GetComponent<GrabObject>();

                // Don't let the player try too hard to go through a wall
                if (collisions > 0 || hadKinematicBody || _bodyCollisions.Collisions.Count > 0)
                {
                    HeadToDynamicJoint.connectedMassScale = _grabbedMassScale;
                    if (hadKinematicBody)
                    {
                        // This most likely means a lever.
                        DynamicGrabJoint.connectedMassScale = _grabbedAnchoredMassScale;
                    }
                    else
                    {
                        DynamicGrabJoint.connectedMassScale = _grabbedMassScale;
                    }

                    if (grabObject)
                    {
                        if (grabObject.ObjectMassScaleOverride.overrideState)
                        {
                            DynamicGrabJoint.massScale = grabObject.ObjectMassScaleOverride.value;
                        }

                        if (grabObject.HandMassScaleOverride.overrideState)
                        {
                            DynamicGrabJoint.connectedMassScale = grabObject.HandMassScaleOverride.value;
                        }

                        if (grabObject.PlayerMassScaleOverride.overrideState)
                        {
                            HeadToDynamicJoint.connectedMassScale = grabObject.PlayerMassScaleOverride.value;
                        }
                    }
                }
                else
                {
                    HeadToDynamicJoint.connectedMassScale = _idleMassScale;
                    DynamicGrabJoint.connectedMassScale = _idleMassScale;
                }

                if (collisions > 0)
                {
                    GrabData.AveragePos /= collisions;
                }
            }

            StaticGrabCooldown = Mathf.Max(0, StaticGrabCooldown - Time.deltaTime);
        }

        private void GrabActionStarted(InputAction.CallbackContext obj)
        {
            if (obj.control.device == InputDevice)
            {
                _grabGraceTime = _grabGraceDuration;
            }
        }

        private void GrabActionCanceledOrPerformed(InputAction.CallbackContext obj)
        {
            if (obj.control.device == InputDevice)
            {
                HandleRelease();
            }
        }

        public event Action<GrabHitData, GrabMoveController> Grabbed;

        public event Action<GrabMoveController> Released;

        private void UpdateLocomotionVelocity()
        {
            //TODO: add velocity of held rigidbody if any
            // Since we do fixed joints and even if we don't setting velocity to exact controller velocity makes movement consistent.
            _body.velocity = _headSpace.transform.TransformDirection(-_rawVelocity);
        }

        private void GrabCallback(GameObject obj)
        {
            foreach (var child in obj.GetComponentsInChildren<IGrabHandler>())
            {
                child.OnGrab(new GrabContext { Left = _leftHand });
            }
        }

        private void ReleaseCallback(GameObject obj)
        {
            var context = new ReleaseContext
            {
                Velocity = _headSpace.TransformDirection(_rawVelocity),
                AngularVelocity = _headSpace.TransformDirection(_rawRotation * _rawAngularVelocity),
                Left = _leftHand
            };
            foreach (var child in obj.GetComponentsInChildren<IReleaseHandler>())
            {
                child.OnRelease(context);
            }
        }

        private void HandleRelease()
        {
            if (!HasGrabbed)
            {
                return;
            }

            GrabData.ReleaseTime = Time.time;

            if (StaticGrabJoint)
            {
                Destroy(StaticGrabJoint);
                StaticGrabJoint = null;
                if (_rawVelocity.magnitude <= _releaseVelocityDampenThreshold)
                {
                    //TODO: If the static grab is kinematic take into account its velocity
                    _body.velocity = Vector3.zero;
                }
                else
                {
                    UpdateLocomotionVelocity();
                }

                HeadToDynamicJoint.connectedMassScale = _idleMassScale;
                Released?.Invoke(this);
            }
            else if (DynamicGrabJoint)
            {
                var rootBody = DynamicGrabJoint.connectedBody;
                var grabObject = rootBody.GetComponent<GrabObject>();
                var allOtherControllers = FindObjectsOfType<GrabMoveController>().ToList();
                allOtherControllers.Remove(this);
                Destroy(DynamicGrabJoint);
                DynamicGrabJoint.connectedBody = null;

                var hadKinematicBody = _dynamicGrabs.Any(g => g.isKinematic);

                foreach (var grab in _dynamicGrabs)
                {
                    Destroy(grab.GetComponent<GrabbedCollisionEventsConnector>());

                    var usedElseWhere = false;
                    foreach (var controller in allOtherControllers)
                    {
                        if (controller._dynamicGrabs.Contains(grab))
                        {
                            usedElseWhere = true;
                            break;
                        }
                    }

                    if (usedElseWhere)
                    {
                        break;
                    }

                    // probably not a good idea to always remove the player from exclude layers
                    grab.excludeLayers &= ~LayerMask.GetMask("Player");

                    //TODO: take mass into account
                    //grab.velocity = _body.velocity + _body.transform.TransformDirection(_rawVelocity);
                    //grab.AddTorque(_rawAngularVelocity, ForceMode.VelocityChange);
                }

                if (!rootBody.isKinematic)
                {
                    if (_rawVelocity.magnitude <= _dynamicReleaseVelocityDampenThreshold)
                    {
                        // Make the object stop, this makes it possible to completely stop objects in the air, not physically accurate but feels good.
                        if (grabObject && !grabObject.Virtual)
                        {
                            rootBody.velocity = Vector3.zero;
                            rootBody.angularVelocity = Vector3.zero;
                        }
                        else
                        {
                            rootBody.velocity = _body.velocity;
                            rootBody.angularVelocity = _body.angularVelocity;
                        }
                    }
                    else
                    {
                        rootBody.velocity = _body.velocity;
                        //TODO: Add a system to take into account mass for objects
                        rootBody.AddForce(_headSpace.TransformDirection(_rawVelocity), ForceMode.VelocityChange);
                    }
                }

                //TODO: figure out how to rotate angular velocity

                ReleaseCallback(rootBody.gameObject);
                DynamicGrabJoint = null;
                _dynamicGrabs.Clear();
                HeadToDynamicJoint.connectedMassScale = _idleMassScale;
                Released?.Invoke(this);
            }
            else if (VirtualGrab)
            {
                ReleaseCallback(VirtualGrab.gameObject);
                VirtualGrab = null;
                Released?.Invoke(this);
            }
        }

        private void ManageGrabbing()
        {
            if (HasGrabbed)
            {
                return;
            }

            RaycastNewTarget();

            GrabData.GrabHitTime = Time.time;
            if (!GrabData.GrabHit.Collider)
            {
                return;
            }

            var grabObject = GrabData.GrabHit.Collider.GetComponentInParent<GrabObject>();
            var grabRigidbody = grabObject ? grabObject.GetComponent<Rigidbody>() : GrabData.GrabHit.Collider.attachedRigidbody;

            // Global grab actions
            GrabData.GrabLocalNormal = GrabData.GrabHit.Collider.transform.InverseTransformDirection(GrabData.GrabHit.Normal);
            // We want projected forward to avoid strange angles, this will ensure it's on the triangle plane.
            var projectedForward = Vector3.ProjectOnPlane(
                _headSpace.transform.TransformDirection(_rawRotation * _forwardDirection),
                GrabData.GrabHit.Normal);
            GrabData.GrabLocalForward = GrabData.GrabHit.Collider.transform.InverseTransformDirection(Vector3.Normalize(projectedForward));
            GrabData.GrabStartHandRotation = Quaternion.Inverse(_headSpace.rotation * _rawRotation);

            if (grabRigidbody && !grabRigidbody.isKinematic)
            {
                GrabData.GrabObjectStartRotation = grabRigidbody.rotation;
                DynamicGrabJoint = _handRigidbody.gameObject.AddComponent<ConfigurableJoint>();
                DynamicGrabJoint.CopyJoint(_dynamicGrabJointPrefab.GetComponent<ConfigurableJoint>());
                if (grabObject)
                {
                    var xDrive = DynamicGrabJoint.xDrive;
                    xDrive.maximumForce *= grabObject.HandPositionMotorMultiply;
                    xDrive.positionSpring *= grabObject.HandPositionMotorMultiply;
                    DynamicGrabJoint.xDrive = xDrive;

                    var yDrive = DynamicGrabJoint.yDrive;
                    yDrive.maximumForce *= grabObject.HandPositionMotorMultiply;
                    yDrive.positionSpring *= grabObject.HandPositionMotorMultiply;
                    DynamicGrabJoint.yDrive = yDrive;

                    var zDrive = DynamicGrabJoint.zDrive;
                    zDrive.maximumForce *= grabObject.HandPositionMotorMultiply;
                    zDrive.positionSpring *= grabObject.HandPositionMotorMultiply;
                    DynamicGrabJoint.xDrive = zDrive;

                    var slerpDrive = DynamicGrabJoint.slerpDrive;
                    slerpDrive.maximumForce *= grabObject.HandRotationMotorMultiply;
                    slerpDrive.positionSpring *= grabObject.HandRotationMotorMultiply;
                    DynamicGrabJoint.slerpDrive = zDrive;
                }
                DynamicGrabJoint.connectedBody = grabRigidbody;
                DynamicGrabJoint.connectedAnchor = grabRigidbody.transform.InverseTransformPoint(GrabData.GrabHit.Position);
                if (grabRigidbody.isKinematic)
                {
                    //We want the player to match the velocity of the grabbed object so that it feels better if say they are moving and they grabbed a kinematic object match their velocity;
                    //TODO: probably better if we also handle non kinematic objects that have a high mass for example so they stop the player too
                    _body.velocity = grabRigidbody.velocity;
                }

                GetDynamicGrabs(grabRigidbody);

                var hadKinematicBody = _dynamicGrabs.Any(g => g.isKinematic);

                foreach (var grab in _dynamicGrabs)
                {
                    // having a kinematic body in the chain often means a door or a lever, we don't want the player to pass through that
                    if (!hadKinematicBody)
                    {
                        grab.excludeLayers |= LayerMask.GetMask("Player");
                    }

                    grab.gameObject.AddComponent<GrabbedCollisionEventsConnector>();
                }

                GrabCallback(grabRigidbody.gameObject);
                Grabbed?.Invoke(GrabData.GrabHit, this);
                _onGrab.Invoke();
                return;
            }

            if (grabObject && grabObject.Virtual)
            {
                VirtualGrab = grabObject.gameObject;
                GrabData.GrabObjectStartRotation = grabObject.transform.rotation;
                GrabCallback(VirtualGrab);
            }
            else
            {
                // Static grabbing
                StaticGrabJoint = Instantiate(
                    _staticGrabJointPrefab,
                    GrabData.GrabHit.Position,
                    Quaternion.identity,
                    grabRigidbody ? grabRigidbody.transform : null);
                StaticGrabJoint.name = $"{HandNode} Static Grab Joint";
                var joint = StaticGrabJoint.GetComponent<ConfigurableJoint>();
                joint.anchor = StaticGrabJoint.transform.InverseTransformPoint(_headSpace.transform.TransformPoint(_rawPosition));
                joint.connectedAnchor = _body.transform.InverseTransformPoint(_headSpace.transform.TransformPoint(_rawPosition));
                joint.connectedBody = _body;
                // makes it a lot nicer if we stop the player immediately;
                _body.velocity = Vector3.zero;
                GrabData.GrabObjectStartRotation = GrabData.GrabHit.Collider.transform.rotation;
                _onGrab.Invoke();
            }

            Grabbed?.Invoke(GrabData.GrabHit, this);
        }

        /// <summary>
        /// This just moves the rigidbody hand helper that joints are attached to when grabbing dynamic objects
        /// </summary>
        private void UpdateHandAnchor()
        {
            if (!_connected)
            {
                return;
            }

            if (!HasGrabbed && GrabData.PenetrationHit.collider)
            {
                HeadToDynamicJoint.anchor = _body.transform.InverseTransformPoint(RawClampedWorldPosition);
            }
            else
            {
                HeadToDynamicJoint.anchor = _headSpace.transform.rotation * _rawPosition;
                RawClampedWorldPosition = _headSpace.TransformPoint(_rawPosition);
            }

            _handRigidbody.MoveRotation(_headSpace.transform.rotation * _rawRotation);
            _handRigidbody.velocity = _headSpace.transform.TransformDirection(_rawVelocity);
            _handRigidbody.angularVelocity = _headSpace.transform.TransformDirection(_rawAngularVelocity);
        }

        private void UpdateRawValues()
        {
            if (!_connected)
            {
                return;
            }

            if (!HasGrabbed && GrabData.PenetrationHit.collider)
            {
                var dir = _headSpace.transform.TransformPoint(_rawPosition) - _head.position;
                var dirLength = dir.magnitude;
                dir /= dirLength;
                dir *= Mathf.Min(dirLength, GrabData.PenetrationHit.distance);
                RawClampedWorldPosition = _head.position + dir;
            }
            else
            {
                RawClampedWorldPosition = _headSpace.TransformPoint(_rawPosition);
            }
        }

        private void GetRawValues()
        {
            _connected = false;

            InputTracking.GetNodeStates(_nodes);
            foreach (var node in _nodes)
            {
                if (node.nodeType == HandNode && node.tracked)
                {
                    node.TryGetPosition(out _rawPosition);
                    node.TryGetRotation(out _rawRotation);
                    node.TryGetVelocity(out _rawVelocity);
                    node.TryGetAngularVelocity(out _rawAngularVelocity);
                    _connected = true;
                }
            }
        }

        /// <summary>
        /// Get rigidbodies down the chain and make them not collide with the player.
        /// This is where the <see cref="JointChild"/> is crucial.
        /// </summary>
        /// <param name="parent"></param>
        private void GetDynamicGrabs(Rigidbody parent)
        {
            if (!_dynamicGrabs.Add(parent))
            {
                return;
            }

            var joints = parent.GetComponents<Joint>();
            foreach (var joint in joints)
            {
                if (joint.connectedBody == null)
                {
                    continue;
                }
                GetDynamicGrabs(joint.connectedBody);
            }

            var children = parent.GetComponents<JointChild>();
            foreach (var child in children)
            {
                if (child.Parent == null)
                {
                    continue;
                }
                GetDynamicGrabs(child.Parent);
            }
        }

        private void UpdatePenetration(out bool hasPenetration, out RaycastHit hitData)
        {
            hitData = default;
            hasPenetration = false;
            var grabRadius = _grabRadius;
            var rawHandPos = _headSpace.transform.TransformPoint(_rawPosition);
            var headPos = _head.position;

            var penetrationRay = new Ray(headPos, Vector3.Normalize(rawHandPos - headPos));
            var penetrationCount = Physics.RaycastNonAlloc(
                penetrationRay,
                _penetrationHits,
                Vector3.Magnitude(rawHandPos - headPos) + grabRadius,
                _grabMask);
            for (var i = 0; i < penetrationCount; i++)
            {
                var hit = _penetrationHits[i];
                if (VirtualGrab && (hit.collider.gameObject == VirtualGrab || hit.collider.transform.IsChildOf(VirtualGrab.transform)))
                {
                    continue;
                }

                if (DynamicGrabJoint && DynamicGrabJoint.connectedBody == hit.collider.attachedRigidbody)
                {
                    continue;
                }

                hasPenetration = true;
                hitData = hit;
                break;
            }
        }

        private bool CalculateMeshHit(
            Vector3 worldOrigin,
            int triangleStart,
            Vector3 palmWorldDirection,
            MeshCollider collider,
            out GrabHitData hitData)
        {
            var connectivityBuilder = _connectivityBuilderHelper.GetOrBuild(collider.sharedMesh);
            var origin = collider.transform.InverseTransformPoint(worldOrigin);
            var palmDirection = collider.transform.InverseTransformDirection(palmWorldDirection);

            if (collider.convex || triangleStart < 0)
            {
                //TODO: Optimize triangle search
                triangleStart = connectivityBuilder.FindClosestStartingTriangle(origin, out _, out _);
            }

            if (connectivityBuilder.FindClosestTriangle(
                    origin,
                    palmDirection,
                    triangleStart,
                    _grabRadius,
                    out var closestTriangle,
                    out var closestPoint))
            {
                var triangle = connectivityBuilder.TrianglesRaw[closestTriangle];
                var plane = new Plane(
                    connectivityBuilder.VerticesRaw[triangle[0]],
                    connectivityBuilder.VerticesRaw[triangle[1]],
                    connectivityBuilder.VerticesRaw[triangle[2]]);

                hitData = new GrabHitData
                {
                    Position = collider.transform.TransformPoint(closestPoint),
                    LocalPosition = closestPoint,
                    Collider = collider,
                    Normal = collider.transform.TransformDirection(plane.normal),
                    Up = palmWorldDirection,
                    Triangle = closestTriangle
                };
                return true;
            }

            hitData = new GrabHitData { Triangle = triangleStart };
            return false;
        }

        private void RaycastNewTarget()
        {
            var grabRadius = _grabRadius;
            var rawHandPos = _headSpace.transform.TransformPoint(_rawPosition);
            var rawHandRotation = _headSpace.rotation * _rawRotation;
            var palmDir = rawHandRotation * _grabDirection;

            GrabData.GrabHit = default;

            Collider hitCollider = null;
            var hitNormal = Vector3.up;
            var hitPoint = rawHandPos;
            var hitTriangle = -1;

            var handConstrained = rawHandPos;
            if (GrabData.HadPenetrationHit)
            {
                hitCollider = GrabData.PenetrationHit.collider;
                hitNormal = GrabData.PenetrationHit.normal;
                hitPoint = GrabData.PenetrationHit.point;
                hitTriangle = GrabData.PenetrationHit.triangleIndex;
                handConstrained = GrabData.PenetrationHit.point;
            }
            else
            {
                if (Physics.SphereCast(new Ray(rawHandPos - _grabRadius * palmDir, palmDir), _grabRadius, out var hit, grabRadius, _grabMask))
                {
                    hitCollider = hit.collider;
                    hitNormal = hit.normal;
                    hitPoint = hit.point;
                    hitTriangle = hit.triangleIndex;
                }
                else
                {
                    // hand might be inside a mesh fallback to slow manual triangle search
                    var hits = Physics.OverlapSphereNonAlloc(rawHandPos, _grabRadius, _fallbackOverlapColliders, _grabMask);
                    var closestColliderDistance = float.PositiveInfinity;
                    for (var i = 0; i < hits; i++)
                    {
                        if (_fallbackOverlapColliders[i] is MeshCollider meshCollider)
                        {
                            var connectivityBuilder = _connectivityBuilderHelper.GetOrBuild(meshCollider.sharedMesh);
                            var origin = meshCollider.transform.InverseTransformPoint(handConstrained);
                            var triangleStart = connectivityBuilder.FindClosestStartingTriangle(
                                origin,
                                out var closestTrianglePoint,
                                out var closestTriangleNormal);
                            var worldClosestTrianglePoint = meshCollider.transform.TransformPoint(closestTrianglePoint);
                            var distanceSq = Vector3.SqrMagnitude((Vector3)closestTrianglePoint - worldClosestTrianglePoint);
                            if (distanceSq < closestColliderDistance)
                            {
                                closestColliderDistance = distanceSq;
                                hitCollider = meshCollider;
                                hitNormal = closestTriangleNormal;
                                hitPoint = worldClosestTrianglePoint;
                                hitTriangle = triangleStart;
                            }
                        }
                        else
                        {
                            var closestPointOnCollider = _fallbackOverlapColliders[i].ClosestPoint(handConstrained);
                            var distanceSq = Vector3.SqrMagnitude(closestPointOnCollider - handConstrained);
                            if (distanceSq < closestColliderDistance)
                            {
                                closestColliderDistance = distanceSq;
                                hitCollider = _fallbackOverlapColliders[i];
                                hitNormal = Vector3.Cross((closestPointOnCollider - handConstrained) / Mathf.Sqrt(distanceSq), Vector3.up);
                                hitPoint = closestPointOnCollider;
                                hitTriangle = -1;
                            }
                        }
                    }
                }
            }

            if (hitCollider)
            {
                GrabHitData closestPoint;
                if (hitCollider is MeshCollider meshCollider)
                {
                    if (!CalculateMeshHit(hitPoint, hitTriangle, palmDir, meshCollider, out closestPoint))
                    {
                        // Use original hit since we didn't find one on mesh
                        closestPoint = new GrabHitData
                        {
                            Position = hitPoint,
                            LocalPosition = meshCollider.transform.InverseTransformPoint(hitPoint),
                            Normal = hitNormal,
                            Collider = hitCollider,
                            Up = rawHandRotation * Vector3.forward,
                            Triangle = closestPoint.Triangle
                        };
                    }
                }
                else
                {
                    closestPoint = new GrabHitData
                    {
                        Position = hitPoint,
                        LocalPosition = hitCollider.transform.InverseTransformPoint(hitPoint),
                        Normal = hitNormal,
                        Collider = hitCollider,
                        Up = rawHandRotation * Vector3.forward,
                        Triangle = hitTriangle
                    };
                }

                GrabData.GrabHit = closestPoint;
            }
        }

        public class GrabDataContainer
        {
            public Vector3 PlayerBumpImpulse;

            public Vector3 PlayerBumpPos;

            public Vector3 AveragePos;

            public float GrabHitTime;

            public float ReleaseTime;

            public GrabHitData GrabHit;

            public RaycastHit PenetrationHit;

            public bool HadPenetrationHit;

            public Vector3 GrabLocalNormal { get; set; }

            public Vector3 GrabLocalForward { get; set; }

            public Quaternion GrabObjectStartRotation { get; set; }

            /// <summary>
            /// The XR origin relative hand rotation start.
            /// </summary>
            public Quaternion GrabStartHandRotation { get; set; }
        }
    }
}