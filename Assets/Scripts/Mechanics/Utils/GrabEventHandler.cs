using System;
using UnityEcho.Mechanics.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Mechanics.Utils
{
    public class GrabEventHandler : MonoBehaviour, IGrabHandler, IReleaseHandler
    {
        [SerializeField]
        public GrabUnityEvent _onGrab;

        [SerializeField]
        public ReleaseUnityEvent _onRelease;

        #region Implementation of IGrabHandler

        public void OnGrab(GrabContext context)
        {
            _onGrab.Invoke(context);
        }

        #endregion

        #region Implementation of IReleaseHandler

        public void OnRelease(ReleaseContext context)
        {
            _onRelease.Invoke(context);
        }

        #endregion

        [Serializable]
        public class GrabUnityEvent : UnityEvent<GrabContext>
        {
        }

        [Serializable]
        public class ReleaseUnityEvent : UnityEvent<ReleaseContext>
        {
        }
    }
}