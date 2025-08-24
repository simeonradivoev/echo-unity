using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Demo
{
    public class OneTimeTrigger : MonoBehaviour
    {
        [SerializeField]
        private string _tag;

        [SerializeField]
        private UnityEvent _onTrigger;

        private bool _triggered;

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_tag) && !other.CompareTag(_tag) && (!other.attachedRigidbody || !other.attachedRigidbody.CompareTag(_tag)))
            {
                return;
            }

            _onTrigger.Invoke();
            _triggered = true;
        }

        public void ManualTrigger()
        {
            if (_triggered)
            {
                return;
            }

            _onTrigger.Invoke();
            _triggered = true;
        }
    }
}