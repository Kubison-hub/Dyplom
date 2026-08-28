using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsScript : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private Slider mouseSensitivitySlider;

    [Header("Scene References")]
    [SerializeField] private GameMusicManager musicManager;
    [SerializeField] private Volume globalVolume;

    [Header("Image Adjustment Ranges")]
    [SerializeField, Min(0f)] private float postExposureRange = 5f;
    [SerializeField, Range(0f, 100f)] private float contrastRange = 100f;
    [SerializeField, Min(0.01f)] private float minimumMouseSensitivity = 0.25f;
    [SerializeField, Min(0.01f)] private float maximumMouseSensitivity = 2f;

    private readonly Dictionary<AudioSource, float> defaultSoundVolumes = new Dictionary<AudioSource, float>();
    private AudioSource musicSource;
    private ColorAdjustments colorAdjustments;
    private float musicMaximumVolume = 1f;
    private float defaultPostExposure;
    private float defaultContrast;

    private void Awake()
    {
        ResolveReferences();
        CacheSoundSources();
        ConfigureInitialSliderValues();
        RegisterSliderListeners();
    }

    private void OnDestroy()
    {
        UnregisterSliderListeners();
    }

    private void ResolveReferences()
    {
        if (musicManager == null)
            musicManager = FindFirstObjectByType<GameMusicManager>();

        if (musicManager != null)
            musicSource = musicManager.GetComponent<AudioSource>();

        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out colorAdjustments);
            if (colorAdjustments != null)
            {
                defaultPostExposure = colorAdjustments.postExposure.value;
                defaultContrast = colorAdjustments.contrast.value;
            }
        }
    }

    private void CacheSoundSources()
    {
        foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (source != null && source != musicSource && !defaultSoundVolumes.ContainsKey(source))
                defaultSoundVolumes.Add(source, source.volume);
        }
    }

    private void ConfigureInitialSliderValues()
    {
        if (musicSource != null)
        {
            musicMaximumVolume = Mathf.Max(0.01f, musicSource.volume);
            ConfigureSlider(musicSlider, 0f, musicMaximumVolume, musicMaximumVolume);
        }

        ConfigureSlider(soundSlider, 0f, 1f, 1f);
        ConfigureSlider(brightnessSlider, 0f, 1f, 0.5f);
        ConfigureSlider(contrastSlider, 0f, 1f, 0.5f);
        ConfigureSlider(mouseSensitivitySlider, minimumMouseSensitivity, maximumMouseSensitivity, 1f);

        ApplyBrightness(brightnessSlider != null ? brightnessSlider.value : 0.5f);
        ApplyContrast(contrastSlider != null ? contrastSlider.value : 0.5f);
    }

    private static void ConfigureSlider(Slider slider, float minValue, float maxValue, float value)
    {
        if (slider == null)
            return;

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.SetValueWithoutNotify(value);
    }

    private void RegisterSliderListeners()
    {
        musicSlider?.onValueChanged.AddListener(SetMusicVolume);
        soundSlider?.onValueChanged.AddListener(SetSoundVolume);
        brightnessSlider?.onValueChanged.AddListener(SetBrightness);
        contrastSlider?.onValueChanged.AddListener(SetContrast);
        mouseSensitivitySlider?.onValueChanged.AddListener(SetMouseSensitivity);
    }

    private void UnregisterSliderListeners()
    {
        musicSlider?.onValueChanged.RemoveListener(SetMusicVolume);
        soundSlider?.onValueChanged.RemoveListener(SetSoundVolume);
        brightnessSlider?.onValueChanged.RemoveListener(SetBrightness);
        contrastSlider?.onValueChanged.RemoveListener(SetContrast);
        mouseSensitivitySlider?.onValueChanged.RemoveListener(SetMouseSensitivity);
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = Mathf.Clamp(volume, 0f, musicMaximumVolume);
    }

    public void SetSoundVolume(float volume)
    {
        CacheSoundSources();
        float multiplier = Mathf.Clamp01(volume);

        foreach (KeyValuePair<AudioSource, float> entry in defaultSoundVolumes)
        {
            if (entry.Key != null)
                entry.Key.volume = entry.Value * multiplier;
        }
    }

    public void SetBrightness(float sliderValue)
    {
        ApplyBrightness(sliderValue);
    }

    public void SetContrast(float sliderValue)
    {
        ApplyContrast(sliderValue);
    }

    public void SetMouseSensitivity(float multiplier)
    {
        foreach (CameraController cameraController in FindObjectsByType<CameraController>(
                     FindObjectsInactive.Include,
                     FindObjectsSortMode.None))
        {
            cameraController?.SetMouseSensitivityMultiplier(multiplier);
        }
    }

    private void ApplyBrightness(float sliderValue)
    {
        if (colorAdjustments != null)
            colorAdjustments.postExposure.value = defaultPostExposure +
                                                  Mathf.Lerp(-postExposureRange, postExposureRange, Mathf.Clamp01(sliderValue));
    }

    private void ApplyContrast(float sliderValue)
    {
        if (colorAdjustments != null)
            colorAdjustments.contrast.value = defaultContrast +
                                               Mathf.Lerp(-contrastRange, contrastRange, Mathf.Clamp01(sliderValue));
    }
}
