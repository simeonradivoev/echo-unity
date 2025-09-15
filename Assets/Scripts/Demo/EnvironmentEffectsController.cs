using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.Audio;

namespace UnityEcho.Demo
{
    /// <summary>
    /// Handles various environmental variables, culling and sound.
    /// </summary>
    public class EnvironmentEffectsController : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _shipHum;

        [SerializeField]
        private AudioSource _windSource;

        [SerializeField]
        private AnimationCurve _windVelocityCurve;

        [SerializeField]
        private AudioMixer _mixer;

        private EnvironmentController _controller;

        private bool _inVacuum = true;

        private PlayerReferences _references;

        private float _shipHumDefaultVolume;

        private AudioMixerSnapshot _standardSnapshot;

        private AudioMixerSnapshot _vacuumSnapshot;

        private void Start()
        {
            _references = GetComponent<PlayerReferences>();
            _controller = GetComponent<EnvironmentController>();
            _shipHumDefaultVolume = _shipHum.volume;
            _vacuumSnapshot = _mixer.FindSnapshot("Vacuum");
            _standardSnapshot = _mixer.FindSnapshot("Snapshot");
        }

        private void Update()
        {
            _shipHum.volume = _shipHumDefaultVolume * _controller.Atmosphere;
            _windSource.volume = _windVelocityCurve.Evaluate(_references.Body.velocity.magnitude) * _controller.Atmosphere;
            _windSource.transform.position = _references.Head.position + _references.Body.velocity.normalized;
            var inVacuum = _controller.Atmosphere <= 0.5f;
            if (_inVacuum != inVacuum)
            {
                if (inVacuum)
                {
                    _vacuumSnapshot.TransitionTo(1);
                }
                else
                {
                    _standardSnapshot.TransitionTo(1);
                }

                _inVacuum = inVacuum;
            }
        }
    }
}