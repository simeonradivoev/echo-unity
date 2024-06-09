using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEcho.Mechanics
{
    public class GrabObject : MonoBehaviour
    {
        /// <summary>
        /// Should the object be moved using joins or directly moved.
        /// </summary>
        [SerializeField]
        private bool _virtual;

        [SerializeField]
        private FloatParameter _objectMassScaleOverride;

        [SerializeField]
        private FloatParameter _handMassScaleOverride;

        [SerializeField]
        private FloatParameter _playerMassScaleOverride;

        [SerializeField]
        private float _handPositionMotorMultiply = 1;

        [SerializeField]
        private float _handRotationMotorMultiply = 1;

        public bool Virtual => _virtual;

        public FloatParameter PlayerMassScaleOverride => _playerMassScaleOverride;

        public FloatParameter ObjectMassScaleOverride => _objectMassScaleOverride;

        public FloatParameter HandMassScaleOverride => _handMassScaleOverride;

        public float HandPositionMotorMultiply => _handPositionMotorMultiply;

        public float HandRotationMotorMultiply => _handRotationMotorMultiply;
    }
}