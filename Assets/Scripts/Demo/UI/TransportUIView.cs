using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.Demo
{
    public class TransportUIView : MonoBehaviour
    {
        [SerializeField]
        private TransportController _transportController;

        [SerializeField]
        private Button _startButton;

        [SerializeField]
        private Slider _speedSlider;

        [SerializeField]
        private Slider _speedSliderSmooth;

        [SerializeField]
        private TMP_Text _speed;

        private void Start()
        {
            _speedSlider.value = _transportController.SpeedLevel;
            _speedSlider.onValueChanged.AddListener(OnSpeedLevelChange);
            _startButton.onClick.AddListener(OnStart);
        }

        private void Update()
        {
            _speedSliderSmooth.value = _transportController.SpeedLevelSmooth;
            _speed.text = $"{_transportController.Velocity.magnitude:F1} m/s";
        }

        private void OnDestroy()
        {
            _speedSlider.onValueChanged.RemoveListener(OnSpeedLevelChange);
            _startButton.onClick.RemoveListener(OnStart);
        }

        private void OnStart()
        {
            _transportController.StartNow();
        }

        private void OnSpeedLevelChange(float speedLevel)
        {
            _transportController.SpeedLevel = speedLevel;
        }
    }
}