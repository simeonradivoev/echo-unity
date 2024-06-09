using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Interactables
{
    public class Receptacle : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent _onActivate;

        [SerializeField]
        private UnityEvent _onDeactivate;

        [Tooltip("If this is true, the receptacle will be activated on attach")]
        [SerializeField]
        private bool _isInteractive = true;

        private ReceptacleConnector _connector;

        private bool _isActive;

        public bool IsInteractive
        {
            get => _isInteractive;
            set => _isInteractive = value;
        }

        private void Start()
        {
            _connector = GetComponentInChildren<ReceptacleConnector>();
            _connector.OnAttach.AddListener(OnAttach);
            _connector.OnDetach.AddListener(OnDetach);
        }

        private void OnDetach()
        {
            if (_isActive)
            {
                Deactivate();
            }
        }

        private void OnAttach()
        {
            if (_isInteractive)
            {
                TryActivate();
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                _onDeactivate.Invoke();
                _isActive = false;
            }
        }

        public void TryActivate()
        {
            if (_connector.AttachedRigidbody && !_isActive)
            {
                _onActivate.Invoke();
                _isActive = true;
            }
        }
    }
}