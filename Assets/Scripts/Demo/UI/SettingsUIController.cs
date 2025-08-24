using Demo.UI;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEcho.Mechanics;
using UnityEcho.Utils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace UnityEcho.UI
{
    public class SettingsUIController : MonoBehaviour
    {
        [SerializeField]
        private Toggle _realisticControls;

        [SerializeField]
        private Toggle _spectatorCamera;

        [SerializeField]
        private QualityToggle _qualityTogglePrefab;

        [SerializeField]
        private Transform _qualityTogglesParent;

        [SerializeField]
        private ToggleGroup _qualityToggleGroup;

        [SerializeField]
        private ToggleGroup _refreshRatesToggleGroup;

        [SerializeField]
        private ToggleGroup _antiAliasingGroup;

        [SerializeField]
        private TMP_Text _fpsCounter;

        private readonly List<QualityToggle> _qualityToggles = new();

        private GraphicsSettingsManager _settings;

        private Toggle _taa;

        private void Start()
        {
            var player = FindObjectOfType<PlayerReferences>();
            _settings = FindObjectOfType<GraphicsSettingsManager>();
            _realisticControls.isOn = player.GetComponent<RealisticMovementController>().IsRealisticMovement;
            _realisticControls.onValueChanged.AddListener(v => player.GetComponent<RealisticMovementController>().SetRealisticMovement(v));
            _spectatorCamera.isOn = player.GetComponent<HeadFollower>().SpectatorCameraActive;
            _spectatorCamera.onValueChanged.AddListener(v => player.GetComponent<HeadFollower>().SetSpeculatorCamera(v));
            QualitySettings.activeQualityLevelChanged += QualitySettingsOnactiveQualityLevelChanged;
            var currentQualityLevel = QualitySettings.GetQualityLevel();

            QualitySettings.ForEach(
                (index, name) =>
                {
                    var quality = Instantiate(_qualityTogglePrefab, _qualityTogglesParent);
                    quality.Label.text = name;
                    quality.Toggle.isOn = index == currentQualityLevel;
                    quality.Toggle.group = _qualityToggleGroup;
                    quality.Toggle.onValueChanged.AddListener(
                        v =>
                        {
                            if (v)
                            {
                                _settings.SetQualityLevel(index);
                            }
                        });
                    _qualityToggles.Add(quality);
                });

            if (XRGeneralSettings.Instance.Manager.activeLoader)
            {
                var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
                displaySubsystem.foveatedRenderingLevel = 2;

                displaySubsystem.TryGetSupportedDisplayRefreshRates(Allocator.Temp, out var refreshRates);
                for (var i = 0; i < refreshRates.Length; i++)
                {
                    var refreshRateToggle = Instantiate(_qualityTogglePrefab, _refreshRatesToggleGroup.transform);
                    var refreshRateValue = refreshRates[i];
                    refreshRateToggle.Label.text = refreshRates[i].ToString("F");
                    if (displaySubsystem.TryGetDisplayRefreshRate(out var refreshRate))
                    {
                        refreshRateToggle.Toggle.isOn = refreshRateValue == refreshRate;
                    }
                    refreshRateToggle.Toggle.group = _refreshRatesToggleGroup;
                    refreshRateToggle.Toggle.onValueChanged.AddListener(
                        v =>
                        {
                            if (v)
                            {
                                _settings.SetRefreshRate(refreshRateValue);
                            }
                        });
                }
            }

            foreach (var value in Enum.GetValues(typeof(GraphicsSettingsManager.AntiAliasingType)))
            {
                var aa = (GraphicsSettingsManager.AntiAliasingType)value;
                var toggle = Instantiate(_qualityTogglePrefab, _antiAliasingGroup.transform);
                toggle.Label.text = value.ToString();
                toggle.Toggle.isOn = _settings.AntiAliasing == aa;
                toggle.Toggle.group = _antiAliasingGroup;
                toggle.Toggle.onValueChanged.AddListener(
                    v =>
                    {
                        if (v)
                        {
                            _settings.SetAntiAliasing(aa);
                        }
                    });
                if (aa == GraphicsSettingsManager.AntiAliasingType.TAA)
                {
                    _taa = toggle.Toggle;
                }
            }

            QualitySettingsOnactiveQualityLevelChanged(QualitySettings.GetQualityLevel(), QualitySettings.GetQualityLevel());
        }

        private void Update()
        {
            var currentFrame = (int)Math.Round(1f / Time.smoothDeltaTime);
            _fpsCounter.text = currentFrame.ToString("D");
        }

        private void QualitySettingsOnactiveQualityLevelChanged(int arg1, int arg2)
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader)
            {
                var displaySubsystem = XRGeneralSettings.Instance.Manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
                if ((SystemInfo.hdrDisplaySupportFlags & HDRDisplaySupportFlags.Supported) != 0 && displaySubsystem.hdrOutputSettings.available)
                {
                    displaySubsystem.hdrOutputSettings.automaticHDRTonemapping = true;
                }
            }
        }
    }
}