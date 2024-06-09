using UnityEngine;

namespace UnityEcho.Mechanics.Data
{
    public struct GrabHitData
    {
        public Vector3 Position;

        public Vector3 LocalPosition;

        public Vector3 Normal;

        public Vector3 Up;

        public Collider Collider;

        public int Triangle;
    }
}