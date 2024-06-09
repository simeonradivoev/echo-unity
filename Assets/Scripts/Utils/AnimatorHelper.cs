using UnityEngine;

namespace UnityEcho.Utils
{
    /// <summary>
    /// Just helper method to set bools from  unity events
    /// </summary>
    public class AnimatorHelper : MonoBehaviour
    {
        private Animator _animator;

        private void Start()
        {
            _animator = GetComponent<Animator>();
        }

        public void SetTrue(string key)
        {
            _animator.SetBool(key, true);
        }

        public void SetFalse(string key)
        {
            _animator.SetBool(key, false);
        }
    }
}