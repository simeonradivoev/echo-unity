using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR.Haptics;

namespace UnityEcho.UI
{
    public class PointerHapticPulse : MonoBehaviour, IPointerClickHandler, IPointerDownHandler
    {
        [SerializeField]
        [Range(0, 1)]
        private float _amplitude = 0.1f;

        [SerializeField]
        private float _duration = 0.1f;

        [SerializeField]
        private bool _triggerOnClick;

        #region Implementation of IPointerClickHandler

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_triggerOnClick)
            {
                return;
            }

            Trigger(eventData);
        }

        #endregion

        #region Implementation of IPointerDownHandler

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_triggerOnClick)
            {
                return;
            }

            Trigger(eventData);
        }

        #endregion

        private void Trigger(PointerEventData eventData)
        {
            if (eventData is ExtendedPointerEventData extendedPointerEventData)
            {
                var device = extendedPointerEventData.device;
                var command = SendHapticImpulseCommand.Create(0, _amplitude, _duration);
                device.ExecuteCommand(ref command);
            }
        }
    }
}