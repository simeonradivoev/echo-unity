using UnityEcho.UI.XR;
using UnityEngine.EventSystems;

namespace UnityEngine.InputSystem.UI
{
    // A pointer is identified by a single unique integer ID. It has an associated position and the ability to press
    // on up to three buttons. It can also scroll.
    //
    // There's a single ExtendedPointerEventData instance allocated for the pointer which is used to retain the pointer's
    // event state. As part of that state is specific to button presses, each button retains a partial copy of press-specific
    // event information.
    //
    // A pointer can operate in 2D (mouse, pen, touch) or 3D (tracked). For 3D, screen-space 2D positions are derived
    // via raycasts based on world-space position and orientation.
    internal struct PointerModel
    {
        public bool changedThisFrame;

        public ButtonState leftButton;

        public ButtonState rightButton;

        public ButtonState middleButton;

        public XRExtendedPointerEventData eventData;

        private float m_AltitudeAngle;

        private float m_AzimuthAngle;

        private float m_Extension;

        private float m_Pressure;

        private Vector2 m_Radius;

        private Vector2 m_ScreenPosition;

        private Vector2 m_ScrollDelta;

        private float m_Twist;

        private Vector3 m_WorldBodyVelocity;

        private Quaternion m_WorldOrientation;

        private Vector3 m_WorldOriginPosition;

        private Vector3 m_WorldPosition;

        public PointerModel(XRExtendedPointerEventData eventData)
        {
            this.eventData = eventData;

            changedThisFrame = false;

            leftButton = default;
            leftButton.OnEndFrame();
            rightButton = default;
            rightButton.OnEndFrame();
            middleButton = default;
            middleButton.OnEndFrame();

            m_ScreenPosition = default;
            m_ScrollDelta = default;
            m_WorldOrientation = default;
            m_WorldPosition = default;
            m_WorldBodyVelocity = default;
            m_WorldOriginPosition = default;

            m_Pressure = default;
            m_AzimuthAngle = default;
            m_AltitudeAngle = default;
            m_Twist = default;
            m_Radius = default;
            m_Extension = default;
            lastWorldPosition = default;
        }

        public UIPointerType pointerType => eventData.pointerType;

