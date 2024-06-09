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
            lhs.connectedMassScale = rhs.connectedMassScale;
            lhs.autoConfigureConnectedAnchor = rhs.autoConfigureConnectedAnchor;
        }
    }
}