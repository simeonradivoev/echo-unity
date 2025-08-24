using System;
using System.Collections.Generic;
using System.Linq;
using UnityEcho.Mechanics.Data;
using UnityEcho.Mechanics.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR;

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

        [SerializeField]
        private float _grabbedObjectMassScale = 2;

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
        /// Should dynamic objects not have their velocity set to 0 if they are released.
        /// This is physically accurate but makes it very hard to suspend object in air without them drifting.
        /// </summary>
        [FormerlySerializedAs("_physicallyAccurateDynamicRelease")]
        [SerializeField]
        private bool _physicallyAccurateRelease;

        /// <summary>
        /// This joint directly connects the attached static spot and the player body without going through the intermediate hand joint.
        /// </summary>
        [Header("References")]
        [SerializeField]
        private PlayerReferences _playerRefs;

        [SerializeField]
        private GameObject _staticGrabJointPrefab;

        /// <summary>
        /// Used for dynamic objects that player can move around and be pushed by if they collide with the world.
        /// </summary>
        [SerializeField]
        private GameObject _dynamicGrabJointPrefab;

        [SerializeField]
        private ConfigurableJoint _headToDynamicJointPrefab;

        [SerializeField]
        private HeadFollower _headFollower;

        [SerializeField]
        private Transform _forward;

        [SerializeField]
        private InputActionReference _grabActionRef;

        [SerializeField]
        private AnimationCurve _releaseVelocityMultiplier;

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

        private bool _hasDynamicGrab;

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

        private Rigidbody _rootDynamicGrabBody;

        private Quaternion _staticGrabHandOffset;

        private Quaternion _staticGrabJointRotation;

        private Quaternion _staticGrabRotation;

        private Quaternion _staticGrabWorldHandRotation;

        public Vector3 HandForward => _playerRefs.HeadSpace.transform.rotation * _rawRotation * _forward.localRotation * Vector3.forward;

        public float StaticGrabCooldown { get; set; }

        public Vector3 RawClampedWorldPosition { get; set; }

        public GrabDataContainer GrabData { get; } = new();

        public ConfigurableJoint DynamicGrabJoint { get; private set; }

        public ConfigurableJoint StaticGrabJoint { get; private set; }

        public bool Secondary { get; set; }

        public GameObject VirtualGrab { get; private set; }

        public bool HasGrabbed => StaticGrabJoint || DynamicGrabJoint || VirtualGrab;

        public Transform Head => _playerRefs.Head;

        public XRNode HandNode => _leftHand ? XRNode.LeftHand : XRNode.RightHand;

        /// <summary>
        /// This is the direction facing the palm inwards
        /// </summary>
        public Vector3 GrabDirection => _grabDirection;

        /// <summary>
        /// This is the direction the fingers are facing towards
        /// </summary>
        public Vector3 ForwardDirection => _forwardDirection;

        public Quaternion RawRotation => _playerRefs.HeadSpace.rotation * _rawRotation;

        public ConfigurableJoint HeadToDynamicJoint { get; private set; }

        /// <summary>
        /// Raw position of controller. This is in local space relative to the origin.
        /// </summary>
        public Vector3 RawPosition => _rawPosition;

        /// <summary>
        /// Should objects not have their velocity set to 0 if they are released.
        /// This includes the player when they release from a static grab.
        /// This is physically accurate but makes it very hard to suspend object in air without them drifting.
        /// </summary>
        public bool PhysicallyAccurateRelease { get; private set; }

        private void Awake()
        {
            _handRigidbody = GetComponent<Rigidbody>();
            _bodyCollisions = _playerRefs.Body.GetComponent<GrabbedCollisionEventsConnector>();
            PhysicallyAccurateRelease = _physicallyAccurateRelease;
        }

        private void Start()
        {
            _connectivityBuilderHelper = FindObjectOfType<MeshConnectivityBuilderHelper>();
            _grabActionRef.action.Enable();
            _grabActionRef.action.started += GrabActionStarted;
            _grabActionRef.action.performed += GrabActionCanceledOrPerformed;
            _grabActionRef.action.canceled += GrabActionCanceledOrPerformed;

            _rawPosition = (_leftHand ? Vector3.left : Vector3.right) * 0.5f;
            RawClampedWorldPosition = transform.TransformPoint(_rawPosition);

            HeadToDynamicJoint = _handRigidbody.gameObject.AddComponent<ConfigurableJoint>();
            HeadToDynamicJoint.CopyJoint(_headToDynamicJointPrefab);
            HeadToDynamicJoint.connectedBody = _playerRefs.Body;
            HeadToDynamicJoint.connectedMassScale = _idleMassScale;
            UpdateHandAnchor();
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
            var rawHandPos = _playerRefs.HeadSpace.TransformPoint(_rawPosition);
            var rawHandRotation = _playerRefs.HeadSpace.rotation * _rawRotation;
            var dir = rawHandRotation * _grabDirection;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(rawHandPos, _grabRadius);
        }

        public void SetPhysicallyAccurateRelease(bool enabled)
        {
            PhysicallyAccurateRelease = enabled;
            if (HeadToDynamicJoint)
            {
                HeadToDynamicJoint.connectedMassScale = _idleMassScale;
            }
        }

        private void UpdateGrabbedObjects()
        {
            if (VirtualGrab)
            {
                var rotationOffset = _playerRefs.HeadSpace.rotation * _rawRotation * GrabData.GrabStartHandRotationInverse;
                var rotation = rotationOffset * GrabData.GrabObjectStartRotation;
                var constrainedHandPos =
                    GrabData.HadPenetrationHit ? GrabData.PenetrationHit.point : _playerRefs.HeadSpace.TransformPoint(_rawPosition);
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

            if (StaticGrabJoint)
            {
                var attachedBody = StaticGrabJoint.GetComponent<Rigidbody>();
                StaticGrabJoint.connectedAnchor =
                    _playerRefs.Body.transform.InverseTransformPoint(_playerRefs.HeadSpace.transform.TransformPoint(_rawPosition));

                if (attachedBody && attachedBody.isKinematic)
                {
                    // For kinematic objects, we need to update the axis to account for the object's rotation
                    // Calculate how much the object has rotated since grab started
                    var rotationDelta = attachedBody.rotation * Quaternion.Inverse(GrabData.GrabObjectStartRotation);

                    // Calculate target rotation using the original coordinate system approach
                    // but account for the kinematic object's rotation
                    var currentHandWorldRotation = Quaternion.Inverse(rotationDelta) * _playerRefs.HeadSpace.rotation * _rawRotation;

                    // Use the original joint rotation reference (not rotated)
                    var currentHandInJointSpace = Quaternion.Inverse(rotationDelta * _staticGrabJointRotation) * currentHandWorldRotation;

                    // Apply the rotation delta to the hand offset to account for object rotation
                    _staticGrabHandOffset = Quaternion.Inverse(rotationDelta * _staticGrabJointRotation) * _staticGrabWorldHandRotation;

                    var targetRot = Quaternion.Inverse(_staticGrabHandOffset) * currentHandInJointSpace;
                    var correctedRot = new Quaternion(-targetRot.x, targetRot.y, targetRot.z, targetRot.w);

                    // Apply the rotation delta to the final target rotation
                    StaticGrabJoint.targetRotation = correctedRot;
                }
                else
                {
                    // Original logic for static objects
                    _staticGrabRotation = attachedBody ? attachedBody.rotation : Quaternion.identity;

                    var currentHandWorldRotation = _playerRefs.HeadSpace.rotation * _rawRotation;
                    _staticGrabHandOffset = Quaternion.Inverse(_staticGrabJointRotation) * _staticGrabWorldHandRotation;
                    var currentHandInJointSpace = Quaternion.Inverse(_staticGrabJointRotation) * currentHandWorldRotation;

                    StaticGrabJoint.axis = Quaternion.Inverse(StaticGrabJoint.transform.rotation) * GrabData.Axis;
                    StaticGrabJoint.secondaryAxis = Quaternion.Inverse(StaticGrabJoint.transform.rotation) * GrabData.SecondaryAxis;

                    var targetRot = Quaternion.Inverse(_staticGrabHandOffset) * currentHandInJointSpace;
                    var correctedRot = new Quaternion(-targetRot.x, targetRot.y, targetRot.z, targetRot.w);
                    StaticGrabJoint.targetRotation = correctedRot;
                }

                var free = Secondary || StaticGrabCooldown > 0 || playerBumpCount > 0;
                // We generally want locked hinge since that has the most responsiveness.
                // We only want free when collisions happen so we don't glitch out the physics system.
                StaticGrabJoint.zMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
                StaticGrabJoint.yMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
                StaticGrabJoint.xMotion = free ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;

                if (StaticGrabJoint.connectedBody == null && StaticGrabCooldown <= 0)
                {
                    var parent = StaticGrabJoint.transform.parent;
                    DestroyImmediate(StaticGrabJoint);

                    // Static grabbing
                    StaticGrabJoint = Instantiate(_staticGrabJointPrefab, GrabData.GrabHit.Position, Quaternion.identity, parent)
                        .GetComponent<ConfigurableJoint>();
                    StaticGrabJoint.name = $"{HandNode} Static Grab Joint";
                    var joint = StaticGrabJoint.GetComponent<ConfigurableJoint>();
                    joint.anchor = StaticGrabJoint.transform.InverseTransformPoint(_playerRefs.HeadSpace.transform.TransformPoint(_rawPosition));
                    joint.connectedAnchor =
                        _playerRefs.Body.transform.InverseTransformPoint(_playerRefs.HeadSpace.transform.TransformPoint(_rawPosition));
                    joint.connectedBody = _playerRefs.Body;
                    // makes it a lot nicer if we stop the player immediately;
                    _playerRefs.Body.velocity = Vector3.zero;
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

                var grabbedAnchoredMassScale = _grabbedAnchoredMassScale;
                var idleMassScale = _idleMassScale;

                // Don't let the player try too hard to go through a wall
                if (collisions > 0 || hadKinematicBody || _bodyCollisions.Collisions.Count > 0)
                {
                    HeadToDynamicJoint.connectedMassScale = _grabbedMassScale;
                    if (hadKinematicBody)
                    {
                        // This most likely means a lever.
                        DynamicGrabJoint.connectedMassScale = grabbedAnchoredMassScale;
                    }
                    else
                    {
                        DynamicGrabJoint.connectedMassScale = _grabbedObjectMassScale;
                    }
                }
                else
                {
                    HeadToDynamicJoint.connectedMassScale = idleMassScale;
                    DynamicGrabJoint.connectedMassScale = idleMassScale;
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

                if (collisions > 0)
                {
                    GrabData.AveragePos /= collisions;
                }

                var rawWorldRotation = _playerRefs.HeadSpace.rotation * _rawRotation;
                DynamicGrabJoint.targetRotation = rawWorldRotation * GrabData.GrabStartHandRotationInverse;
            }
            else if (_hasDynamicGrab)
            {
                // Dynamic grab was broken
                HandleBrokenDynamicJoint();
            }

            StaticGrabCooldown = Mathf.Max(0, StaticGrabCooldown - Time.deltaTime);

            if (playerBumpCount > 0)
            {
                GrabData.PlayerBumpPos /= playerBumpCount;
            }
        }

        private void GrabActionStarted(InputAction.CallbackContext obj)
        {
            _grabGraceTime = _grabGraceDuration;
        }

        private void GrabActionCanceledOrPerformed(InputAction.CallbackContext obj)
        {
            HandleRelease();
        }

        public event Action<GrabHitData, GrabMoveController> Grabbed;

        public event Action<GrabMoveController> Released;

        private void UpdateStaticLocomotionVelocity(Rigidbody attachedRigidbody)
        {
            // Since we do fixed joints and even if we don't setting velocity to exact controller velocity makes movement consistent.
            _playerRefs.Body.velocity = Vector3.zero;
            var pushBack = _playerRefs.HeadSpace.transform.TransformDirection(-_rawVelocity);
            _playerRefs.Body.AddForceAtPosition(pushBack, _handRigidbody.transform.position, ForceMode.VelocityChange);
            if (attachedRigidbody)
            {
                _playerRefs.Body.AddForce(attachedRigidbody.velocity, ForceMode.VelocityChange);
            }
        }

        private void UpdateDynamicLocomotionVelocity(Rigidbody attachedRigidbody)
        {
            // Since we do fixed joints and even if we don't setting velocity to exact controller velocity makes movement consistent.
            _playerRefs.Body.velocity = Vector3.zero;
            var pushBack = _playerRefs.HeadSpace.transform.TransformDirection(-_rawVelocity) *
                           Mathf.Clamp01(attachedRigidbody.mass / _playerRefs.Body.mass);
            _playerRefs.Body.AddForceAtPosition(pushBack, _handRigidbody.transform.position, ForceMode.VelocityChange);
            if (attachedRigidbody)
            {
                _playerRefs.Body.AddForce(attachedRigidbody.velocity, ForceMode.VelocityChange);
            }
        }

        private void GrabCallback(GameObject obj, Vector3 position)
        {
            foreach (var child in obj.GetComponentsInChildren<IGrabHandler>())
            {
                child.OnGrab(new GrabContext { Left = _leftHand, Position = position });
            }
        }

        private void ReleaseCallback(GameObject obj)
        {
            var context = new ReleaseContext
            {
                Velocity = _playerRefs.HeadSpace.TransformDirection(_rawVelocity),
                AngularVelocity = _playerRefs.HeadSpace.TransformDirection(_rawRotation * _rawAngularVelocity),
                Left = _leftHand
            };
            foreach (var child in obj.GetComponentsInChildren<IReleaseHandler>())
            {
                child.OnRelease(context);
            }
        }

        private void EndDynamicGrab()
        {
            ReleaseCallback(_rootDynamicGrabBody.gameObject);
            DynamicGrabJoint = null;
            _rootDynamicGrabBody = null;
            _dynamicGrabs.Clear();
            HeadToDynamicJoint.connectedMassScale = _idleMassScale;
            Released?.Invoke(this);
            _hasDynamicGrab = false;
        }

        private void HandleBrokenDynamicJoint()
        {
            ClearDynamicGrabs();
            EndDynamicGrab();
        }

        private void ClearDynamicGrabs()
        {
            var allOtherControllers = FindObjectsOfType<GrabMoveController>().ToList();
            allOtherControllers.Remove(this);

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
                var attachedRigidBody = StaticGrabJoint.GetComponent<Rigidbody>();
                if (GrabData.GrabHit.Collider.attachedRigidbody && GrabData.GrabHit.Collider.attachedRigidbody.isKinematic)
                {
                    Destroy(StaticGrabJoint);
                    ReleaseCallback(GrabData.GrabHit.Collider.attachedRigidbody.gameObject);
                }
                else
                {
                    Destroy(StaticGrabJoint.gameObject);
                    ReleaseCallback(GrabData.GrabHit.Collider.gameObject);
                }

                StaticGrabJoint = null;
                if (!PhysicallyAccurateRelease &&
                    _rawVelocity.magnitude <= _releaseVelocityDampenThreshold &&
                    (!attachedRigidBody || attachedRigidBody.velocity.magnitude <= _releaseVelocityDampenThreshold))
                {
                    //TODO: If the static grab is kinematic take into account its velocity
                    _playerRefs.Body.velocity = Vector3.zero;
                }
                else
                {
                    UpdateStaticLocomotionVelocity(attachedRigidBody);
                }

                HeadToDynamicJoint.connectedMassScale = _idleMassScale;
                Released?.Invoke(this);
            }
            else if (DynamicGrabJoint)
            {
                var rootBody = DynamicGrabJoint.connectedBody;
                var grabObject = rootBody.GetComponent<GrabObject>();

                Destroy(DynamicGrabJoint);
                DynamicGrabJoint.connectedBody = null;

                ClearDynamicGrabs();

                if (!rootBody.isKinematic)
                {
                    if (!PhysicallyAccurateRelease && _rawVelocity.magnitude <= _dynamicReleaseVelocityDampenThreshold)
                    {
                        // Make the object stop, this makes it possible to completely stop objects in the air, not physically accurate but feels good.
                        if (grabObject && !grabObject.Virtual)
                        {
                            rootBody.velocity = Vector3.zero;
                            rootBody.angularVelocity = Vector3.zero;
                        }
                        else
                        {
                            rootBody.velocity = _playerRefs.Body.velocity;
                            rootBody.angularVelocity = _playerRefs.Body.angularVelocity;
                        }
                    }
                    else
                    {
                        rootBody.velocity = _playerRefs.Body.velocity;
                        // Have more controlled force, since we want only big objects to hard to push, others we want 1 to 1 with hand velocity.
                        var force = _releaseVelocityMultiplier.Evaluate(rootBody.mass);
                        rootBody.AddForce(_playerRefs.HeadSpace.TransformDirection(_rawVelocity) * force, ForceMode.VelocityChange);
                    }

                    UpdateDynamicLocomotionVelocity(rootBody);
                }
                else
                {
                    UpdateStaticLocomotionVelocity(rootBody);
                }

                //TODO: figure out how to rotate angular velocity

                EndDynamicGrab();
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
                _playerRefs.HeadSpace.transform.TransformDirection(_rawRotation * _forwardDirection),
                GrabData.GrabHit.Normal);
            GrabData.GrabLocalForward = GrabData.GrabHit.Collider.transform.InverseTransformDirection(Vector3.Normalize(projectedForward));
            GrabData.GrabStartHandRotationInverse = Quaternion.Inverse(_playerRefs.HeadSpace.rotation * _rawRotation);
            GrabData.Rigidbody = grabRigidbody;

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

                var bodyRotation = Quaternion.Inverse(_playerRefs.HeadSpace.transform.rotation);
                DynamicGrabJoint.axis = Vector3.right;
                DynamicGrabJoint.secondaryAxis = Vector3.up;

                _hasDynamicGrab = true;
                _rootDynamicGrabBody = grabRigidbody;
                DynamicGrabJoint.connectedBody = grabRigidbody;
                DynamicGrabJoint.connectedAnchor = grabRigidbody.transform.InverseTransformPoint(GrabData.GrabHit.Position);
                if (grabRigidbody.isKinematic)
                {
                    //We want the player to match the velocity of the grabbed object so that it feels better if say they are moving and they grabbed a kinematic object match their velocity;
                    //TODO: probably better if we also handle non kinematic objects that have a high mass for example so they stop the player too
                    _playerRefs.Body.velocity = grabRigidbody.velocity;
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

                GrabCallback(grabRigidbody.gameObject, GrabData.GrabHit.Position);
                Grabbed?.Invoke(GrabData.GrabHit, this);
                _onGrab.Invoke();
                return;
            }

            if (grabObject && grabObject.Virtual)
            {
                VirtualGrab = grabObject.gameObject;
                GrabData.GrabObjectStartRotation = grabObject.transform.rotation;
                GrabCallback(VirtualGrab, GrabData.GrabHit.Position);
            }
            else
            {
                if (grabRigidbody && grabRigidbody.isKinematic)
                {
                    // Static grab to kinematic objects, straight joint to it.
                    StaticGrabJoint = grabRigidbody.gameObject.AddComponent<ConfigurableJoint>();
                    StaticGrabJoint.CopyJoint(_staticGrabJointPrefab.GetComponent<ConfigurableJoint>());
                    GrabCallback(grabRigidbody.gameObject, GrabData.GrabHit.Position);
                }
                else
                {
                    // Static grabbing to surfaces no rigidbody involved
                    StaticGrabJoint = Instantiate(_staticGrabJointPrefab, GrabData.GrabHit.Position, Quaternion.identity)
                        .GetComponent<ConfigurableJoint>();
                    GrabCallback(GrabData.GrabHit.Collider.gameObject, GrabData.GrabHit.Position);
                    StaticGrabJoint.name = $"{HandNode} Static Grab Joint";
                }

                StaticGrabJoint.anchor =
                    StaticGrabJoint.transform.InverseTransformPoint(_playerRefs.HeadSpace.transform.TransformPoint(_rawPosition));
                StaticGrabJoint.connectedAnchor =
                    _playerRefs.Body.transform.InverseTransformPoint(_playerRefs.HeadSpace.transform.TransformPoint(_rawPosition));
                StaticGrabJoint.connectedBody = _playerRefs.Body;

                var axis = _playerRefs.HeadSpace.TransformDirection(_rawRotation * Vector3.right);
                var secondaryAxis = _playerRefs.HeadSpace.TransformDirection(_rawRotation * Vector3.down);
                GrabData.Axis = axis;
                GrabData.SecondaryAxis = secondaryAxis;
                StaticGrabJoint.axis = Quaternion.Inverse(StaticGrabJoint.transform.rotation) * axis;
                StaticGrabJoint.secondaryAxis = Quaternion.Inverse(StaticGrabJoint.transform.rotation) * secondaryAxis;

                // Store the joint's coordinate system (created from normal and up)
                _staticGrabJointRotation = Quaternion.LookRotation(axis, secondaryAxis);

                // Calculate the hand's rotation relative to the joint's coordinate system
                _staticGrabWorldHandRotation = _playerRefs.HeadSpace.rotation * _rawRotation;

                // makes it a lot nicer if we stop the player immediately;
                _playerRefs.Body.velocity = Vector3.zero;
                GrabData.GrabObjectStartRotation = StaticGrabJoint.transform.rotation;

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
                HeadToDynamicJoint.targetPosition = _playerRefs.HeadSpace.localRotation * _rawPosition;
            }
            else
            {
                HeadToDynamicJoint.targetPosition = _playerRefs.HeadSpace.localRotation * _rawPosition;
                RawClampedWorldPosition = _playerRefs.HeadSpace.TransformPoint(_rawPosition);
            }

            //_handRigidbody.MoveRotation(_playerRefs.HeadSpace.transform.rotation * _rawRotation);
            _handRigidbody.velocity = _playerRefs.HeadSpace.transform.TransformDirection(_rawVelocity);
            _handRigidbody.angularVelocity = _playerRefs.HeadSpace.transform.TransformDirection(_rawAngularVelocity);
        }

        private void UpdateRawValues()
        {
            if (!_connected)
            {
                return;
            }

            // This is to avoid hands going through walls based on your head position.
            // We don't want to be able to grab stuff beyond a wall you can't see.
            if (!HasGrabbed && GrabData.PenetrationHit.collider)
            {
                var headPos = _playerRefs.Head.position;
                var dir = _playerRefs.HeadSpace.transform.TransformPoint(_rawPosition) - headPos;
                var dirLength = dir.magnitude;
                dir /= dirLength;
                dir *= Mathf.Min(dirLength, GrabData.PenetrationHit.distance);
                RawClampedWorldPosition = headPos + dir;
            }
            else
            {
                RawClampedWorldPosition = _playerRefs.HeadSpace.TransformPoint(_rawPosition);
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
            var rawHandPos = _playerRefs.HeadSpace.transform.TransformPoint(_rawPosition);
            var headPos = _playerRefs.Head.position;

            var penetrationRay = new Ray(headPos, Vector3.Normalize(rawHandPos - headPos));
            var penetrationCount = Physics.RaycastNonAlloc(
                penetrationRay,
                _penetrationHits,
                Vector3.Magnitude(rawHandPos - headPos) + grabRadius,
                _grabMask,
                QueryTriggerInteraction.Ignore);
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
            Vector3 palmWorldUp,
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
                var normal = collider.transform.TransformDirection(plane.normal);

                hitData = new GrabHitData
                {
                    Position = collider.transform.TransformPoint(closestPoint),
                    LocalPosition = closestPoint,
                    Collider = collider,
                    Normal = normal,
                    Up = Vector3.ProjectOnPlane(palmWorldUp, normal).normalized,
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
            var rawHandPos = _playerRefs.HeadSpace.transform.TransformPoint(_rawPosition);
            var rawHandRotation = _playerRefs.HeadSpace.rotation * _rawRotation;
            var palmDir = rawHandRotation * _grabDirection;
            var palmUp = rawHandRotation * Vector3.Cross(_grabDirection, _forwardDirection);

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
                if (Physics.SphereCast(
                        new Ray(rawHandPos - _grabRadius * palmDir, palmDir),
                        _grabRadius,
                        out var hit,
                        grabRadius,
                        _grabMask,
                        QueryTriggerInteraction.Ignore))
                {
                    hitCollider = hit.collider;
                    hitNormal = hit.normal;
                    hitPoint = hit.point;
                    hitTriangle = hit.triangleIndex;
                }
                else
                {
                    // hand might be inside a mesh fallback to slow manual triangle search
                    var hits = Physics.OverlapSphereNonAlloc(
                        rawHandPos,
                        _grabRadius,
                        _fallbackOverlapColliders,
                        _grabMask,
                        QueryTriggerInteraction.Ignore);
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
                    if (!CalculateMeshHit(hitPoint, hitTriangle, palmDir, palmUp, meshCollider, out closestPoint))
                    {
                        // Use original hit since we didn't find one on mesh
                        closestPoint = new GrabHitData
                        {
                            Position = hitPoint,
                            LocalPosition = meshCollider.transform.InverseTransformPoint(hitPoint),
                            Normal = hitNormal,
                            Collider = hitCollider,
                            Up = Vector3.ProjectOnPlane(rawHandRotation * Vector3.up, hitNormal).normalized,
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
                        Up = Vector3.ProjectOnPlane(rawHandRotation * Vector3.up, hitNormal).normalized,
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

            public Vector3 Axis;

            public Vector3 SecondaryAxis;

            public Vector3 GrabLocalNormal { get; set; }

            public Vector3 GrabLocalForward { get; set; }

            /// <summary>
            /// The starting rotation of object grabbed, in world space
            /// </summary>
            public Quaternion GrabObjectStartRotation { get; set; }

            /// <summary>
            /// The Inverse of the XR origin relative hand rotation.
            /// </summary>
            public Quaternion GrabStartHandRotationInverse { get; set; }

            public Rigidbody Rigidbody { get; set; }
        }
    }
}