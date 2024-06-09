using System.Collections.Generic;
using ThisOtherThing.UI.Shapes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UnityEcho.UI
{
    public class Pointer : MonoBehaviour, IPointerMoveHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        private Ellipse _pointer;

        [SerializeField]
        private Vector2 _size;

        private readonly Dictionary<int, Ellipse> _pointers = new();

        private void Start()
        {
            _pointer.gameObject.SetActive(false);
        }

        #region Implementation of IPointerEnterHandler

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_pointers.TryGetValue(eventData.pointerId, out var pointer))
            {
                pointer = Instantiate(_pointer, _pointer.transform.parent);
                pointer.name = "Pointer " + eventData.pointerId;
                _pointers.Add(eventData.pointerId, pointer);
            }

            pointer.gameObject.SetActive(true);
            pointer.enabled = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.enterEventCamera,
                out _);
        }

        #endregion

        #region Implementation of IPointerExitHandler

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_pointers.TryGetValue(eventData.pointerId, out var pointer))
            {
                pointer.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Implementation of IPointerMoveHandler

        public void OnPointerMove(PointerEventData eventData)
        {
            if (_pointers.TryGetValue(eventData.pointerId, out var pointer))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        transform as RectTransform,
                        eventData.position,
                        eventData.enterEventCamera,
                        out var localPoint) &&
                    eventData.pointerPressRaycast.isValid)
                {
                    pointer.enabled = true;

                    pointer.rectTransform.localPosition = localPoint;

                    if (eventData.currentInputModule is InputSystemXRINputModule inputModule)
                    {
                        var activationDistance = inputModule.PressThreshold;

                        var clampedDistance = Mathf.Min(eventData.pointerCurrentRaycast.distance, activationDistance);
                        pointer.CrossFadeAlpha(eventData.pointerCurrentRaycast.distance <= activationDistance ? 1 : 0, 0, true);
                        pointer.rectTransform.sizeDelta =
                            Vector2.one * Mathf.Lerp(_size.x, _size.y, Mathf.Pow(1 - clampedDistance / activationDistance, 2));
                    }
                }
                else
                {
                    pointer.enabled = false;
                }
            }
        }

        #endregion
    }
}