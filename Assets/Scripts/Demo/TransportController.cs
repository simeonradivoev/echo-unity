using Mechanics.Utils;
using System.Linq;
using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.XR.Haptics;
using QuaternionUtil = UnityEcho.Utils.QuaternionUtil;

namespace UnityEcho.Demo
{
    public class TransportController : MonoBehaviour
    {
        [SerializeField]
        private Transform[] _waypoints;

        [SerializeField]
        private AnimationCurve _speedCurve;

        [SerializeField]
        private float _speed;

        [SerializeField]
        private float _speedLevel;

        [SerializeField]
        private float _speedLevelSmoothingSpeed;

        [SerializeField]
        private float _turnSpeed;

        [SerializeField]
        private float _turnAcceleration;

        [SerializeField]
        private float _delay;

        [SerializeField]
        private Renderer[] _thrusters;

        [SerializeField]
        private Vector3 _thrusterSize;

        [SerializeField]
        public float _velocityScaleFactor = 0.1f;

        [SerializeField]
        public float _angularScaleFactor = 0.1f;

        [SerializeField]
        private AudioSource _thrusterSound;

        [SerializeField]
        private float _thrusterSoundVolume;

        [SerializeField]
        private float _hapticVelocityMultiply;

        private Vector3 _acceleration;

        private Vector3 _angularAcceleration;

        private Vector3 _angularVelocity;

        private Vector3 _angularVelocityDeriv;

        private int _currentTargetIndex = 1;

        private GrabHelper _grabHelper;

        private GrabObject _grabObject;

        private Vector3 _lastAngularVelocity;

        private Vector3 _lastVelocity;

        private float _maxScaleSmooth;

        private float _maxScaleSmoothVel;

        private float _progress;

        private Rigidbody _rigidbody;

        private Quaternion _rotDeriv;

        private float _rotVel;

        private float _speedSmoothingDeriv;

        private float _startTime;

        private float[] _thrusterWeights;

        private float[] _thrusterWeightsVel;

        public float SpeedLevel
        {
            get => _speedLevel;
            set => _speedLevel = value;
        }

        public Vector3 Velocity => _rigidbody.velocity;

        public float SpeedLevelSmooth { get; private set; }

        private void Awake()
        {
            _thrusterSound.Pause();
        }

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _startTime = Time.time + _delay;
            _rigidbody.MovePosition(_waypoints.First().position);
            _grabHelper = GetComponent<GrabHelper>();
            _grabObject = GetComponent<GrabObject>();
            _thrusterWeights = new float[_thrusters.Length];
            _thrusterWeightsVel = new float[_thrusters.Length];
        }

        private void Update()
        {
            SpeedLevelSmooth = Mathf.SmoothDamp(SpeedLevelSmooth, _speedLevel, ref _speedSmoothingDeriv, Time.deltaTime, _speedLevelSmoothingSpeed);

            var maxScale = 0f;
            var averagePosition = Vector3.zero;
            var activeCount = 0f;

            for (var i = 0; i < _thrusters.Length; i++)
            {
                var thruster = _thrusters[i];
                var scale = CalculateThrusterScale(thruster.transform, _acceleration, _angularAcceleration, _rigidbody.worldCenterOfMass);
                _thrusterWeights[i] = Mathf.SmoothDamp(_thrusterWeights[i], scale, ref _thrusterWeightsVel[i], Time.deltaTime * 10);
                var isActive = !Mathf.Approximately(_thrusterWeights[i], 0);
                if (isActive)
                {
                    averagePosition += transform.position * _thrusterWeights[i];
                    activeCount++;
                }
                maxScale = Mathf.Max(maxScale, _thrusterWeights[i]);

                var localScale = _thrusterSize;
                localScale.y *= _thrusterWeights[i];

                thruster.transform.localScale = localScale;
            }

            _thrusterSound.volume = _thrusterSoundVolume * maxScale;
            _thrusterSound.transform.position = WeightedAverage();

            SendHaptics(maxScale);

            // In space you can only hear sound by touch
            if (!_grabHelper.IsGrabbed)
            {
                maxScale = 0;
            }

            if (_thrusterSound.isPlaying && maxScale <= 0)
            {
                _thrusterSound.Pause();
            }
            else if (!_thrusterSound.isPlaying && maxScale > 0)
            {
                _thrusterSound.UnPause();
            }
        }

