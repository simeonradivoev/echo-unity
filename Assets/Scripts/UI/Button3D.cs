using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace UnityEcho.UI
{
    public class Button3D : MonoBehaviour, IPointerMoveHandler
    {
        [SerializeField]
        private float _maxDistance;

        [SerializeField]
        private RectTransform _transform;

        #region Implementation of IPointerMoveHandler

        public void OnPointerMove(PointerEventData eventData)
        {
            if (eventData.currentInputModule is InputSystemXRINputModule inputModule)
            {
                var localPosition = _transform.localPosition;
                localPosition.z = -Mathf.Clamp(eventData.pointerCurrentRaycast.distance / _transform.lossyScale.z, 0, _maxDistance);
                _transform.localPosition = localPosition;
            }
        }

        #endregion
    }
}