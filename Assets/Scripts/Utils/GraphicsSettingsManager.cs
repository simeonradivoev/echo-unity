using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Meta;
#if PLATFORM_ANDROID
#endif

namespace UnityEcho.Utils
{
    public class GraphicsSettingsManager : MonoBehaviour
    {
        public enum AntiAliasingType { None, MSAAx2, MSAAx4, MSSAx8, TAA }

        private const string QualityLevelKey = "Quality Level";

        private const string RefreshRateKey = "Refresh Rate";

        private const string AntiAliasingKey = "Anti Aliasing";

        [SerializeField]
        private float _defaultRefreshRate;

        private readonly HashSet<UniversalRenderPipelineAsset> _pipelineAssetInstances = new();

        private int[] _refreshRates;

        public AntiAliasingType AntiAliasing { get; private set; }

        private void Awake()
        {
            if (PlayerPrefs.HasKey(QualityLevelKey))
            {
                QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(QualityLevelKey));
            }

            if (PlayerPrefs.HasKey(AntiAliasingKey))
            {
                SetAntiAliasing((AntiAliasingType)PlayerPrefs.GetInt(AntiAliasingKey));
            }

            QualitySettings.activeQualityLevelChanged += QualitySettingsOnactiveQualityLevelChanged;

            if (XRGeneralSettings.Instance.Manager.activeLoader)
            {
                var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
                if (displaySubsystem.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out var supportedRefreshRates))
                {
                    var closest = _defaultRefreshRate;
                    var maxDiff = float.MaxValue;

                    for (var i = 0; i < supportedRefreshRates.Length; i++)
                    {
                        var diff = Mathf.Abs(_defaultRefreshRate - supportedRefreshRates[i]);
                        if (diff < maxDiff)
                        {
                            maxDiff = diff;
                            closest = supportedRefreshRates[i];
                        }
                    }

                    displaySubsystem.TryRequestDisplayRefreshRate(closest);
                }
            }
        }

        private void OnDestroy()
        {
            QualitySettings.activeQualityLevelChanged -= QualitySettingsOnactiveQualityLevelChanged;
        }

        public void SetRefreshRate(float value)
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader)
            {
                var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
                if (displaySubsystem.TryRequestDisplayRefreshRate(value))
                {
                    PlayerPrefs.SetFloat(RefreshRateKey, value);
                }
            }
        }

        public void SetQualityLevel(int level)
        {
            if (QualitySettings.GetQualityLevel() == level)
            {
                return;
            }

            QualitySettings.SetQualityLevel(level);
            PlayerPrefs.SetInt(QualityLevelKey, level);
        }

        public void SetAntiAliasing(AntiAliasingType level)
        {
            if (AntiAliasing == level)
            {
                return;
            }

            AntiAliasing = level;
            QualitySettingsOnactiveQualityLevelChanged(QualitySettings.GetQualityLevel(), QualitySettings.GetQualityLevel());
            PlayerPrefs.SetInt(AntiAliasingKey, (int)level);
        }

        private void QualitySettingsOnactiveQualityLevelChanged(int arg1, int current)
        {
            var urp = (UniversalRenderPipelineAsset)QualitySettings.renderPipeline;
            if (!_pipelineAssetInstances.Contains(urp))
            {
                urp = Instantiate(urp);
                _pipelineAssetInstances.Add(urp);
                QualitySettings.renderPipeline = urp;
            }

            urp.msaaSampleCount = AntiAliasing switch
            {
                AntiAliasingType.MSAAx2 => 2,
                AntiAliasingType.MSAAx4 => 4,
                AntiAliasingType.MSSAx8 => 8,
                _ => 1
            };

            var cameraData = Camera.main.GetComponent<UniversalAdditionalCameraData>();

            // High TAA
            cameraData.antialiasing = AntiAliasing == AntiAliasingType.TAA ? AntialiasingMode.TemporalAntiAliasing : AntialiasingMode.None;
        }
    }
}