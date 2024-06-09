using UnityEngine;

namespace UnityEcho.Interactables
{
    public class Door : MonoBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private void Reset()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetOpenAmount(float amount)
        {
            _animator.SetFloat("ClosedAmount", 1 - Mathf.Clamp01(amount));
        }

        public void SetClosed(bool closed)
        {
            _animator.SetBool("Closed", closed);
        }
    }
}