using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Demo.UI
{
    public class QualityToggle : MonoBehaviour
    {
        [SerializeField]
        private Toggle _toggle;

        [SerializeField]
        private TMP_Text _label;

        public Toggle Toggle => _toggle;

        public TMP_Text Label => _label;
    }
}