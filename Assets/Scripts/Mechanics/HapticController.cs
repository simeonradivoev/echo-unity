using UnityEcho.Mechanics.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.XR.Haptics;

namespace UnityEcho.Mechanics
{
    public class HapticController : MonoBehaviour
    {
        /// <summary>
        /// How long should each pulse be.
        /// The lower the amount the more fine grain the pulses will be.
        /// There might be a lower limit to this amount depends on how short of a pulse can the motors deliver.
        /// </summary>
        [SerializeField]
        private float _hapticDuration = 0.2f;

        /// <summary>
        /// The multiplier for the haptic force
        /// </summary>
        [SerializeField]
        private float _hapticMultiplier = 0.6f;

        /// <summary>
        /// The min amount of normalized force to trigger the haptic
        /// </summary>
        [SerializeField]
        private float _hapticCutoff = 0.2f;

        /// <summary>
        /// What is the average scale value of impact force. This will determine how much the haptic is scaled with the force applied.
        /// </summary>
        [SerializeField]
        private float _hapticForceScale = 30;

        [SerializeField]
        private float _grabHapticAmplitude = 0.3f;

        [SerializeField]
        private float _grabHapticDuration = 0.2f;

        private GrabMoveController _grabMoveController;

        private Vector3 _lastHapticForce;

        private float _lastImpulseTime;

        private void Start()
        {
            _grabMoveController = GetComponent<GrabMoveController>();
            _grabMoveController.Grabbed += GrabMoveControllerOnGrabbed;
        }

        private void Update()
        {
            // Player rigibody haptics
            ProcessDeltaHaptics(
                _grabMoveController.GrabData.PlayerBumpImpulse,
                _grabMoveController.GrabData.PlayerBumpPos,
                _hapticDuration,
                _hapticMultiplier,
                _hapticCutoff,
                _hapticForceScale);
        }

        // Grab haptics
        private void GrabMoveControllerOnGrabbed(GrabHitData obj, GrabMoveController controller)
        {
            var device = InputSystem.GetDevice<XRController>(_grabMoveController.HandNode.ToString());
            var command = SendHapticImpulseCommand.Create(0, _grabHapticAmplitude, _grabHapticDuration);
            device.ExecuteCommand(ref command);
        }

        // Stereo haptics based on the direction of the collision from the hand. So that a collision on the right will mainly trigger the right hand motor.
        private void ProcessDeltaHaptics(Vector3 accumilated, Vector3 averagePoint, float duration, float multiplier, float cutoff, float forceScale)
        {
            var bodyForward = Vector3.Dot(
                Vector3.Normalize(averagePoint - _grabMoveController.Head.position),
                Vector3.Normalize(_grabMoveController.transform.position - _grabMoveController.Head.position));

            multiplier *= Mathf.Lerp(0.2f, 1, Mathf.Clamp01(bodyForward));

            var forceDiff = _lastHapticForce - accumilated;
            _lastHapticForce = accumilated;
            var force = forceDiff.magnitude;

            if (Time.unscaledTime - _lastImpulseTime >= 0)
            {
                if (force > 0.05f)
                {
                    var device = InputSystem.GetDevice<XRController>(_grabMoveController.HandNode.ToString());
                    if (device != null)
                    {
                        var command = SendHapticImpulseCommand.Create(
                            0,
                            Mathf.Clamp01(Mathf.Clamp01(force / forceScale) - cutoff) * multiplier,
                            Time.deltaTime);
                        device.ExecuteCommand(ref command);
                        _lastImpulseTime = Time.unscaledTime + Time.deltaTime;
                    }
                }
            }
        }
    }
}