        public void FixedUpdate()
        {
            var velocity = _rigidbody.velocity;
            _acceleration = (velocity - _lastVelocity) / Time.deltaTime;
            var angularVelocity = _rigidbody.angularVelocity;
            _angularAcceleration = (angularVelocity - _lastAngularVelocity) / Time.deltaTime;

            var lastPoint = _waypoints[(_waypoints.Length + (_currentTargetIndex - 1)) % _waypoints.Length];
            var toPoint = _waypoints[_currentTargetIndex];
            var lastPointPosition = lastPoint.position;
            var dir = toPoint.position - lastPointPosition;
            var length = dir.magnitude;
            dir /= length;
            var pos = lastPointPosition + dir * (_speedCurve.Evaluate(_progress) * length);
            var angle = Vector3.Angle(transform.forward, dir);
            if (angle > 1f)
            {
                _startTime = Time.time + _delay;
                _rigidbody.MoveRotation(
                    QuaternionUtil.DynamicEaseLook(_rigidbody.rotation, dir, ref _rotVel, _turnAcceleration, _turnSpeed, Time.deltaTime));
            }
            else
            {
                if (Time.time > _startTime)
                {
                    _rigidbody.MovePosition(pos);
                    _progress += Time.deltaTime / length * _speed * SpeedLevelSmooth;
                }

                if (_progress > 1)
                {
                    _progress = 0;
                    _currentTargetIndex = (_currentTargetIndex + 1) % _waypoints.Length;

                    _startTime = Time.time + _delay;
                }
            }

            _lastVelocity = velocity;
            _lastAngularVelocity = angularVelocity;
        }

        // If only one weight is > epsilon, return that point exactly.
        // If all weights are zero, this returns points[0] (change behavior if you prefer).
        public Vector3 WeightedAverage(float epsilon = 1e-6f)
        {
            var lastNonZeroIndex = -1;
            var nonZeroCount = 0;
            var weightedSum = Vector3.zero;
            var weightSum = 0f;

            for (var i = 0; i < _thrusters.Length; i++)
            {
                var w = _thrusterWeights[i];
                if (Mathf.Abs(w) > epsilon)
                {
                    nonZeroCount++;
                    lastNonZeroIndex = i;
                }
                weightedSum += _thrusters[i].transform.position * w;
                weightSum += w;
            }

            if (nonZeroCount == 1)
            {
                // exactly one real contributor → return it unchanged
                return _thrusters[lastNonZeroIndex].transform.position;
            }

            // if all weights zero, choose fallback (here: first point). You can change this to Vector3.zero or throw.
            if (Mathf.Abs(weightSum) <= epsilon)
            {
                return _thrusters[0].transform.position;
            }

            return weightedSum / weightSum;
        }

        private void SendHaptics(float scale)
        {
            var finalAmount = scale * _hapticVelocityMultiply;
            if (finalAmount <= 0)
            {
                return;
            }

            var command = SendHapticImpulseCommand.Create(0, finalAmount, Time.deltaTime);

            if (_grabHelper.HasGrab(true))
            {
                var device = XRController.leftHand;
                device.ExecuteCommand(ref command);
            }

            if (_grabHelper.HasGrab(false))
            {
                var device = XRController.rightHand;
                device.ExecuteCommand(ref command);
            }
        }

        private float CalculateThrusterScale(Transform t, Vector3 velocity, Vector3 angularVelocity, Vector3 worldCenterOfMass)
        {
            // Thruster's forward direction (up in local space)
            var thrustDirection = t.up;

            // Linear contribution: how aligned is thrust with current velocity?
            var linearContribution = 0f;
            if (velocity.magnitude > 0.1f)
            {
                linearContribution = Mathf.Max(0f, Vector3.Dot(thrustDirection.normalized, -velocity.normalized));
                linearContribution *= velocity.magnitude * _velocityScaleFactor;
            }

            // Angular contribution: how much torque does this thruster contribute?
            var angularContribution = 0f;
            if (angularVelocity.magnitude > float.Epsilon)
            {
                // Vector from center of mass to thruster
                var leverArm = t.position - worldCenterOfMass;

                // Torque = leverArm × force (cross product)
                var potentialTorque = Vector3.Cross(leverArm, thrustDirection);

                // How aligned is this potential torque with current angular velocity?
                angularContribution = Mathf.Max(0f, Vector3.Dot(potentialTorque.normalized, -angularVelocity.normalized));
                angularContribution *= angularVelocity.magnitude * _angularScaleFactor;
            }

            // Combine contributions
            var totalContribution = linearContribution + angularContribution;

            // Normalize to 0-1 range
            return Mathf.Clamp01(totalContribution);
        }

        [ContextMenu("Start Now")]
        public void StartNow()
        {
            _startTime = Time.time;
        }
    }
}