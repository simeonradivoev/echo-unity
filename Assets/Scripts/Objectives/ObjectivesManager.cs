using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UnityEcho.Objectives
{
    public class ObjectivesManager : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent _onObjectiveStarted;

        [SerializeField]
        private UnityEvent _onObjectiveCompleted;

        public UnityEvent OnObjectiveStarted => _onObjectiveStarted;

        public UnityEvent OnObjectiveCompleted => _onObjectiveCompleted;

        public HashSet<ObjectiveDefinition> StartedObjectives { get; } = new();

        public HashSet<ObjectiveDefinition> CompletedObjectives { get; } = new();

        public void CompleteObjective(ObjectiveDefinition objectiveDefinition)
        {
            if (CompletedObjectives.Add(objectiveDefinition))
            {
                StartedObjectives.Remove(objectiveDefinition);
                _onObjectiveCompleted.Invoke();
            }
        }

        public void StartObjective(ObjectiveDefinition objectiveDefinition)
        {
            if (CompletedObjectives.Contains(objectiveDefinition))
            {
                return;
            }

            if (StartedObjectives.Add(objectiveDefinition))
            {
                _onObjectiveStarted.Invoke();
            }
        }
    }
}