using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.XR.Haptics;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace UnityEcho.Mechanics.Utils
{
    public class GrabHapticPulse : MonoBehaviour, IGrabHandler, IReleaseHandler
    {
        [SerializeField]
        private bool _playOnGrab;

        [SerializeField]
        private bool _playOnRelease;

        [SerializeField]
        [Range(0, 1)]
        private float _amplitude = 0.1f;

        [SerializeField]
        private float _duration = 0.1f;

        private bool2 _grabs;

        #region Implementation of IGrabHandler

        public void OnGrab(GrabContext context)
        {
            _grabs[context.Left ? 0 : 1] = true;
            if (_playOnGrab)
            {
                Play(context.Left ? XRController.leftHand : XRController.rightHand);
            }
        }

        #endregion

        #region Implementation of IReleaseHandler

        public void OnRelease(ReleaseContext context)
        {
            if (_playOnRelease)
            {
                Play(context.Left ? XRController.leftHand : XRController.rightHand);
            }
            _grabs[context.Left ? 0 : 1] = false;
        }

        #endregion

        private void Play(InputDevice device, float amplitudeMultiply = 1)
        {
            var command = SendHapticImpulseCommand.Create(0, _amplitude * amplitudeMultiply, _duration);
            device.ExecuteCommand(ref command);
        }

        public void Play(float amplitudeMultiply)
        {
            if (_grabs[0] && XRController.leftHand != null)
            {
                Play(XRController.leftHand, amplitudeMultiply);
            }

            if (_grabs[1] && XRController.rightHand != null)
            {
                Play(XRController.rightHand, amplitudeMultiply);
            }
        }

        public void Play()
        {
            Play(1);
        }
    }
}