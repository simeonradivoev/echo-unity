using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEcho.Mechanics
{
    public class EnvironmentController : MonoBehaviour
    {
        [SerializeField]
        private PlayerReferences _references;

        [SerializeField]
        private float _nonVacuumDrag = 0.1f;

        [SerializeField]
        private float _vacuumDrag;

        public float Atmosphere { get; private set; }

        private void Reset()
        {
            _references = GetComponent<PlayerReferences>();
        }

        private void FixedUpdate()
        {
            var stack = VolumeManager.instance.stack;
            var settings = stack.GetComponent<EnvironmentSettings>();
            Atmosphere = settings.atmosphere.value;
            _references.Body.drag = Mathf.Lerp(_vacuumDrag, _nonVacuumDrag, Atmosphere);
        }
    }
}