using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.UI
{
    public class ThrusterUI : MonoBehaviour
    {
        [SerializeField]
        private Gradient _color;

        [SerializeField]
        private Image _image;

        [SerializeField]
        private ThrustersController _controller;

        private void Update()
        {
            if (!Mathf.Approximately(_image.fillAmount, _controller.Heat))
            {
                _image.fillAmount = _controller.Heat;
                _image.color = _color.Evaluate(_controller.Heat);
            }
        }
    }
}