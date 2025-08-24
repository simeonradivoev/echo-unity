using UnityEcho.Interactables;
using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UnityEcho.Demo
{
    public class AirlockController : MonoBehaviour
    {
        private static readonly int InteriorOpen = Animator.StringToHash("InteriorOpen");

        [SerializeField]
        private Button _interiorButton;

        [SerializeField]
        private Button _exteriorButton;

        [SerializeField]
        private Door _interiorDoor;

        [SerializeField]
        private Door _exteriorDoor;

        [SerializeField]
        private LinearJointInteractable _lock;

        [SerializeField]
        private float _cycleDelay;

        [SerializeField]
        private Volume _volume;

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        [HideInInspector]
        private bool _inCycle;

        [SerializeField]
        [HideInInspector]
        private float _atmosphere;

        [SerializeField]
        [HideInInspector]
        private bool _isInteriorDoorOpen;

        private EnvironmentSettings _environmentSettings;

        private float _vacuumVel;

        private void Start()
        {
            _environmentSettings = _volume.profile.Add<EnvironmentSettings>();
            _environmentSettings.atmosphere.overrideState = true;

            _lock.On.AddListener(
                () =>
                {
                    if (_isInteriorDoorOpen)
                    {
                        _interiorDoor.SetClosed(false);
                    }
                    else
                    {
                        _animator.SetBool(InteriorOpen, true);
                    }
                });
            _lock.Off.AddListener(
                () =>
                {
                    if (_isInteriorDoorOpen)
                    {
                        _interiorDoor.SetClosed(true);
                    }
                });
            _interiorButton.onClick.AddListener(() => _animator.SetBool(InteriorOpen, true));
            _exteriorButton.onClick.AddListener(() => _animator.SetBool(InteriorOpen, false));
        }

        private void Update()
        {
            _environmentSettings.atmosphere.value = _atmosphere;
            _interiorButton.interactable = !_inCycle && _lock.IsOn;
            _exteriorButton.interactable = !_inCycle;
            _lock.Interactable = !_inCycle;
        }

        public void SetInteriorDoorClosed()
        {
            _interiorDoor.SetClosed(true);
        }

        public void SetInteriorDoorOpen()
        {
            _interiorDoor.SetClosed(false);
        }

        public void SetExteriorDoorClosed()
        {
            _exteriorDoor.SetClosed(true);
        }

        public void SetExteriorDoorOpen()
        {
            _exteriorDoor.SetClosed(false);
        }
    }
}