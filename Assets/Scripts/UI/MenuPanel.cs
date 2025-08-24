using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEcho.UI
{
    public class MenuPanel : MonoBehaviour
    {
        [SerializeField]
        [TextArea]
        private string _exitButtonLabel;

        [SerializeField]
        private TabletPanelButton _tabletButtonPrefab;

        [SerializeField]
        private Transform _parent;

        private Tablet _tablet;

        private void Start()
        {
            _tablet = GetComponentInParent<Tablet>();
            foreach (var panel in _tablet.Panels)
            {
                if (string.IsNullOrEmpty(panel.Label))
                {
                    continue;
                }

                var button = Instantiate(_tabletButtonPrefab, _parent);
                button.Button.onClick.AddListener(() => _tablet.SetActivePanel(panel));
                button.Label.text = Regex.Unescape(panel.Label);
            }

            if (!string.IsNullOrEmpty(_exitButtonLabel))
            {
                var exitButton = Instantiate(_tabletButtonPrefab, _parent);
                exitButton.Button.onClick.AddListener(() => Application.Quit());
                exitButton.Label.text = Regex.Unescape(_exitButtonLabel);
            }
        }
    }
}