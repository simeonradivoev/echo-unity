// Copy from interactions toolkit package

using System.Collections.Generic;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    [DisallowMultipleComponent]
    public class CanvasOptimizer : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How wide of an field-of-view to use when determining if a canvas is in view.")]
        private float m_RayPositionIgnoreAngle = 45f;

        [SerializeField]
        [Tooltip("How much the camera and canvas rotate away from one another and still be considered facing.")]
        private float m_RayFacingIgnoreAngle = 75f;

        [SerializeField]
        [Tooltip("How far away a canvas can be from this camera and still receive input.")]
        private float m_RayPositionIgnoreDistance = 25f;

        private readonly Dictionary<CanvasTracker, CanvasState> m_CanvasTrackers = new();

        private Camera m_CullingCamera;

        private Transform m_CullingCameraTransform;

        /// <summary>
        /// How wide of an field-of-view to use when determining if a canvas is in view.
        /// </summary>
        public float rayPositionIgnoreAngle
        {
            get => m_RayPositionIgnoreAngle;
            set => m_RayPositionIgnoreAngle = value;
        }

        /// <summary>
        /// How much the camera and canvas rotate away from one another and still be considered facing.
        /// </summary>
        public float rayFacingIgnoreAngle
        {
            get => m_RayFacingIgnoreAngle;
            set => m_RayFacingIgnoreAngle = value;
        }

        /// <summary>
        /// How far away a canvas can be from this camera and still receive input.
        /// </summary>
        public float rayPositionIgnoreDistance
        {
            get => m_RayPositionIgnoreDistance;
            set => m_RayPositionIgnoreDistance = value;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            if (FindFirstObjectByType<CanvasOptimizer>() != this)
            {
                Debug.LogWarning(
                    $"Duplicate Canvas Optimizer {gameObject.name} found. Only one Canvas Optimizer is allowed in the scene at a time.",
                    this);
                Destroy(this);
                enabled = false;
                return;
            }

            FindCullingCamera();

            // Canvases cannot auto-register, so collect all canvases in the scene at start
#if UNITY_2023_1_OR_NEWER
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            var canvases = FindObjectsOfType<Canvas>(true);
#endif
            for (var index = 0; index < canvases.Length; ++index)
            {
                var canvas = canvases[index];
                RegisterCanvas(canvas);
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            CheckForNestedCanvasChanges();
            CheckForOutOfViewCanvases();
        }

        /// <summary>
        /// Allows the canvas optimizer to process this canvas. Will be called automatically for all canvases in the scene.
        /// </summary>
        /// <param name="canvas">
        /// The canvas to optimize.
        /// </param>
        /// <remarks>
        /// This only needs to be called manually for canvases instantiated at runtime.
        /// </remarks>
        public void RegisterCanvas(Canvas canvas)
        {
            var canvasTracker = InitializeCanvasTracking(canvas);
            if (m_CanvasTrackers.ContainsKey(canvasTracker))
            {
                return;
            }

            var canvasState = new CanvasState();
            canvasState.Initialize(canvasTracker);
            m_CanvasTrackers.Add(canvasTracker, canvasState);
        }

        /// <summary>
        /// Tells the canvas optimizer to stop processing this canvas. Will be called automatically for all canvases in the scene.
        /// </summary>
        /// <param name="canvas">
        /// The canvas to stop optimizing.
        /// </param>
        /// <remarks>
        /// This only needs to be called manually for canvases destroyed during runtime.
        /// </remarks>
        public void UnregisterCanvas(Canvas canvas)
        {
            // Remove matching canvas tracker
            if (canvas.TryGetComponent(out CanvasTracker toRemove))
            {
                m_CanvasTrackers.Remove(toRemove);
            }
        }

        private static CanvasTracker InitializeCanvasTracking(Canvas target)
        {
            // Put parent tracker on target
            if (!target.gameObject.TryGetComponent(out CanvasTracker tracker))
            {
                tracker = target.gameObject.AddComponent<CanvasTracker>();
                tracker.hideFlags = HideFlags.HideAndDontSave;
            }

            return tracker;
        }

        private void CheckForNestedCanvasChanges()
        {
            foreach (var canvasData in m_CanvasTrackers.Values)
            {
                canvasData.CheckForNestedChanges();
            }
        }

        private void CheckForOutOfViewCanvases()
        {
            // Find the new main camera if necessary
            if (m_CullingCamera == null || !m_CullingCamera.enabled)
            {
                FindCullingCamera();

                if (m_CullingCameraTransform == null)
                {
                    return;
                }
            }

            foreach (var canvasData in m_CanvasTrackers.Values)
            {
                canvasData.CheckForOutOfView(m_CullingCameraTransform, m_RayPositionIgnoreAngle, m_RayFacingIgnoreAngle, m_RayPositionIgnoreDistance);
            }
        }

        private void FindCullingCamera()
        {
            m_CullingCamera = Camera.main;
            m_CullingCameraTransform = m_CullingCamera != null ? m_CullingCamera.transform : null;
        }

        private class CanvasState
        {
            private const float k_CanvasCheckInterval = 0.5f;

            private CanvasTracker m_Tracker;

            private readonly CanvasSettings m_CanvasSettings = new();

            private readonly CanvasScalerSettings m_CanvasScalerSettings = new();

            private readonly GraphicRaycasterSettings m_GraphicRaycasterSettings = new();

            private bool m_WasNested;

            private bool m_Nested;

            private bool m_RaysDisabled;

            private Canvas m_Canvas;

            private GraphicRaycaster m_Raycaster;

            private XRTrackedDeviceRaycasater m_TrackedDeviceGraphicRaycaster;

            private float m_CheckTimer;

            internal void Initialize(CanvasTracker tracker)
            {
                m_Tracker = tracker;
                var go = m_Tracker.gameObject;
                go.TryGetComponent(out m_Canvas);
                go.TryGetComponent(out m_Raycaster);
                CheckForNestedChanges(true);
            }

            internal void CheckForNestedChanges(bool force = false)
            {
                if (!m_Tracker.transformDirty && !force)
                {
                    return;
                }

                m_Tracker.transformDirty = false;

                var transform = m_Tracker.transform;

                // Check for nesting
                var parent = transform.parent;
                var parentCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
                m_Nested = parentCanvas != null;

                // If nested has occurred, remove unnecessary components
                if (m_Nested && (!m_WasNested || force))
                {
                    if (transform.TryGetComponent<CanvasScaler>(out var canvasScaler))
                    {
                        m_CanvasScalerSettings.present = true;
                        m_CanvasScalerSettings.CopyFrom(canvasScaler);
                        Destroy(canvasScaler);
                    }
                    else
                    {
                        m_CanvasScalerSettings.present = false;
                    }

                    if (transform.TryGetComponent<GraphicRaycaster>(out var graphicRaycaster))
                    {
                        m_GraphicRaycasterSettings.present = true;
                        m_GraphicRaycasterSettings.CopyFrom(graphicRaycaster);
                        Destroy(graphicRaycaster);
                    }
                    else
                    {
                        m_GraphicRaycasterSettings.present = false;
                    }

                    if (transform.TryGetComponent<Canvas>(out var canvas))
                    {
                        m_CanvasSettings.present = true;
                        m_CanvasSettings.CopyFrom(canvas);
                        Destroy(canvas);
                    }
                    else
                    {
                        m_CanvasSettings.present = false;
                    }

                    if (transform.TryGetComponent(out m_TrackedDeviceGraphicRaycaster))
                    {
                        // ReSharper disable once PossibleNullReferenceException -- already verified above with m_Nested
                        if (!parentCanvas.TryGetComponent<XRTrackedDeviceRaycasater>(out _))
                        {
                            Debug.LogWarning(
                                $"Tracked device raycaster not present on parent canvas: {parent.name}. Tracked device input will likely not work on: {transform.name}",
                                transform);
                        }

                        m_TrackedDeviceGraphicRaycaster.enabled = false;
                    }
                }

                // If nesting has not occurred, restore the components
                if (!m_Nested && (m_WasNested || force))
                {
                    if (m_CanvasSettings.present)
                    {
                        var go = transform.gameObject;
                        m_Canvas = go.AddComponent<Canvas>();

                        m_CanvasSettings.CopyTo(m_Canvas);

                        if (m_CanvasScalerSettings.present)
                        {
                            var canvasScaler = go.AddComponent<CanvasScaler>();
                            m_CanvasScalerSettings.CopyTo(canvasScaler);
                        }

                        if (m_GraphicRaycasterSettings.present)
                        {
                            m_Raycaster = go.AddComponent<GraphicRaycaster>();
                            m_GraphicRaycasterSettings.CopyTo(m_Raycaster);
                        }

                        if (m_TrackedDeviceGraphicRaycaster != null)
                        {
                            m_TrackedDeviceGraphicRaycaster.enabled = true;
                        }
                    }
                }

                m_WasNested = m_Nested;
            }

            internal void CheckForOutOfView(Transform gazeSource, float fovAngle, float facingAngle, float maxDistance)
            {
                if (m_Nested)
                {
                    return;
                }

                if (m_Canvas.renderMode != RenderMode.WorldSpace)
                {
                    return;
                }

                m_CheckTimer += Time.deltaTime;

                if (m_CheckTimer < k_CanvasCheckInterval)
                {
                    return;
                }

                m_CheckTimer = 0f;

                var transform = m_Canvas.transform;
                var gazePos = gazeSource.position;
                var gazeDir = gazeSource.forward;
                var targetPos = transform.position;
                var targetDir = transform.forward;

                // Check if canvas is facing away from camera
                // Check if canvas is off camera
                // If any of these are true, disable the ray casters
                var disableRayCasters = BurstGazeUtility.IsOutsideGaze(gazePos, gazeDir, targetPos, fovAngle) ||
                                        (!BurstGazeUtility.IsAlignedToGazeForward(gazeDir, targetDir, facingAngle) &&
                                         BurstGazeUtility.IsOutsideDistanceRange(gazePos, targetPos, maxDistance));

                // See if state changed
                if (m_RaysDisabled != disableRayCasters)
                {
                    m_RaysDisabled = disableRayCasters;

                    // Disable tracked device caster
                    if (m_Raycaster != null)
                    {
                        m_Raycaster.enabled = !m_RaysDisabled;
                    }

                    if (m_TrackedDeviceGraphicRaycaster != null)
                    {
                        m_TrackedDeviceGraphicRaycaster.enabled = !m_RaysDisabled;
                    }
                }
            }

            private class CanvasScalerSettings
            {
                private float m_DefaultSpriteDPI;

                private float m_DynamicPixelsPerUnit;

                private float m_FallbackScreenDPI;

                private float m_MatchWidthOrHeight;

                private CanvasScaler.Unit m_PhysicalUnit;

                private float m_ReferencePixelsPerUnit;

                private Vector2 m_ReferenceResolution;

                private float m_ScaleFactor;

                private CanvasScaler.ScreenMatchMode m_ScreenMatchMode;

                private CanvasScaler.ScaleMode m_UiScaleMode;

                public bool present { get; set; }

                public void CopyFrom(CanvasScaler source)
                {
                    m_DefaultSpriteDPI = source.defaultSpriteDPI;
                    m_DynamicPixelsPerUnit = source.dynamicPixelsPerUnit;
                    m_FallbackScreenDPI = source.fallbackScreenDPI;
                    m_MatchWidthOrHeight = source.matchWidthOrHeight;
                    m_PhysicalUnit = source.physicalUnit;
                    m_ReferencePixelsPerUnit = source.referencePixelsPerUnit;
                    m_ReferenceResolution = source.referenceResolution;
                    m_ScaleFactor = source.scaleFactor;
                    m_ScreenMatchMode = source.screenMatchMode;
                    m_UiScaleMode = source.uiScaleMode;
                }

                public void CopyTo(CanvasScaler dest)
                {
                    dest.defaultSpriteDPI = m_DefaultSpriteDPI;
                    dest.dynamicPixelsPerUnit = m_DynamicPixelsPerUnit;
                    dest.fallbackScreenDPI = m_FallbackScreenDPI;
                    dest.matchWidthOrHeight = m_MatchWidthOrHeight;
                    dest.physicalUnit = m_PhysicalUnit;
                    dest.referencePixelsPerUnit = m_ReferencePixelsPerUnit;
                    dest.referenceResolution = m_ReferenceResolution;
                    dest.scaleFactor = m_ScaleFactor;
                    dest.screenMatchMode = m_ScreenMatchMode;
                    dest.uiScaleMode = m_UiScaleMode;
                }
            }

            private class CanvasSettings
            {
                private AdditionalCanvasShaderChannels m_AdditionalShaderChannels;

                private float m_NormalizedSortingGridSize;

                private bool m_OverridePixelPerfect;

                private bool m_OverrideSorting;

                private float m_PlaneDistance;

                private float m_ReferencePixelsPerUnit;

                private RenderMode m_RenderMode;

                private float m_ScaleFactor;

                private int m_SortingLayerID;

                private string m_SortingLayerName;

                private int m_SortingOrder;

                private int m_TargetDisplay;

                public bool present { get; set; }

                public void CopyFrom(Canvas source)
                {
                    m_AdditionalShaderChannels = source.additionalShaderChannels;
                    m_NormalizedSortingGridSize = source.normalizedSortingGridSize;
                    m_OverridePixelPerfect = source.overridePixelPerfect;
                    m_OverrideSorting = source.overrideSorting;
                    m_PlaneDistance = source.planeDistance;
                    m_ReferencePixelsPerUnit = source.referencePixelsPerUnit;
                    m_RenderMode = source.renderMode;
                    m_ScaleFactor = source.scaleFactor;
                    m_SortingLayerID = source.sortingLayerID;
                    m_SortingLayerName = source.sortingLayerName;
                    m_SortingOrder = source.sortingOrder;
                    m_TargetDisplay = source.targetDisplay;
                }

                public void CopyTo(Canvas dest)
                {
                    dest.additionalShaderChannels = m_AdditionalShaderChannels;
                    dest.normalizedSortingGridSize = m_NormalizedSortingGridSize;
                    dest.overridePixelPerfect = m_OverridePixelPerfect;
                    dest.overrideSorting = m_OverrideSorting;
                    dest.planeDistance = m_PlaneDistance;
                    dest.referencePixelsPerUnit = m_ReferencePixelsPerUnit;
                    dest.renderMode = m_RenderMode;
                    dest.scaleFactor = m_ScaleFactor;
                    dest.sortingLayerID = m_SortingLayerID;
                    dest.sortingLayerName = m_SortingLayerName;
                    dest.sortingOrder = m_SortingOrder;
                    dest.targetDisplay = m_TargetDisplay;
                }
            }

            private class GraphicRaycasterSettings
            {
                private LayerMask m_BlockingMask;

                private GraphicRaycaster.BlockingObjects m_BlockingObjects;

                private bool m_IgnoreReversedGraphics;

                public bool present { get; set; }

                public void CopyFrom(GraphicRaycaster source)
                {
                    m_BlockingMask = source.blockingMask;
                    m_BlockingObjects = source.blockingObjects;
                    m_IgnoreReversedGraphics = source.ignoreReversedGraphics;
                }

                public void CopyTo(GraphicRaycaster dest)
                {
                    dest.blockingMask = m_BlockingMask;
                    dest.blockingObjects = m_BlockingObjects;
                    dest.ignoreReversedGraphics = m_IgnoreReversedGraphics;
                }
            }
        }
    }
}