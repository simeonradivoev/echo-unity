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
        private PlayerReferences _references;

        [SerializeField]
        private Transform _specatorCam;

        [SerializeField]
        private float _spectatorCamSmoothing;

        private Quaternion _smoothingRotationVelocity;

        public bool SpectatorCameraActive => _specatorCam.gameObject.activeSelf;

        private void Reset()
        {
            _references = GetComponent<PlayerReferences>();
        }

        private void Start()
        {
            _references.Body.automaticCenterOfMass = false;
        }

        private void Update()
        {
            _specatorCam.transform.position = _references.Head.position;
            _specatorCam.transform.rotation = QuaternionUtil.SmoothDamp(
                _specatorCam.rotation,
                _references.Head.rotation,
                ref _smoothingRotationVelocity,
                _spectatorCamSmoothing);
        }

        private void FixedUpdate()
        {
            var head = _references.Head;
            var toMove = _references.HeadCollider;

            _references.Body.centerOfMass = _references.Body.transform.InverseTransformPoint(head.position);
            var dir = head.position - toMove.transform.position;
            var dirLength = dir.magnitude;
            if (dirLength > 0)
            {
                var hits = toMove.attachedRigidbody.SweepTestAll(dir / dirLength, dirLength);
                if (hits.Length > 0)
                {
                    foreach (var hit in hits)
                    {
                        if (hit.rigidbody)
                        {
                            hit.rigidbody.AddForceAtPosition(dir * toMove.attachedRigidbody.mass, hit.point, ForceMode.Impulse);
                            toMove.attachedRigidbody.AddForceAtPosition(-dir * hit.rigidbody.mass, hit.point, ForceMode.Impulse);
                        }
                        else
                        {
                            toMove.attachedRigidbody.AddForceAtPosition(-dir * toMove.attachedRigidbody.mass * 2, hit.point, ForceMode.Impulse);
                        }
                    }
                }
            }

            toMove.transform.position += dir;
        }

        private void OnCollisionEnter(Collision other)
        {
            Collision?.Invoke(other);
        }

        public void SetSpeculatorCamera(bool active)
        {
            _specatorCam.gameObject.SetActive(active);
        }

        public event Action<Collision> Collision;
    }
}