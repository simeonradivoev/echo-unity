using UnityEngine;

namespace UnityEcho.Mechanics.Utils
{
    public static class JointExtension
    {
        public static void CopyJoint(this ConfigurableJoint lhs, ConfigurableJoint rhs)
        {
            lhs.xMotion = rhs.xMotion;
            lhs.yMotion = rhs.yMotion;
            lhs.zMotion = rhs.zMotion;
            lhs.xDrive = rhs.xDrive;
            lhs.yDrive = rhs.yDrive;
            lhs.zDrive = rhs.zDrive;
            lhs.angularXMotion = rhs.angularXMotion;
            lhs.angularYMotion = rhs.angularYMotion;
            lhs.angularZMotion = rhs.angularZMotion;
            lhs.linearLimit = rhs.linearLimit;
            lhs.linearLimitSpring = rhs.linearLimitSpring;
            lhs.rotationDriveMode = rhs.rotationDriveMode;
            lhs.angularXDrive = rhs.angularXDrive;
            lhs.angularYZDrive = rhs.angularYZDrive;
            lhs.slerpDrive = rhs.slerpDrive;
            lhs.swapBodies = rhs.swapBodies;
            lhs.axis = rhs.axis;
            lhs.massScale = rhs.massScale;
            lhs.anchor = rhs.anchor;
            lhs.connectedMassScale = rhs.connectedMassScale;
            lhs.enableCollision = rhs.enableCollision;
            lhs.enablePreprocessing = rhs.enablePreprocessing;
            lhs.autoConfigureConnectedAnchor = rhs.autoConfigureConnectedAnchor;
            lhs.targetPosition = rhs.targetPosition;
            lhs.targetVelocity = rhs.targetVelocity;
            lhs.targetAngularVelocity = rhs.targetAngularVelocity;
            lhs.targetRotation = rhs.targetRotation;
            lhs.rotationDriveMode = rhs.rotationDriveMode;
            lhs.projectionAngle = rhs.projectionAngle;
            lhs.projectionMode = rhs.projectionMode;
            lhs.projectionDistance = rhs.projectionDistance;
            lhs.configuredInWorldSpace = rhs.configuredInWorldSpace;
            lhs.angularXLimitSpring = rhs.angularXLimitSpring;
            lhs.lowAngularXLimit = rhs.lowAngularXLimit;
            lhs.highAngularXLimit = rhs.highAngularXLimit;
            lhs.angularYZLimitSpring = rhs.angularYZLimitSpring;
            lhs.angularYLimit = rhs.angularYLimit;
            lhs.angularZLimit = rhs.angularZLimit;
            lhs.breakForce = rhs.breakForce;
            lhs.breakTorque = rhs.breakTorque;
        }
    }
}