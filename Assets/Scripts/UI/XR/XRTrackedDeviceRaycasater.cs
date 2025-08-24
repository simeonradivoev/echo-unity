using System;
using System.Collections.Generic;
using UnityEcho.UI.XR;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UnityEngine.InputSystem.UI
{
    /// <summary>
    /// Raycasting implementation for use with <see cref="TrackedDevice"/>s.
    /// </summary>
    /// <remarks>
    /// This component needs to be added alongside the
    /// <c>
    /// Canvas
    /// </c>
    /// component. Usually, raycasting is
    /// performed by the
    /// <c>
    /// GraphicRaycaster
    /// </c>
    /// component found there but for 3D raycasting necessary for
    /// tracked devices, this component is required.
    /// </remarks>
    public class XRTrackedDeviceRaycasater : BaseRaycaster
    {
        internal static InlinedArray<XRTrackedDeviceRaycasater> s_Instances;

        private static readonly List<RaycastHitData> s_SortedGraphics = new();

        private static readonly Vector3[] _cornersTmp = new Vector3[4];

        [FormerlySerializedAs("ignoreReversedGraphics")]
        [SerializeField]
        private bool m_IgnoreReversedGraphics;

        [FormerlySerializedAs("checkFor2DOcclusion")]
        [SerializeField]
        private bool m_CheckFor2DOcclusion;

        [FormerlySerializedAs("checkFor3DOcclusion")]
        [SerializeField]
        private bool m_CheckFor3DOcclusion;

        [Tooltip("Maximum distance (in 3D world space) that rays are traced to find a hit.")]
        [SerializeField]
        private float m_MaxDistance = 1000;

        [SerializeField]
        private LayerMask m_BlockingMask;

        /// <summary>
        /// How far away should the pointer lag.
        /// </summary>
        [SerializeField]
        private float _pressLagBehindDistance;

        /// <summary>
        /// At what distance should the lagging pointer escape the press.
        /// </summary>
        [SerializeField]
        private float _pressLagExcapeDistance;

        // Cached instances for raycasts hits to minimize GC.
        [NonSerialized]
        private readonly List<RaycastHitData> m_RaycastResultsCache = new();

        [NonSerialized]
        private Canvas m_Canvas;

        public override Camera eventCamera
        {
            get
            {
                var myCanvas = canvas;
                return myCanvas != null ? myCanvas.worldCamera : null;
            }
        }

        public LayerMask blockingMask
        {
            get => m_BlockingMask;
            set => m_BlockingMask = value;
        }

        public bool checkFor3DOcclusion
        {
            get => m_CheckFor3DOcclusion;
            set => m_CheckFor3DOcclusion = value;
        }

        public bool checkFor2DOcclusion
        {
            get => m_CheckFor2DOcclusion;
            set => m_CheckFor2DOcclusion = value;
        }

        public bool ignoreReversedGraphics
        {
            get => m_IgnoreReversedGraphics;
            set => m_IgnoreReversedGraphics = value;
        }

        public float maxDistance
        {
            get => m_MaxDistance;
            set => m_MaxDistance = value;
        }

        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                {
                    return m_Canvas;
                }

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            s_Instances.AppendWithCapacity(this);
        }

        protected override void OnDisable()
        {
            var index = s_Instances.IndexOf(this);
            if (index != -1)
            {
                s_Instances.RemoveAtByMovingTailWithCapacity(index);
            }

            base.OnDisable();
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is XRExtendedPointerEventData trackedEventData && trackedEventData.pointerType == UIPointerType.Tracked)
            {
                PerformRaycast(trackedEventData, resultAppendList);
            }
        }

        private void PerformRaycast(
            XRExtendedPointerEventData pointerEventData,
            Vector3 worldPosition,
            Vector3 direction,
            List<RaycastResult> resultAppendList)
        {
            var ray = new Ray(worldPosition, direction);
            var hitDistance = m_MaxDistance;

#if UNITY_INPUT_SYSTEM_ENABLE_PHYSICS
            if (m_CheckFor3DOcclusion)
            {
                if (Physics.Raycast(ray, out var hit, maxDistance: hitDistance, layerMask: m_BlockingMask))
                    hitDistance = hit.distance;
            }
#endif

            m_RaycastResultsCache.Clear();
            SortedRaycastGraphics(canvas, ray, m_RaycastResultsCache);

            // Now that we have a list of sorted hits, process any extra settings and filters.
            for (var i = 0; i < m_RaycastResultsCache.Count; i++)
            {
                var validHit = true;

                var hitData = m_RaycastResultsCache[i];

                var go = hitData.graphic.gameObject;
                if (m_IgnoreReversedGraphics)
                {
                    var forward = ray.direction;
                    var goDirection = go.transform.rotation * Vector3.forward;
                    validHit = Vector3.Dot(forward, goDirection) > 0;
                }

                ((RectTransform)hitData.graphic.transform).GetWorldCorners(_cornersTmp);
                var canvasPlane = new Plane(_cornersTmp[0], _cornersTmp[1], _cornersTmp[2]);
                var side = canvasPlane.GetSide(worldPosition);

                // We want to count a release over an element if you go far back enough but still kinda pointing towards the element.
                // So that we can press buttons by poking them fast, the callback still requires a raycast
                if (side || !pointerEventData.pointerPress)
                {
                    validHit &= hitData.distance <= hitDistance;
                }

                if (validHit)
                {
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = hitData.distance * (side ? 1 : -1),
                        index = resultAppendList.Count,
                        depth = hitData.graphic.depth,
                        worldPosition = worldPosition,
                        screenPosition = hitData.screenPosition,
                        worldNormal = direction
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        internal void PerformRaycast(XRExtendedPointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null || !canvas.enabled || eventCamera == null || eventData.extension < 0.5f)
            {
                return;
            }

            var worldPosition = eventData.trackedDevicePosition;
            var lastWorldPosition = eventData.trackedDeviceLastPosition;

            (canvas.transform as RectTransform).GetWorldCorners(_cornersTmp);

            var canvasPlane = new Plane(_cornersTmp[0], _cornersTmp[1], _cornersTmp[2]);

            if (!canvasPlane.SameSide(lastWorldPosition, worldPosition) && canvasPlane.GetSide(lastWorldPosition))
            {
                var dir = (worldPosition - lastWorldPosition).normalized;
                if (canvasPlane.Raycast(new Ray(lastWorldPosition, dir), out var enter))
                {
                    worldPosition = lastWorldPosition + dir * (enter * 0.5f);
                }
            }

            var closestPoint = canvasPlane.ClosestPointOnPlane(worldPosition);
            var direction = Vector3.Normalize(closestPoint - worldPosition);

            PerformRaycast(eventData, worldPosition, direction, resultAppendList);

            if (_pressLagBehindDistance > 0 &&
                eventData.pointerPressRaycast.module == this &&
                resultAppendList.Count > 0 &&
                !eventData.excapedPressLag)
            {
                var pressWorldPos = eventData.pointerPressRaycast.gameObject.transform.TransformPoint(eventData.pressLocalPos);
                var topNormalRaycast = resultAppendList[0];
                resultAppendList.Clear();

                var topProjectedPosition = canvasPlane.ClosestPointOnPlane(topNormalRaycast.worldPosition);
                var pressProjectedPosition = canvasPlane.ClosestPointOnPlane(pressWorldPos);

                var projectedDistance = Mathf.Clamp01(Vector3.Distance(topProjectedPosition, pressProjectedPosition) / _pressLagBehindDistance);

                worldPosition = Vector3.Lerp(pressProjectedPosition, topProjectedPosition, projectedDistance) +
                                canvasPlane.normal * Mathf.Max(topNormalRaycast.distance, 0.001f);
                eventData.excapedPressLag = projectedDistance >= _pressLagExcapeDistance;

                closestPoint = canvasPlane.ClosestPointOnPlane(worldPosition);
                direction = Vector3.Normalize(closestPoint - worldPosition);

                PerformRaycast(eventData, worldPosition, direction, resultAppendList);
            }
        }

        private void SortedRaycastGraphics(Canvas canvas, Ray ray, List<RaycastHitData> results)
        {
            var graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(canvas);

            s_SortedGraphics.Clear();
            for (var i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

                if (graphic.depth == -1)
                {
                    continue;
                }

                Vector3 worldPos;
                float distance;
                if (RayIntersectsRectTransform(graphic.rectTransform, ray, graphic.raycastPadding, out worldPos, out distance))
                {
                    // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                    if (!graphic.raycastTarget || graphic.canvasRenderer.cull || graphic.depth == -1)
                    {
                        continue;
                    }

                    Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                    // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                    if (graphic.Raycast(screenPos, eventCamera))
                    {
                        s_SortedGraphics.Add(new RaycastHitData(graphic, worldPos, screenPos, distance));
                    }
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

            results.AddRange(s_SortedGraphics);
        }

        private static bool RayIntersectsRectTransform(
            RectTransform transform,
            Ray ray,
            Vector4 padding,
            out Vector3 worldPosition,
            out float distance)
        {
            var corners = new Vector3[4];
            transform.GetLocalCorners(corners);
            corners[1].x += padding.x;
            corners[0].x += padding.x;

            corners[3].y += padding.y;
            corners[0].y += padding.y;

            corners[3].x -= padding.z;
            corners[2].x -= padding.z;

            corners[1].y -= padding.w;
            corners[2].y -= padding.w;
            for (var i = 0; i < corners.Length; i++)
            {
                corners[i] = transform.TransformPoint(corners[i]);
            }
            var plane = new Plane(corners[0], corners[1], corners[2]);

            float enter;
            if (plane.Raycast(ray, out enter))
            {
                var intersection = ray.GetPoint(enter);

                var bottomEdge = corners[3] - corners[0];
                var leftEdge = corners[1] - corners[0];
                var bottomDot = Vector3.Dot(intersection - corners[0], bottomEdge);
                var leftDot = Vector3.Dot(intersection - corners[0], leftEdge);

                // If the intersection is right of the left edge and above the bottom edge.
                if (leftDot >= 0 && bottomDot >= 0)
                {
                    var topEdge = corners[1] - corners[2];
                    var rightEdge = corners[3] - corners[2];
                    var topDot = Vector3.Dot(intersection - corners[2], topEdge);
                    var rightDot = Vector3.Dot(intersection - corners[2], rightEdge);

                    //If the intersection is left of the right edge, and below the top edge
                    if (topDot >= 0 && rightDot >= 0)
                    {
                        worldPosition = intersection;
                        distance = enter;
                        return true;
                    }
                }
            }
            worldPosition = Vector3.zero;
            distance = 0;
            return false;
        }

        private struct RaycastHitData
        {
            public RaycastHitData(Graphic graphic, Vector3 worldHitPosition, Vector2 screenPosition, float distance)
            {
                this.graphic = graphic;
                this.worldHitPosition = worldHitPosition;
                this.screenPosition = screenPosition;
                this.distance = distance;
            }

            public Graphic graphic { get; }

            public Vector3 worldHitPosition { get; }

            public Vector2 screenPosition { get; }

            public float distance { get; }
        }
    }
}