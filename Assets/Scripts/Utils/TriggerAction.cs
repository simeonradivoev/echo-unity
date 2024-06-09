using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Utils
{
    public class TriggerAction : MonoBehaviour
    {
        [SerializeField]
        private string _tag;

        [SerializeField]
        private UnityEvent _onEnter;

        [SerializeField]
        private UnityEvent _onExit;

        private void OnTriggerEnter(Collider other)
        {
            if (!string.IsNullOrEmpty(_tag) && !other.CompareTag(_tag))
            {
                return;
            }

            _onEnter.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!string.IsNullOrEmpty(_tag) && !other.CompareTag(_tag))
            {
                return;
            }

            _onExit.Invoke();
        }
    }
}