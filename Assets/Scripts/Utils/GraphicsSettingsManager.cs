using UnityEngine;
using UnityEngine.Rendering.Universal;
#if PLATFORM_ANDROID
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Meta;
#endif

namespace UnityEcho.Utils
{
    public class GraphicsSettingsManager : MonoBehaviour
    {
        [SerializeField]
        private int[] _refreshRates;

        private void Awake()
        {
            QualitySettings.activeQualityLevelChanged += QualitySettingsOnactiveQualityLevelChanged;
        }

        private void Start()
        {
            QualitySettingsOnactiveQualityLevelChanged(0, QualitySettings.GetQualityLevel());
        }

        private void OnDestroy()
        {
            QualitySettings.activeQualityLevelChanged -= QualitySettingsOnactiveQualityLevelChanged;
        }

        private void QualitySettingsOnactiveQualityLevelChanged(int arg1, int current)
        {
#if PLATFORM_ANDROID
            var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            displaySubsystem.TryRequestDisplayRefreshRate(_refreshRates[Mathf.Clamp(current, 0, _refreshRates.Length - 1)]);
#else
            Application.targetFrameRate = _refreshRates[Mathf.Clamp(current, 0, _refreshRates.Length - 1)];
#endif

            var cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

            // High TAA
            cameraData.antialiasing = current == 2 ? AntialiasingMode.TemporalAntiAliasing : AntialiasingMode.None;
        }
    }
}