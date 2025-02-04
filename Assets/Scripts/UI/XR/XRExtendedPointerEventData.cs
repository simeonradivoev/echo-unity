﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UnityEcho.UI.XR
{
    public class XRExtendedPointerEventData : ExtendedPointerEventData
    {
        public Vector3 pressLocalPos;

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