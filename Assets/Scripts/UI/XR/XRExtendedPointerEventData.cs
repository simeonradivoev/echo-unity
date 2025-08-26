using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UnityEcho.UI.XR
{
    public class XRExtendedPointerEventData : ExtendedPointerEventData
    {
        public Vector3 pressLocalPos;

        /// <summary>
        /// Same as ExtendedPointerEventData but last value
        /// Also compensated with the velocity of the body that represent the tracked device. To avoid jitter when moving fast.
        /// </summary>
        public Vector3 trackedDeviceLastPosition { get; set; }

        /// <summary>
        /// How much is the finger extended.
        /// </summary>
        public float extension { get; set; }

        /// <summary>
        /// Don't lag behind anymore if press was far enough.
        /// </summary>
        public bool excapedPressLag { get; set; }

        public bool firstEnter { get; set; }

        public float lastDistance { get; set; }

        public float distanceDelta { get; set; }

        public XRExtendedPointerEventData(EventSystem eventSystem)
            : base(eventSystem)
        {
        }
    }
}