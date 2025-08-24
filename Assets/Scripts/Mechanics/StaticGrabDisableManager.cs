using System.Collections.Generic;
using UnityEcho.Mechanics.Data;
using UnityEngine;

namespace UnityEcho.Mechanics
{
    /// <summary>
    /// Ensures there are always just 1 static joint active at a time.
    /// In most cases the newest static joint should be the active one, this is desired when moving around by alternating grabs.
    /// Otherwise the player will jump around.
    /// </summary>
    public class StaticGrabDisableManager : MonoBehaviour
    {
        [SerializeField]
        private float _mainConnectedMass;

        [SerializeField]
        private float _secondaryConnectedMass;

        private readonly List<GrabMoveController> _staticActiveControllers = new();

        private GrabMoveController[] _allControllers;

        private void Start()
        {
            _allControllers = GetComponentsInChildren<GrabMoveController>();
            foreach (var controller in _allControllers)
            {
                controller.Grabbed += ControllerOnGrabbed;
                controller.Released += ControllerOnReleased;
            }
        }

        private void OnDestroy()
        {
            foreach (var controller in _allControllers)
            {
                controller.Grabbed -= ControllerOnGrabbed;
                controller.Released -= ControllerOnReleased;
            }
        }

        private void ControllerOnReleased(GrabMoveController obj)
        {
            if (_staticActiveControllers.Remove(obj))
            {
                UpdateJoints();
            }
        }

        private void UpdateJoints()
        {
            for (var i = 0; i < _staticActiveControllers.Count; i++)
            {
                var isLastOne = i == _staticActiveControllers.Count - 1;
                var joint = _staticActiveControllers[i].StaticGrabJoint;
                joint.connectedMassScale = isLastOne ? _mainConnectedMass : _secondaryConnectedMass;
                _staticActiveControllers[i].Secondary = !isLastOne;
            }
        }

        private void ControllerOnGrabbed(GrabHitData obj, GrabMoveController controller)
        {
            if (controller.StaticGrabJoint)
            {
                _staticActiveControllers.Add(controller);
                UpdateJoints();
            }
        }
    }
}