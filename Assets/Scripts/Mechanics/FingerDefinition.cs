using UnityEngine;

namespace UnityEcho.Mechanics
{
    public class FingerDefinition : MonoBehaviour
    {
        [SerializeField]
        private float _radius;

        [SerializeField]
        private Vector3 _fingerDiskDirection;

        [SerializeField]
        private Vector3 _fingerUpDirection;

        [SerializeField]
        private float _maxCurlAngle;

        [SerializeField]
        private float _curlAngleRange;

        [SerializeField]
        private float _grabSpread;

        [SerializeField]
        private Transform _uiRay;

        public float MaxCurlAngle => _maxCurlAngle;

        public float CurlAngleRange => _curlAngleRange;

        public float Radius => _radius;

        public Vector3 FingerDiskDirection => _fingerDiskDirection;

        public Vector3 FingerUpDirection => _fingerUpDirection;

        public float GrabSpread => _grabSpread;

        public Transform UIRay => _uiRay;

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.Translate(transform.position) *
                            Matrix4x4.TRS(Vector3.zero, transform.parent.rotation, Vector3.one) *
                            Matrix4x4.Rotate(Quaternion.LookRotation(_fingerDiskDirection)) *
                            Matrix4x4.Scale(new Vector3(1, 1, 0));
            Gizmos.DrawWireSphere(Vector3.zero, _radius);
            Gizmos.matrix = Matrix4x4.Translate(transform.position) * Matrix4x4.TRS(Vector3.zero, transform.parent.rotation, Vector3.one);
            Gizmos.DrawRay(Vector3.zero, _fingerDiskDirection);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.parent.rotation, Vector3.one);
            Gizmos.DrawRay(Vector3.zero, _fingerUpDirection);
        }
    }
}