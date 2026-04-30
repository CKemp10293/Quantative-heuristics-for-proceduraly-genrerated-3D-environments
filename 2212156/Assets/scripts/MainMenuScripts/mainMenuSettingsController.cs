using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class mainMenuSettingsController : MonoBehaviour
{
    [Header("UI References")]
    public Slider volumeSlider;
    public TMP_Text volumeValueText;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Optional Actions")]
    [Tooltip("Optional button if you want a manual apply flow")]
    public Button applyButton;

    private const string VolumePrefKey = "settings.volume";
    private const string FullscreenPrefKey = "settings.fullscreen";
    private const string ResolutionWidthPrefKey = "settings.resolution.width";
    private const string ResolutionHeightPrefKey = "settings.resolution.height";

    private readonly List<Resolution> uniqueResolutions = new List<Resolution>();
    private bool isInitializing;

    private void Start()
    {
        BuildResolutionOptions();
        LoadAndApplySavedSettings();
        HookUiEvents();
    }

    public void OnSettingsOpened()
    {
        // Keep the dropdown in sync in case display modes changed.
        BuildResolutionOptions();
        RefreshUiFromCurrentSettings();
    }

    public void OnSettingsClosed()
    {
        // Intentionally no-op for now.
    }

    public void OnVolumeChanged(float value)
    {
        if (isInitializing) return;

        ApplyVolume(value);
        SaveVolume(value);
        UpdateVolumeLabel(value);
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        if (isInitializing) return;

        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenPrefKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void OnResolutionChanged(int index)
    {
        if (isInitializing) return;
        if (index < 0 || index >= uniqueResolutions.Count) return;

        Resolution selectedResolution = uniqueResolutions[index];
        Screen.SetResolution(selectedResolution.width, selectedResolution.height, Screen.fullScreen);

        PlayerPrefs.SetInt(ResolutionWidthPrefKey, selectedResolution.width);
        PlayerPrefs.SetInt(ResolutionHeightPrefKey, selectedResolution.height);
        PlayerPrefs.Save();
    }

    public void ApplyCurrentUiValues()
    {
        if (volumeSlider != null) OnVolumeChanged(volumeSlider.value);
        if (fullscreenToggle != null) OnFullscreenToggled(fullscreenToggle.isOn);
        if (resolutionDropdown != null) OnResolutionChanged(resolutionDropdown.value);
    }

    private void BuildResolutionOptions()
    {
        if (resolutionDropdown == null) return;

        Resolution[] allResolutions = Screen.resolutions;
        uniqueResolutions.Clear();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            Resolution candidate = allResolutions[i];
            bool alreadyAdded = false;

            for (int j = 0; j < uniqueResolutions.Count; j++)
            {
                if (uniqueResolutions[j].width == candidate.width && uniqueResolutions[j].height == candidate.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                uniqueResolutions.Add(candidate);
            }
        }

        resolutionDropdown.ClearOptions();
        List<string> labels = new List<string>(uniqueResolutions.Count);
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            labels.Add(uniqueResolutions[i].width + " x " + uniqueResolutions[i].height);
        }
        resolutionDropdown.AddOptions(labels);
    }

    private void LoadAndApplySavedSettings()
    {
        isInitializing = true;

        float volume = PlayerPrefs.GetFloat(VolumePrefKey, 0.7f);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthPrefKey, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightPrefKey, Screen.currentResolution.height);

        ApplyVolume(volume);
        Screen.fullScreen = isFullscreen;

        int resolutionIndex = GetResolutionIndex(savedWidth, savedHeight);
        if (resolutionIndex >= 0)
        {
            Resolution res = uniqueResolutions[resolutionIndex];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        }

        RefreshUiFromCurrentSettings();
        isInitializing = false;
    }

    private void RefreshUiFromCurrentSettings()
    {
        isInitializing = true;

        float volume = PlayerPrefs.GetFloat(VolumePrefKey, AudioListener.volume);
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, Screen.fullScreen ? 1 : 0) == 1;
        int savedWidth = PlayerPrefs.GetInt(ResolutionWidthPrefKey, Screen.currentResolution.width);
        int savedHeight = PlayerPrefs.GetInt(ResolutionHeightPrefKey, Screen.currentResolution.height);

        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(volume);
        UpdateVolumeLabel(volume);

        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);

        if (resolutionDropdown != null)
        {
            int resolutionIndex = GetResolutionIndex(savedWidth, savedHeight);
            if (resolutionIndex < 0) resolutionIndex = GetResolutionIndex(Screen.currentResolution.width, Screen.currentResolution.height);
            if (resolutionIndex < 0) resolutionIndex = 0;

            resolutionDropdown.SetValueWithoutNotify(resolutionIndex);
            resolutionDropdown.RefreshShownValue();
        }

        isInitializing = false;
    }

    private void HookUiEvents()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenToggled);
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }

        if (applyButton != null)
        {
            applyButton.onClick.RemoveListener(ApplyCurrentUiValues);
            applyButton.onClick.AddListener(ApplyCurrentUiValues);
        }
    }

    private int GetResolutionIndex(int width, int height)
    {
        for (int i = 0; i < uniqueResolutions.Count; i++)
        {
            if (uniqueResolutions[i].width == width && uniqueResolutions[i].height == height)
            {
                return i;
            }
        }

        return -1;
    }

    private void ApplyVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private void SaveVolume(float value)
    {
        PlayerPrefs.SetFloat(VolumePrefKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
    }

    private void UpdateVolumeLabel(float value)
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }
    }
}

