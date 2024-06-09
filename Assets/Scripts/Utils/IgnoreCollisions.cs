using UnityEngine;

namespace UnityEcho.Utils
{
    public class IgnoreCollisions : MonoBehaviour
    {
        [SerializeField]
        private Collider _collider;

        private void Awake()
        {
            var colliders = GetComponents<Collider>();
            foreach (var lhs in colliders)
            {
                Physics.IgnoreCollision(lhs, _collider);
            }
        }
    }
}