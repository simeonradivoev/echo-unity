using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.UI
{
    public class Tablet : MonoBehaviour
    {
        [SerializeField]
        private TabletPanel _defaultPanel;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private TMP_Text _title;

        private readonly Stack<TabletPanel> _stack = new();

        public TabletPanel[] Panels { get; private set; }

        private void Awake()
        {
            Panels = transform.GetComponentsInChildren<TabletPanel>().ToArray();
        }

        private void Start()
        {
            SetActivePanel(_defaultPanel);
            _backButton.onClick.AddListener(Return);
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(Return);
        }

        public event Action<TabletPanel> OnPanelActivate;

        public void ExitApplication()
        {
            Application.Quit();
        }

        private void Return()
        {
            _stack.Pop();
            UpdatePanels();
        }

        private void UpdatePanels()
        {
            _stack.Peek().SetActive(true);

            foreach (var otherPanel in Panels.Where(p => p != _stack.Peek()))
            {
                otherPanel.SetActive(false);
            }

            _backButton.gameObject.SetActive(_stack.Count > 1);
            _title.transform.parent.gameObject.SetActive(_stack.Count > 1);
            _title.text = _stack.Peek().Label;
        }

        public void SetActivePanel(TabletPanel panel)
        {
            if (panel == _defaultPanel)
            {
                _stack.Clear();
            }

            if (_stack.Count > 0 && _stack.Peek() == panel)
            {
                return;
            }

            OnPanelActivate?.Invoke(panel.GetComponent<TabletPanel>());

            _stack.Push(panel);
            UpdatePanels();
        }
    }
}