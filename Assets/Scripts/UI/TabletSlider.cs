using UnityEngine;

namespace UnityEcho.UI
{
    public class TabletSlider : MonoBehaviour
    {
        private UnlockSlider _slider;

        private Tablet _tablet;

        private void Start()
        {
            _tablet = FindObjectOfType<Tablet>();
            _slider = GetComponent<UnlockSlider>();
            _slider.OnPerformed.AddListener(OpenTablet);
        }

        private void OpenTablet()
        {
            _tablet.GetComponent<TabletReleaseHandler>().Show();
        }
    }
}