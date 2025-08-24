using Unity.Collections;
using UnityEcho.Mechanics.Utils;
using UnityEngine;

namespace Mechanics.Utils
{
    public class GrabHelper : MonoBehaviour, IGrabHandler, IReleaseHandler
    {
        private readonly Vector3[] _points = new Vector3[2];

        private BitField32 _grabs;

        public bool IsGrabbed => _grabs.CountBits() > 0;

        public Vector3 LocalAverageGrabPosition { get; private set; }

        public Vector3 WorldAverageGrabPosition => transform.TransformPoint(LocalAverageGrabPosition);

        #region Implementation of IGrabHandler

        public void OnGrab(GrabContext context)
        {
            _points[context.Left ? 1 : 0] = transform.InverseTransformPoint(context.Position);
            _grabs.SetBits(context.Left ? 1 : 0, true);
            CalculateAverage();
        }

        #endregion

        #region Implementation of IReleaseHandler

        public void OnRelease(ReleaseContext context)
        {
            _grabs.SetBits(context.Left ? 1 : 0, false);
            CalculateAverage();
        }

        #endregion

        public bool HasGrab(bool left)
        {
            return _grabs.IsSet(left ? 1 : 0);
        }

        public Vector3 GetLocalGrab(bool left)
        {
            return _points[left ? 1 : 0];
        }

        private void CalculateAverage()
        {
            if (_grabs.CountBits() > 0)
            {
                LocalAverageGrabPosition = Vector3.zero;
                for (var i = 0; i < 2; i++)
                {
                    if (_grabs.IsSet(i))
                    {
                        LocalAverageGrabPosition += _points[i];
                    }
                }

                LocalAverageGrabPosition /= _grabs.CountBits();
            }
        }
    }
}