using UnityEcho.Mechanics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEcho.Demo
{
    public class CullingZoneManager : MonoBehaviour
    {
        [SerializeField]
        private CullingZone _exterior;

        [SerializeField]
        private CullingZone _interior;

        private EnvironmentSettings _enviornment;

        private Camera _mainCamera;

        private VolumeStack _stack;

        private void Start()
        {
            _stack = VolumeManager.instance.CreateStack();
            _mainCamera = Camera.main;
            _mainCamera.UpdateVolumeStack();
            _enviornment = _stack.GetComponent<EnvironmentSettings>();
        }

        private void Update()
        {
            var cameraData = _mainCamera.GetUniversalAdditionalCameraData();
            var layerMask = cameraData.volumeLayerMask;
            var trigger = cameraData.volumeTrigger != null ? cameraData.volumeTrigger : _mainCamera.transform;
            VolumeManager.instance.Update(_stack, trigger, layerMask);

            var interiorVisible = _enviornment.atmosphere.value > 0f;
            _interior.Culled = !interiorVisible;
            _exterior.Culled = interiorVisible;
        }

        private void OnDestroy()
        {
            _stack.Dispose();
        }
    }
}