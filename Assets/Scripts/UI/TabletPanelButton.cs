using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.UI
{
    public class TabletPanelButton : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _label;

        [SerializeField]
        private Button _button;

        public TMP_Text Label => _label;

        public Button Button => _button;
    }
}