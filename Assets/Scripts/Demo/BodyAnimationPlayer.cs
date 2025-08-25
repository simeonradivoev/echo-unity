using UnityEngine;

namespace UnityEcho.Demo
{
    public class BodyAnimationPlayer : MonoBehaviour
    {
        [SerializeField]
        private HumanoidAnimation _animation;

        [SerializeField]
        private WrapMode _wrapMode;

        [SerializeField]
        private Quaternion _headRotationOffset = Quaternion.identity;

        private Animator _animator;

        private Rigidbody _rigidbody;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody>();
            if (_animation)
            {
                _animation.SetLoop(_wrapMode);
            }
        }

        private void Update()
        {
            var time = Time.unscaledTime;
            if (!_animation)
            {
                return;
            }

            if (_rigidbody)
            {
                var parentSpace = _rigidbody.transform.parent ? _rigidbody.transform.parent.localToWorldMatrix : Matrix4x4.identity;
                var parentRotation = _rigidbody.transform.parent ? _rigidbody.transform.parent.rotation : Quaternion.identity;

                _rigidbody.MovePosition(parentSpace.MultiplyPoint(_animation.Position.Sample(_animation.FPS, time)));
                _rigidbody.MoveRotation(parentRotation * _animation.Rotation.SampleQuaternion(_animation.FPS, time));
            }
            else
            {
                _animator.transform.localPosition = _animation.Position.Sample(_animation.FPS, time);
                _animator.transform.localRotation = _animation.Rotation.SampleQuaternion(_animation.FPS, time);
            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_animation)
            {
                return;
            }

            var parent = transform.parent;
            var parentSpace = parent ? parent.localToWorldMatrix : Matrix4x4.identity;
            var parentRot = parent ? parent.rotation : Quaternion.identity;

            var time = Time.unscaledTime;

            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                if (i == (int)HumanBodyBones.Head)
                {
                    _animator.SetBoneLocalRotation(
                        (HumanBodyBones)i,
                        _headRotationOffset * _animation.Bones[i].SampleQuaternion(_animation.FPS, time));
                }
                else
                {
                    _animator.SetBoneLocalRotation((HumanBodyBones)i, _animation.Bones[i].SampleQuaternion(_animation.FPS, time));
                }
            }

            for (var i = 0; i <= (int)AvatarIKHint.RightElbow; i++)
            {
                _animator.SetIKHintPositionWeight((AvatarIKHint)i, _animation.IkHintPositionWeights[i].Sample(_animation.FPS, time));
                _animator.SetIKHintPosition((AvatarIKHint)i, parentSpace.MultiplyPoint(_animation.IkHintPosition[i].Sample(_animation.FPS, time)));
            }

            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);

            for (var i = 0; i <= (int)AvatarIKGoal.RightHand; i++)
            {
                _animator.SetIKPosition((AvatarIKGoal)i, parentSpace.MultiplyPoint(_animation.IkPosition[i].Sample(_animation.FPS, time)));
                _animator.SetIKRotation((AvatarIKGoal)i, parentRot * _animation.IkRotation[i].SampleQuaternion(_animation.FPS, time));
            }
        }
    }
}