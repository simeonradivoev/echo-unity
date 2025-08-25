using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEcho.Demo
{
    [CreateAssetMenu]
    public class HumanoidAnimation : ScriptableObject
    {
        public int FPS;

        public Vector3Curves Position;

        public Vector4Curves Rotation;

        public Vector4Curves[] Bones;

        public Vector1Curve[] IkHintPositionWeights;

        public Vector3Curves[] IkPosition;

        public Vector3Curves[] IkHintPosition;

        public Vector4Curves[] IkRotation;

        public void Reset()
        {
            Position = new Vector3Curves();
            Rotation = new Vector4Curves();

            var boneCount = (int)HumanBodyBones.LastBone;
            Bones = new Vector4Curves[boneCount];

            for (var i = 0; i < boneCount; i++)
            {
                Bones[i] = new Vector4Curves();
            }

            var ikGoalCount = (int)AvatarIKGoal.RightHand + 1;

            IkPosition = new Vector3Curves[ikGoalCount];
            IkRotation = new Vector4Curves[ikGoalCount];

            for (var i = 0; i < ikGoalCount; i++)
            {
                IkPosition[i] = new Vector3Curves();
                IkRotation[i] = new Vector4Curves();
            }

            var ikHintCount = (int)AvatarIKHint.RightElbow + 1;

            IkHintPositionWeights = new Vector1Curve[ikHintCount];
            IkHintPosition = new Vector3Curves[ikHintCount];

            for (var i = 0; i < ikHintCount; i++)
            {
                IkHintPositionWeights[i] = new Vector1Curve();
                IkHintPosition[i] = new Vector3Curves();
            }
        }

        public void SetLoop(WrapMode mode)
        {
            Position.SetLoop(mode);
            Rotation.SetLoop(mode);

            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                Bones[i].SetLoop(mode);
            }

            for (var i = 0; i <= (int)AvatarIKGoal.RightHand; i++)
            {
                IkPosition[i].SetLoop(mode);
                IkRotation[i].SetLoop(mode);
            }

            for (var i = 0; i <= (int)AvatarIKHint.RightElbow; i++)
            {
                IkHintPositionWeights[i].SetLoop(mode);
                IkHintPosition[i].SetLoop(mode);
            }
        }

        public void Clear()
        {
            Position.Clear();
            Rotation.Clear();

            for (var i = 0; i < (int)HumanBodyBones.LastBone; i++)
            {
                Bones[i].Clear();
            }

            for (var i = 0; i <= (int)AvatarIKGoal.RightHand; i++)
            {
                IkPosition[i].Clear();
                IkRotation[i].Clear();
            }

            for (var i = 0; i <= (int)AvatarIKHint.RightElbow; i++)
            {
                IkHintPositionWeights[i].Clear();
                IkHintPosition[i].Clear();
            }
        }

        [Serializable]
        public class Vector1Curve
        {
            public enum EaseType { Linear, Spline }

            public List<float> X = new();

            protected void AddKey(List<float> curve, float value, float time)
            {
                curve.Add(value);
            }

            public float Sample(List<float> data, int fps, float time, EaseType easeType = EaseType.Spline)
            {
                if (easeType is EaseType.Spline)
                {
                    var count = data.Count;
                    if (count < 2)
                    {
                        return default;
                    }

                    var frame = time * fps;
                    var i1 = Mathf.FloorToInt(frame) % count;
                    var i2 = (i1 + 1) % count;
                    var i0 = (i1 - 1 + count) % count;
                    var i3 = (i2 + 1) % count;

                    var t = frame - Mathf.Floor(frame); // fractional part

                    var p0 = data[i0];
                    var p1 = data[i1];
                    var p2 = data[i2];
                    var p3 = data[i3];

                    var t2 = t * t;
                    var t3 = t2 * t;

                    return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                }

                if (data.Count <= 0)
                {
                    return default;
                }
                var indexPrev = Mathf.FloorToInt(time * fps) % data.Count;
                var indexNext = Mathf.CeilToInt(time * fps) % data.Count;
                var frac = time * fps % 1f;
                return Mathf.Lerp(data[indexPrev], data[indexNext], frac);
            }

            public float Sample(int fps, float time, EaseType easeType = EaseType.Spline)
            {
                return Sample(X, fps, time, easeType);
            }

            public void Record(float value, float time)
            {
                AddKey(X, value, time);
            }

            public virtual void SetLoop(WrapMode mode)
            {
                //X.postWrapMode = mode;
            }

            public virtual void Clear()
            {
                X.Clear();
            }
        }

        [Serializable]
        public class Vector3Curves : Vector1Curve
        {
            public List<float> Y = new();

            public List<float> Z = new();

            public Vector3 Sample(int fps, float time, EaseType easeType = EaseType.Spline)
            {
                return new Vector3(Sample(X, fps, time, easeType), Sample(Y, fps, time, easeType), Sample(Z, fps, time, easeType));
            }

            public void Record(Vector3 value, float time)
            {
                AddKey(X, value.x, time);
                AddKey(Y, value.y, time);
                AddKey(Z, value.z, time);
            }

            public override void SetLoop(WrapMode mode)
            {
                base.SetLoop(mode);
                //Y.postWrapMode = mode;
                //Z.postWrapMode = mode;
            }

            public override void Clear()
            {
                base.Clear();
                Y.Clear();
                Z.Clear();
            }
        }

        [Serializable]
        public class Vector4Curves : Vector3Curves
        {
            public List<float> W = new();

            public Quaternion SampleQuaternion(int fps, float t, EaseType easeType = EaseType.Spline)
            {
                return new Quaternion(
                    Sample(X, fps, t, easeType),
                    Sample(Y, fps, t, easeType),
                    Sample(Z, fps, t, easeType),
                    Sample(W, fps, t, easeType));
            }

            public Vector4 Sample(int fps, float t, EaseType easeType = EaseType.Spline)
            {
                return new Vector4(
                    Sample(X, fps, t, easeType),
                    Sample(Y, fps, t, easeType),
                    Sample(Z, fps, t, easeType),
                    Sample(W, fps, t, easeType));
            }

            public void Record(Quaternion val, float t)
            {
                AddKey(X, val.x, t);
                AddKey(Y, val.y, t);
                AddKey(Z, val.z, t);
                AddKey(W, val.w, t);
            }

            public void Record(Vector4 val, float t)
            {
                AddKey(X, val.x, t);
                AddKey(Y, val.y, t);
                AddKey(Z, val.z, t);
                AddKey(W, val.w, t);
            }

            #region Overrides of Vector3Curves

            public override void SetLoop(WrapMode mode)
            {
                base.SetLoop(mode);
                //W.postWrapMode = mode;
            }

            public override void Clear()
            {
                base.Clear();
                W.Clear();
            }

            #endregion
        }
    }
}