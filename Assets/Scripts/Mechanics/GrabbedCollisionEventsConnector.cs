using System.Collections.Generic;
using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Used to get collision events from a target
    /// </summary>
    public class GrabbedCollisionEventsConnector : MonoBehaviour
    {
        public HashSet<Collider> Colliders { get; } = new();

        public HashSet<Collision> Collisions { get; } = new();

        private void OnCollisionEnter(Collision other)
        {
            Colliders.Add(other.collider);
            Collisions.Add(other);
        }

        private void OnCollisionExit(Collision other)
        {
            Colliders.Remove(other.collider);
            Collisions.Remove(other);
        }
    }
}