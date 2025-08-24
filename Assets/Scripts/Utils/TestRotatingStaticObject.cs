using UnityEngine;

namespace UnityEcho.Utils
{
    public class TestRotatingStaticObject : MonoBehaviour
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private Vector3 _axis;

        [SerializeField]
        private float _speed;

        private void FixedUpdate()
        {
            _rigidbody.MoveRotation(_rigidbody.rotation * Quaternion.AngleAxis(Time.deltaTime * _speed, _axis));
        }
    }
}