using UnityEngine;

namespace UnityEcho.Utils
{
    public static class QuaternionUtil
    {
        public static Quaternion AngVelToDeriv(Quaternion Current, Vector3 AngVel)
        {
            var Spin = new Quaternion(AngVel.x, AngVel.y, AngVel.z, 0f);
            var Result = Spin * Current;
            return new Quaternion(0.5f * Result.x, 0.5f * Result.y, 0.5f * Result.z, 0.5f * Result.w);
        }

        public static Vector3 DerivToAngVel(Quaternion Current, Quaternion Deriv)
        {
            var Result = Deriv * Quaternion.Inverse(Current);
            return new Vector3(2f * Result.x, 2f * Result.y, 2f * Result.z);
        }

        public static Quaternion IntegrateRotation(Quaternion Rotation, Vector3 AngularVelocity, float DeltaTime)
        {
            if (DeltaTime < Mathf.Epsilon)
            {
                return Rotation;
            }
            var Deriv = AngVelToDeriv(Rotation, AngularVelocity);
            var Pred = new Vector4(
                Rotation.x + Deriv.x * DeltaTime,
                Rotation.y + Deriv.y * DeltaTime,
                Rotation.z + Deriv.z * DeltaTime,
                Rotation.w + Deriv.w * DeltaTime).normalized;
            return new Quaternion(Pred.x, Pred.y, Pred.z, Pred.w);
        }

        /// <summary>
        /// Dynamically rotates toward a target direction with ease in/out.
        /// </summary>
        /// <param name="current">
        /// Current rotation.
        /// </param>
        /// <param name="targetDir">
        /// Direction to look toward.
        /// </param>
        /// <param name="angularVelocity">
        /// Current angular velocity in deg/sec (store between frames).
        /// </param>
        /// <param name="acceleration">
        /// How quickly to speed up/slow down (deg/sec²).
        /// </param>
        /// <param name="maxSpeed">
        /// Maximum rotation speed (deg/sec).
        /// </param>
        /// <param name="deltaTime">
        /// Time since last update.
        /// </param>
        public static Quaternion DynamicEaseLook(
            Quaternion current,
            Vector3 targetDir,
            ref float angularVelocity,
            float acceleration,
            float maxSpeed,
            float deltaTime)
        {
            if (targetDir.sqrMagnitude < 0.0001f)
            {
                return current;
            }

            // Get target rotation
            var target = Quaternion.LookRotation(targetDir.normalized);

            // Find shortest angle to target
            var angle = Quaternion.Angle(current, target);

            if (angle < 0.001f)
            {
                angularVelocity = 0f;
                return target; // aligned
            }

            // Determine desired speed (slower when close)
            var desiredSpeed = Mathf.Min(maxSpeed, Mathf.Sqrt(2f * acceleration * angle));

            // Smoothly adjust angular velocity toward desired speed
            angularVelocity = Mathf.MoveTowards(angularVelocity, desiredSpeed, acceleration * deltaTime);

            // Move toward target rotation
            var step = Mathf.Min(angularVelocity * deltaTime / angle, 1f);
            return Quaternion.Slerp(current, target, step);
        }
    }
}