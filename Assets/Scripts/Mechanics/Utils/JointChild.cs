using UnityEngine;

namespace UnityEcho.Mechanics.Utils
{
    /// <summary>
    /// This needs to be put on chained joints so that we know the parent child relationship.
    /// Mainly used to disable collision with the head
    /// </summary>
    public class JointChild : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _parent;

        public Rigidbody Parent => _parent;
    }
}