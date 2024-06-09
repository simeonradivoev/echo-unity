using System;
using UnityEcho.Mechanics.Utils;
using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Used to move the head collider.
    /// It also adds velocity ot all pushed objects including ourselves since just moving the collider inside the rigidbody doesnt change its velocity.
    /// </summary>
    public class HeadFollower : MonoBehaviour
    {
        [SerializeField]
        private Transform _head;

        [SerializeField]
        private Collider _toMove;

        [SerializeField]
        private Transform _specatorCam;

        [SerializeField]
        private float _spectatorCamSmoothing;

        private Vector3 _lastPos;

        private Rigidbody _rigidbody;

        private Quaternion _smoothingRotationVelocity;

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.automaticCenterOfMass = false;
        }

        private void Update()
        {
            _specatorCam.transform.position = _head.position;
            _specatorCam.transform.rotation = QuaternionUtil.SmoothDamp(
                _specatorCam.rotation,
                _head.rotation,
                ref _smoothingRotationVelocity,
                _spectatorCamSmoothing);
        }

        private void FixedUpdate()
        {
            _rigidbody.centerOfMass = _rigidbody.transform.InverseTransformPoint(_head.position);
            var dir = _head.position - _toMove.transform.position;
            var dirLength = dir.magnitude;
            if (dirLength > 0)
            {
                var hits = _toMove.attachedRigidbody.SweepTestAll(dir / dirLength, dirLength);
                if (hits.Length > 0)
                {
                    foreach (var hit in hits)
                    {
                        if (hit.rigidbody)
                        {
                            hit.rigidbody.AddForceAtPosition(dir * _toMove.attachedRigidbody.mass, hit.point, ForceMode.Impulse);
                            _toMove.attachedRigidbody.AddForceAtPosition(-dir * hit.rigidbody.mass, hit.point, ForceMode.Impulse);
                        }
                        else
                        {
                            _toMove.attachedRigidbody.AddForceAtPosition(-dir * _toMove.attachedRigidbody.mass * 2, hit.point, ForceMode.Impulse);
                        }
                    }
                }
            }

            _toMove.transform.position += dir;
        }

        private void OnCollisionEnter(Collision other)
        {
            Collision?.Invoke(other);
        }

        public event Action<Collision> Collision;
    }
}