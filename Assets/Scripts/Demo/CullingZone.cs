using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEcho.Demo
{
    public class CullingZone : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _rootObjects;

        private readonly List<Canvas> _canvases = new();

        private readonly List<Light> _lights = new();

        private readonly List<Renderer> _renderers = new();

        private bool _culled;

        private bool _finishedInitialization;

        public bool Culled
        {
            get => _culled;
            set
            {
                if (_culled != value)
                {
                    _culled = value;
                    if (_finishedInitialization)
                    {
                        UpdateCulled();
                    }
                }
            }
        }

        private IEnumerator Start()
        {
            foreach (var rootObject in _rootObjects)
            {
                _renderers.AddRange(rootObject.GetComponentsInChildren<Renderer>());
                yield return null;
                _canvases.AddRange(rootObject.GetComponentsInChildren<Canvas>());
                yield return null;
                _lights.AddRange(rootObject.GetComponentsInChildren<Light>());
                yield return null;
            }

            _finishedInitialization = true;
            UpdateCulled();
        }

        private void UpdateCulled()
        {
            foreach (var renderer in _renderers)
            {
                renderer.enabled = !_culled;
            }

            foreach (var canvas in _canvases)
            {
                canvas.enabled = !_culled;
            }

            foreach (var light in _lights)
            {
                light.enabled = !_culled;
            }
        }
    }
}