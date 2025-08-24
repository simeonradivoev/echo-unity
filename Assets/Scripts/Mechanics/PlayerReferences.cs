using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Holds all the references used in other components. This is to avoid having duplicate fields you need to manage all over the place.
    /// </summary>
    public class PlayerReferences : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _body;

        [SerializeField]
        private Transform _headSpace;

        [SerializeField]
        private Transform _head;

        [SerializeField]
        private SphereCollider _headCollider;

        [SerializeField]
        private Animator _animator;

        public Rigidbody Body => _body;

        public Transform HeadSpace => _headSpace;

        public Transform Head => _head;

        public SphereCollider HeadCollider => _headCollider;

        public Animator Animator => _animator;
    }
}