using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.UI
{
    public class ProgressBarConnector : MonoBehaviour
    {
        [SerializeField]
        private Image _image;

        public void UpdateProgress(float normalizedValue)
        {
            _image.fillAmount = normalizedValue;
        }
    }
}