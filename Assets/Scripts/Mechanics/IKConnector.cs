using System;
using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Simple callback for IK
    /// </summary>
    public class IKConnector : MonoBehaviour
    {
        private void OnAnimatorIK(int layerIndex)
        {
            AnimatorIK?.Invoke(layerIndex);
        }

        public event Action<int> AnimatorIK;
    }
}