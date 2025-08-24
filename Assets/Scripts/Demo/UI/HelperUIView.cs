using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.UI
{
    public class HelperUIView : MonoBehaviour
    {
        [SerializeField]
        private InputActionReference _grab;

        [SerializeField]
        private InputActionReference _rapidStop;

        [SerializeField]
        private InputActionReference _boost;

        [SerializeField]
        private InputActionReference _menu;

        [SerializeField]
        private InputActionReference _rotate;

        [SerializeField]
        private TMP_Text _text;

        private void Start()
        {
            var re = new Regex(@"\{(\w*?)\}", RegexOptions.Compiled);

            var vals = new Dictionary<string, string>();
            vals.Add("grab", _grab.action.GetBindingDisplayString());
            vals.Add("rapidStop", _rapidStop.action.GetBindingDisplayString());
            vals.Add("boost", _boost.action.GetBindingDisplayString());
            vals.Add("menu", _menu.action.GetBindingDisplayString());
            vals.Add("rotate", _rotate.action.GetBindingDisplayString());

            _text.text = re.Replace(
                _text.text,
                delegate(Match match)
                {
                    var key = match.Groups[1].Value;
                    return vals[key];
                });
        }
    }
}