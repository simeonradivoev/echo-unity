using UnityEcho.Interactables;
using UnityEngine;

namespace UnityEcho.Demo
{
    public class BrideControls : MonoBehaviour
    {
        [SerializeField]
        private Receptacle _receptacle;

        [SerializeField]
        private LinearJointInteractable _batterySlider;

        [SerializeField]
        private HingeJoinInteractable _bridgeLever;

        [SerializeField]
        private Rigidbody _bridgeLeverRigidbody;

        private void Start()
        {
            UpdateBridgeControls();
            _receptacle.OnActivate.AddListener(UpdateBridgeControls);
            _receptacle.OnDeactivate.AddListener(UpdateBridgeControls);
            _batterySlider.On.AddListener(UpdateBridgeControls);
            _batterySlider.Off.AddListener(UpdateBridgeControls);
        }

        private void UpdateBridgeControls()
        {
            _bridgeLever.IsInteractable = _receptacle.IsActive && _batterySlider.IsOn;
            _bridgeLeverRigidbody.isKinematic = !(_receptacle.IsActive && _batterySlider.IsOn);
        }
    }
}