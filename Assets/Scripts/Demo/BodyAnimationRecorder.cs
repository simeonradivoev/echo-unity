using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEcho.Demo
{
    public class BodyAnimationRecorder : MonoBehaviour
    {
        [SerializeField]
        private Transform _parent;

        [SerializeField]
        private InputActionReference _record;

        private HumanoidAnimation _animation;

        private Animator _animator;

        private int _fixedDeltaFrame;

        private bool _isRecording;

        private HumanPose _pose;

        private float _startTime;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _record.action.Enable();
            _record.action.performed += ActionOnperformed;
        }

        private void FixedUpdate()
        {
            if (!_isRecording)
            {
                return;
            }

            var parent = _parent;

            var inverseParentPosition = parent ? parent.worldToLocalMatrix : Matrix4x4.identity;
            var inverseParentRotation = parent ? Quaternion.Inverse(parent.transform.rotation) : Quaternion.identity;

            if (_fixedDeltaFrame % 2 == 0)
            {
                var t = Time.unscaledTime - _startTime;

                _animation.Position.Record(inverseParentPosition.MultiplyPoint(_animator.transform.position), t);
                _animation.Rotation.Record(inverseParentRotation * _animator.transform.rotation, t);

                for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
                {
                    var boneTransform = _animator.GetBoneTransform((HumanBodyBones)i);
                    if (boneTransform)
                    {
                        _animation.Bones[i].Record(boneTransform.localRotation, t);
                    }
                }

                for (var i = 0; i <= (int)AvatarIKGoal.RightHand; i++)
                {
                    _animation.IkPosition[i].Record(inverseParentPosition.MultiplyPoint(_animator.GetIKPosition((AvatarIKGoal)i)), t);
                    _animation.IkRotation[i].Record(inverseParentRotation * _animator.GetIKRotation((AvatarIKGoal)i), t);
                }

                for (var i = 0; i <= (int)AvatarIKHint.RightElbow; i++)
                {
                    _animation.IkHintPositionWeights[i].Record(_animator.GetIKHintPositionWeight((AvatarIKHint)i), t);
                    _animation.IkHintPosition[i].Record(inverseParentPosition.MultiplyPoint(_animator.GetIKHintPosition((AvatarIKHint)i)), t);
                }
            }

            _fixedDeltaFrame++;
        }

        private void OnDestroy()
        {
            _record.action.performed -= ActionOnperformed;

            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;
        }

        private void ActionOnperformed(InputAction.CallbackContext obj)
        {
            if (!_isRecording)
            {
                _animation = ScriptableObject.CreateInstance<HumanoidAnimation>();
                _animation.Reset();
                _animation.FPS = Mathf.RoundToInt(1f / Time.fixedDeltaTime) / 2;
                _startTime = Time.unscaledTime;
                _isRecording = true;
            }
            else
            {
#if UNITY_EDITOR
                AssetDatabase.CreateAsset(_animation, $"Assets/Animations/Recordings/{DateTime.Now:yyyy-dd-M--HH-mm-ss}.asset");
                AssetDatabase.SaveAssets();
#endif
                _isRecording = false;
            }
        }
    }
}