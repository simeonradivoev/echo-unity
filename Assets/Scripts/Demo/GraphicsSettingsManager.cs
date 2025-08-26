using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.Management;
#if PLATFORM_ANDROID
using UnityEngine.XR.OpenXR.Features.Meta;
using Unity.Collections;
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

        public float RefreshRate { get; set; }

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
        }

        private void Start()
        {
            var defaultRefreshRate = _defaultRefreshRate;
            if (PlayerPrefs.HasKey(RefreshRateKey))
            {
                defaultRefreshRate = PlayerPrefs.GetFloat(RefreshRateKey);
            }

            if (XRGeneralSettings.Instance.Manager.activeLoader)
            {
                var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
#if PLATFORM_ANDROID
                if (displaySubsystem.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out var supportedRefreshRates))
                {
                    var closest = defaultRefreshRate;
                    var maxDiff = float.MaxValue;

                    for (var i = 0; i < supportedRefreshRates.Length; i++)
                    {
                        var diff = Mathf.Abs(defaultRefreshRate - supportedRefreshRates[i]);
                        if (diff < maxDiff)
                        {
                            maxDiff = diff;
                            closest = supportedRefreshRates[i];
                        }
                    }

                    if (!displaySubsystem.TryRequestDisplayRefreshRate(closest))
                    {
                        Application.targetFrameRate = Mathf.RoundToInt(closest);
                    }

                    RefreshRate = closest;
                }

#else
                Application.targetFrameRate = Mathf.RoundToInt(defaultRefreshRate);
#endif
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
#if PLATFORM_ANDROID
                if (displaySubsystem.TryRequestDisplayRefreshRate(value))
                {
                    PlayerPrefs.SetFloat(RefreshRateKey, value);
                }
                else
                {
                    Application.targetFrameRate = Mathf.RoundToInt(value);
                    PlayerPrefs.SetFloat(RefreshRateKey, value);
                }
#else
                    Application.targetFrameRate = Mathf.RoundToInt(value);
                    PlayerPrefs.SetFloat(RefreshRateKey, value);
#endif
                RefreshRate = value;
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