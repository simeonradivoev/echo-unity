using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using Tweens;
using UnityEcho.Objectives;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEcho.UI
{
    public class Tablet : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _defaultPanel;

        [SerializeField]
        private Button _backButton;

        [SerializeField]
        private TMP_Text _title;

        [SerializeField]
        private TMP_Text _objectivesText;

        [SerializeField]
        private Toggle[] _qualityToggles;

        [SerializeField]
        private RectTransform _objectivesPanel;

        [SerializeField]
        private GameObject _objectiveButtonNotification;

        private readonly StringBuilder _objectiveBuilder = new();

        private readonly Stack<RectTransform> _stack = new();

        private Canvas _canvas;

        private ObjectivesManager _objectivesManager;

        private RectTransform[] _panels;

        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            _objectivesManager = FindObjectOfType<ObjectivesManager>();
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveCompleted.AddListener(UpdateObjectives);
                _objectivesManager.OnObjectiveStarted.AddListener(OnObjectiveStarted);
            }
            _panels = GameObject.FindGameObjectsWithTag("TabletPanel")
                .Select(o => o.GetComponent<RectTransform>())
                .Where(o => o && o.IsChildOf(transform))
                .ToArray();
            SetActivePanel(_defaultPanel);
            _backButton.onClick.AddListener(Return);
            _qualityToggles[QualitySettings.GetQualityLevel()].isOn = true;
            for (var i = 0; i < _qualityToggles.Length; i++)
            {
                var qualityLevel = i;
                _qualityToggles[i]
                    .onValueChanged.AddListener(
                        v =>
                        {
                            if (v)
                            {
                                QualitySettings.SetQualityLevel(qualityLevel);
                            }
                        });
            }
            UpdateObjectives();
        }

        private void OnDestroy()
        {
            _backButton.onClick.RemoveListener(Return);
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveCompleted.RemoveListener(UpdateObjectives);
                _objectivesManager.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
            }
            foreach (var qualityToggle in _qualityToggles)
            {
                qualityToggle.onValueChanged.RemoveAllListeners();
            }
        }

        public void ExitApplication()
        {
            Application.Quit();
        }

        private void OnObjectiveStarted()
        {
            _objectiveButtonNotification.SetActive(true);
            UpdateObjectives();
        }

        private void UpdateObjectives()
        {
            _objectiveBuilder.Clear();

            if (!_objectivesManager || (_objectivesManager.CompletedObjectives.Count <= 0 && _objectivesManager.StartedObjectives.Count <= 0))
            {
                _objectiveBuilder.AppendLine("         - No Objectives -          ");
            }
            else
            {
                foreach (var startedObjective in _objectivesManager.StartedObjectives)
                {
                    _objectiveBuilder.AppendLine($"[ ] {startedObjective.Objective}");
                }

                if (_objectivesManager.CompletedObjectives.Count > 0)
                {
                    _objectiveBuilder.AppendLine("------------------------------------");

                    foreach (var completed in _objectivesManager.CompletedObjectives)
                    {
                        _objectiveBuilder.AppendLine($"[x] {completed.Objective}");
                    }
                }
            }

            _objectivesText.SetText(_objectiveBuilder);
        }

        private void Return()
        {
            _stack.Pop();
            UpdatePanels();
        }

        public void SpawnTestObject(GameObject prefab)
        {
            var camera = Camera.main;
            var ray = new Ray(camera.transform.position, camera.transform.forward);
            var hadHit = Physics.Raycast(ray, out var hit);
            var distance = 1f;
            if (hadHit)
            {
                distance = Mathf.Min(distance, hit.distance);
            }
            Instantiate(prefab, ray.GetPoint(distance), Quaternion.identity);
        }

        private void UpdatePanels()
        {
            _stack.Peek()
                .gameObject.AddTween(
                    new FloatTween
                    {
                        from = 0,
                        to = 1,
                        onUpdate = (c, v) => _stack.Peek().GetComponent<CanvasGroup>().alpha = v,
                        onStart = c => _stack.Peek().gameObject.SetActive(true),
                        onEnd = c => _stack.Peek().gameObject.SetActive(true)
                    });

            foreach (var otherPanel in _panels.Where(p => p != _stack.Peek()))
            {
                otherPanel.gameObject.SetActive(false);
            }

            _backButton.gameObject.SetActive(_stack.Count > 1);
            _title.transform.parent.gameObject.SetActive(_stack.Count > 1);
            _title.text = _stack.Peek().name;
        }

        public void SetActivePanel(RectTransform panel)
        {
            if (panel == _defaultPanel)
            {
                _stack.Clear();
            }

            if (_stack.Count > 0 && _stack.Peek() == panel)
            {
                return;
            }

            if (panel == _objectivesPanel)
            {
                _objectiveButtonNotification.SetActive(false);
            }

            _stack.Push(panel);
            UpdatePanels();
        }
    }
}