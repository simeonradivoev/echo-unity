using UnityEngine;

namespace UnityEcho.Objectives
{
    [CreateAssetMenu]
    public class ObjectiveDefinition : ScriptableObject
    {
        [SerializeField]
        private string _objective;

        public string Objective => _objective;
    }
}