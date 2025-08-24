using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Demo
{
    public class LookAtTrigger : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent _onTrigger;

        [SerializeField]
        private Transform _target;

        [SerializeField]
        private float _maxDistance;

        [SerializeField]
        private float _maxAngle;

        [SerializeField]
        private bool _checkCollisions;

        [SerializeField]
        private LayerMask _layerMask;

        [SerializeField]
        private float _delay;

        [SerializeField]
        private float _closeTriggerRadius;

        private Camera _camera;

        private float _timer;

        private bool _triggered;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            if (_triggered)
            {
                return;
            }

            var dir = _target.position - _camera.transform.position;
            var distanceSq = dir.sqrMagnitude;

            var rangeSq = _maxDistance * _maxDistance;
            if (distanceSq > rangeSq)
            {
                _timer = 0;
                return;
            }

            var closeRangeSq = _closeTriggerRadius * _closeTriggerRadius;

            if (distanceSq > closeRangeSq)
            {
                var distance = Mathf.Sqrt(distanceSq);
                dir /= distance;
                var angle = Vector3.Angle(dir, _camera.transform.forward);
                if (angle > _maxAngle)
                {
                    _timer = 0;
                    return;
                }

                if (_checkCollisions && Physics.Raycast(_camera.transform.position, dir, distance, _layerMask))
                {
                    _timer = 0;
                    return;
                }
            }

            _timer += Time.deltaTime;

            if (_timer < _delay)
            {
                return;
            }

            _onTrigger.Invoke();
            _triggered = true;
        }

        private void OnDrawGizmosSelected()
        {
            if (_target)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(_target.position, _maxDistance);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_target.position, _closeTriggerRadius);
            }
        }
    }
}