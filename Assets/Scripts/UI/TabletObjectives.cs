using System.Text;
using TMPro;
using UnityEcho.Objectives;
using UnityEngine;

namespace UnityEcho.UI
{
    public class TabletObjectives : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _objectiveText;

        private readonly StringBuilder _objectiveBuilder = new();

        private ObjectivesManager _objectivesManager;

        private void Start()
        {
            _objectivesManager = FindObjectOfType<ObjectivesManager>();
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveCompleted.AddListener(UpdateObjectives);
                _objectivesManager.OnObjectiveStarted.AddListener(OnObjectiveStarted);
            }

            UpdateObjectives();
        }

        private void OnDestroy()
        {
            if (_objectivesManager)
            {
                _objectivesManager.OnObjectiveCompleted.RemoveListener(UpdateObjectives);
                _objectivesManager.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
            }
        }

        private void OnObjectiveStarted()
        {
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

            _objectiveText.SetText(_objectiveBuilder);
        }
    }
}