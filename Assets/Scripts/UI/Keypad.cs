using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.UI
{
    public class Keypad : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _input;

        [SerializeField]
        private string _passcode;

        [SerializeField]
        private UnityEvent _onCorrect;

        [SerializeField]
        private UnityEvent _onFailed;

        private readonly StringBuilder _inputBuilder = new();

        private void Start()
        {
            _input.SetText("");
        }

        public void Type(string symbol)
        {
            _inputBuilder.Append(symbol);
            _input.SetText(_inputBuilder);

            if (_inputBuilder.Length >= _passcode.Length)
            {
                if (_passcode == _inputBuilder.ToString())
                {
                    _onCorrect.Invoke();
                }
                else
                {
                    _onFailed.Invoke();
                }

                Clear();
            }
        }

        public void Clear()
        {
            _inputBuilder.Clear();
            _input.SetText(_inputBuilder);
        }
    }
}