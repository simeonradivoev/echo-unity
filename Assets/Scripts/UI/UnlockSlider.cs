using UnityEcho.Objectives;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR.Haptics;

namespace UnityEcho.UI
{
    public class UnlockSlider : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
    {
        [SerializeField]
        private float _acceleration;

        [SerializeField]
        private float _terminalVelocity;

        [SerializeField]
        private float _performTreshold;

        [SerializeField]
        private UnityEvent _onPerformed;

        [Header("Haptics")]
        [SerializeField]
        [Range(0, 1)]
        private float _performAmplitude = 0.1f;

        [SerializeField]
        private float _performDuration = 0.1f;

        [SerializeField]
        private UnityEvent _onObjectiveNotification;

        [SerializeField]
        private UnityEvent _onObjectiveNotificationDissmiss;

        private bool _dragging;

        private ObjectivesManager _objectivesManager;

        private bool _performed;

        private RectTransform _rectTransform;

        private bool _reset = true;

        private float _velocity;

        private void Start()
        {
            _rectTransform = GetComponent<RectTransform>();
            _objectivesManager = FindObjectOfType<ObjectivesManager>();
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveStarted.AddListener(OnObjectiveStarted);
            }
        }

        private void Update()
        {
            var anchoredPosition = _rectTransform.anchoredPosition;
            var parent = (RectTransform)_rectTransform.parent;
            var parentWidth = parent.rect.width;

            if (anchoredPosition.x <= 0 || _dragging)
            {
                return;
            }

            _velocity = Mathf.Min(_velocity + Time.deltaTime * _acceleration, _terminalVelocity);
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x - _velocity, 0, parentWidth);
            _rectTransform.anchoredPosition = anchoredPosition;
            _reset = anchoredPosition.x <= 0;
        }

        private void OnDestroy()
        {
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
            }
        }

        #region Implementation of IBeginDragHandler

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_reset)
            {
                return;
            }

            _velocity = 0;
            _dragging = true;
        }

        #endregion

        #region Implementation of IDragHandler

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging)
            {
                return;
            }

            var localPosition = _rectTransform.localPosition;

            var parent = (RectTransform)_rectTransform.parent;
            var parentWidth = parent.rect.width;

            if (_performed)
            {
                localPosition.x = parentWidth;
                _rectTransform.localPosition = localPosition;
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var localPointerPos);
            localPosition.x = Mathf.Clamp(localPointerPos.x, 0, parentWidth);
            _rectTransform.localPosition = localPosition;
            if (localPointerPos.x >= parentWidth - _performTreshold)
            {
                _onPerformed.Invoke();
                _performed = true;

                if (eventData is ExtendedPointerEventData extendedPointerEventData)
                {
                    var device = extendedPointerEventData.device;
                    var command = SendHapticImpulseCommand.Create(0, _performAmplitude, _performDuration);
                    device.ExecuteCommand(ref command);
                }
            }
        }

        #endregion

        #region Implementation of IEndDragHandler

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _performed = false;
        }

        #endregion

        #region Implementation of IInitializePotentialDragHandler

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        #endregion

        private void OnObjectiveStarted()
        {
            _onObjectiveNotification.Invoke();
        }

        public void DismissNotification()
        {
            _onObjectiveNotificationDissmiss.Invoke();
        }
    }
}