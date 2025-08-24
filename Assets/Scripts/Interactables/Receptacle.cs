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

        public bool IsInteractive
        {
            get => _isInteractive;
            set => _isInteractive = value;
        }

        public bool IsActive { get; private set; }

        public UnityEvent OnActivate => _onActivate;

        public UnityEvent OnDeactivate => _onDeactivate;

        private void Start()
        {
            _connector = GetComponentInChildren<ReceptacleConnector>();
            _connector.OnAttach.AddListener(OnAttach);
            _connector.OnDetach.AddListener(OnDetach);
        }

        private void OnDetach()
        {
            if (IsActive)
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
            if (IsActive)
            {
                IsActive = false;
                _onDeactivate.Invoke();
            }
        }

        public void TryActivate()
        {
            if (_connector.AttachedRigidbody && !IsActive)
            {
                IsActive = true;
                _onActivate.Invoke();
            }
        }
    }
}