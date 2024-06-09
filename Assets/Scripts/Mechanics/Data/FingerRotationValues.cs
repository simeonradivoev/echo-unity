using UnityEngine;

namespace UnityEcho.Mechanics.Data
{
    [CreateAssetMenu]
    public class FingerRotationValues : ScriptableObject
    {
        public Vector3[] GrabFingerRotations = new Vector3[5];

        public Vector3[] GrabFingerHitRotations = new Vector3[5];

        public Vector3[] IdleFingerRotations = new Vector3[5];

        public Vector3[] FistFingerRotations = new Vector3[5];

        public Vector3 IndexPointRotations;
    }
}