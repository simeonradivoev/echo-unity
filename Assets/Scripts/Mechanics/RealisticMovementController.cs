using Unity.Mathematics;
using UnityEngine;

namespace UnityEcho.Mechanics
{
    public class RealisticMovementController : MonoBehaviour
    {
        [SerializeField]
        private bool _defaultRealisticMovement;

        [SerializeField]
        private PlayerReferences _references;

        public bool IsRealisticMovement { get; private set; }

        private void Awake()
        {
            SetRealisticMovement(_defaultRealisticMovement);
        }

        public void SetRealisticMovement(bool realistic)
        {
            IsRealisticMovement = realistic;

            if (realistic)
            {
                _references.Body.constraints = RigidbodyConstraints.None;
            }
            else
            {
                _references.Body.MoveRotation(quaternion.identity);
                _references.Body.constraints = RigidbodyConstraints.FreezeRotation;
            }

            foreach (var grabMoveController in GetComponentsInChildren<GrabMoveController>())
            {
                grabMoveController.SetPhysicallyAccurateRelease(realistic);
            }
        }
    }
}