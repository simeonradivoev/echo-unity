using UnityEngine;

namespace UnityEcho.Mechanics.Utils
{
    public struct ReleaseContext
    {
        public Vector3 Velocity;

        public Vector3 AngularVelocity;

        public bool Left;
    }

    public struct GrabContext
    {
        public bool Left;
    }

    public interface IReleaseHandler
    {
        void OnRelease(ReleaseContext context);
    }

    public interface IGrabHandler
    {
        void OnGrab(GrabContext context);
    }
}