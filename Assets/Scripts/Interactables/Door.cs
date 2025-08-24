using UnityEngine;

namespace UnityEcho.Interactables
{
    public class Door : MonoBehaviour
    {
        private static readonly int Closed = Animator.StringToHash("Closed");

        private static readonly int ClosedAmount = Animator.StringToHash("ClosedAmount");

        [SerializeField]
        private bool _closedDefault;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        [HideInInspector]
        public bool _isClosedFinished;

        [SerializeField]
        [HideInInspector]
        public bool _isOpenFinished;

        public bool ClosingFinished => _isClosedFinished;

        public bool OpeningFinished => _isOpenFinished;

        public bool FullyClosed => _animator.GetBool(Closed) && ClosingFinished;

        private void Reset()
        {
            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            SetClosed(_closedDefault);
        }

        public void SetOpenAmount(float amount)
        {
            _animator.SetFloat(ClosedAmount, 1 - Mathf.Clamp01(amount));
        }

        public void SetClosed(bool closed)
        {
            _animator.SetBool(Closed, closed);
        }
    }
}