        public Vector2 screenPosition
        {
            get => m_ScreenPosition;
            set
            {
                if (m_ScreenPosition != value)
                {
                    m_ScreenPosition = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector3 lastWorldPosition { get; set; }

        public float extension
        {
            get => m_Extension;
            set
            {
                if (m_Extension != value)
                {
                    m_Extension = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector3 worldBodyVelocity
        {
            get => m_WorldBodyVelocity;
            set
            {
                if (m_WorldBodyVelocity != value)
                {
                    m_WorldBodyVelocity = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector3 worldPosition
        {
            get => m_WorldPosition;
            set
            {
                if (m_WorldPosition != value)
                {
                    lastWorldPosition = m_WorldPosition;
                    m_WorldPosition = value;
                    changedThisFrame = true;
                }
            }
        }

        public Quaternion worldOrientation
        {
            get => m_WorldOrientation;
            set
            {
                if (m_WorldOrientation != value)
                {
                    m_WorldOrientation = value;
                    changedThisFrame = true;
                }
            }
        }

        public Vector2 scrollDelta
        {
            get => m_ScrollDelta;
            set
            {
                if (m_ScrollDelta != value)
                {
                    changedThisFrame = true;
                    m_ScrollDelta = value;
                }
            }
        }

        public float pressure
        {
            get => m_Pressure;
            set
            {
                if (m_Pressure != value)
                {
                    changedThisFrame = true;
                    m_Pressure = value;
                }
            }
        }

        public float azimuthAngle
        {
            get => m_AzimuthAngle;
            set
            {
                if (m_AzimuthAngle != value)
                {
                    changedThisFrame = true;
                    m_AzimuthAngle = value;
                }
            }
        }

        public float altitudeAngle
        {
            get => m_AltitudeAngle;
            set
            {
                if (m_AltitudeAngle != value)
                {
                    changedThisFrame = true;
                    m_AltitudeAngle = value;
                }
            }
        }

        public float twist
        {
            get => m_Twist;
            set
            {
                if (m_Twist != value)
                {
                    changedThisFrame = true;
                    m_Twist = value;
                }
            }
        }

        public Vector2 radius
        {
            get => m_Radius;
            set
            {
                if (m_Radius != value)
                {
                    changedThisFrame = true;
                    m_Radius = value;
                }
            }
        }

        public void OnFrameFinished()
        {
            changedThisFrame = false;
            scrollDelta = default;
            leftButton.OnEndFrame();
            rightButton.OnEndFrame();
            middleButton.OnEndFrame();
        }

        public void CopyTouchOrPenStateFrom(PointerEventData eventData)
        {
#if UNITY_2021_1_OR_NEWER
            pressure = eventData.pressure;
            azimuthAngle = eventData.azimuthAngle;
            altitudeAngle = eventData.altitudeAngle;
            twist = eventData.twist;
            radius = eventData.radius;
#endif
        }

        // State related to pressing and releasing individual bodies. Retains those parts of
        // PointerInputEvent that are specific to presses and releases.
        public struct ButtonState
        {
            private int m_ClickCount;

            private bool m_ClickedOnSameGameObject;

            private float m_ClickTime; // On Time.unscaledTime timeline, NOT input event time.

            private bool m_Dragging;

            private GameObject m_DragObject;

            private PointerEventData.FramePressState m_FramePressState;

            private bool m_IgnoreNextClick;

            private bool m_IsPressed;

            private GameObject m_LastPressObject;

            private Vector3 m_PressLocalPos;

            private GameObject m_PressObject;

            private Vector2 m_PressPosition;

            private RaycastResult m_PressRaycast;

            private float m_PressTime;

            private GameObject m_RawPressObject;

            public bool isPressed
            {
                get => m_IsPressed;
                set
                {
                    if (m_IsPressed != value)
                    {
                        m_IsPressed = value;

                        if (m_FramePressState == PointerEventData.FramePressState.NotChanged && value)
                        {
                            m_FramePressState = PointerEventData.FramePressState.Pressed;
                        }
                        else if (m_FramePressState == PointerEventData.FramePressState.NotChanged && !value)
                        {
                            m_FramePressState = PointerEventData.FramePressState.Released;
                        }
                        else if (m_FramePressState == PointerEventData.FramePressState.Pressed && !value)
                        {
                            m_FramePressState = PointerEventData.FramePressState.PressedAndReleased;
                        }
                    }
                }
            }

            /// <summary>
            /// When we "release" a button other than through user interaction (e.g. through focus switching),
            /// we don't want this to count as an actual release that ends up clicking. This flag will cause
            /// generated events to have
            /// <c>
            /// eligibleForClick
            /// </c>
            /// to be false.
            /// </summary>
            public bool ignoreNextClick
            {
                get => m_IgnoreNextClick;
                set => m_IgnoreNextClick = value;
            }

            public float pressTime
            {
                get => m_PressTime;
                set => m_PressTime = value;
            }

            public bool clickedOnSameGameObject
            {
                get => m_ClickedOnSameGameObject;
                set => m_ClickedOnSameGameObject = value;
            }

            public bool wasPressedThisFrame =>
                m_FramePressState == PointerEventData.FramePressState.Pressed ||
                m_FramePressState == PointerEventData.FramePressState.PressedAndReleased;

            public bool wasReleasedThisFrame =>
                m_FramePressState == PointerEventData.FramePressState.Released ||
                m_FramePressState == PointerEventData.FramePressState.PressedAndReleased;

            public void CopyPressStateTo(XRExtendedPointerEventData eventData)
            {
                eventData.pointerPressRaycast = m_PressRaycast;
                eventData.pressLocalPos = m_PressLocalPos;
                eventData.pressPosition = m_PressPosition;
                eventData.clickCount = m_ClickCount;
                eventData.clickTime = m_ClickTime;
                // We can't set lastPress directly. Old input module uses three different event instances, one for each
                // button. We share one instance and just switch press states. Set pointerPress twice to get the lastPress
                // we need.
                //
                // NOTE: This does *NOT* quite work as stated in the docs. pointerPress is nulled out on button release which
                //       will set lastPress as a side-effect. This means that lastPress will only be non-null while no press is
                //       going on and will *NOT* refer to the last pressed object when a new object has been pressed on.
                eventData.pointerPress = m_LastPressObject;
                eventData.pointerPress = m_PressObject;
                eventData.rawPointerPress = m_RawPressObject;
                eventData.pointerDrag = m_DragObject;
                eventData.dragging = m_Dragging;

                if (ignoreNextClick)
                {
                    eventData.eligibleForClick = false;
                }
            }

            public void CopyPressStateFrom(XRExtendedPointerEventData eventData)
            {
                m_PressRaycast = eventData.pointerPressRaycast;
                m_PressLocalPos = eventData.pressLocalPos;
                m_PressObject = eventData.pointerPress;
                m_RawPressObject = eventData.rawPointerPress;
                m_LastPressObject = eventData.lastPress;
                m_PressPosition = eventData.pressPosition;
                m_ClickTime = eventData.clickTime;
                m_ClickCount = eventData.clickCount;
                m_DragObject = eventData.pointerDrag;
                m_Dragging = eventData.dragging;
            }

            public void OnEndFrame()
            {
                m_FramePressState = PointerEventData.FramePressState.NotChanged;
            }
        }
    }
}