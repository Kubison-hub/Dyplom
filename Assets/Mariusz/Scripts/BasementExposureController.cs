using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Temporarily raises the global exposure while the group is in the basement,
/// then restores the exact value that was active before entering it.
/// </summary>
public class BasementExposureController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume globalVolume;

    [Header("Basement Exposure")]
    [SerializeField] private float basementPostExposureBoost = 0.6f;
    [SerializeField, Min(0.05f)] private float fadeDuration = 1.25f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Vision Eye Exposure")]
    [SerializeField] private Volume visionEyeVolume;
    [SerializeField] private float visionEyeNormalPostExposure = 2f;
    [SerializeField] private float visionEyeBasementPostExposure = 6f;

    private ColorAdjustments colorAdjustments;
    private ColorAdjustments visionEyeColorAdjustments;
    private Coroutine fadeCoroutine;
    private bool hasSavedExposure;
    private float savedPostExposure;

    private void Start()
    {
        StartCoroutine(InitializeVisionEyeExposure());
    }

    public void EnterBasement()
    {
        if (!TryResolveColorAdjustments())
            return;

        if (!hasSavedExposure)
        {
            savedPostExposure = colorAdjustments.postExposure.value;
            hasSavedExposure = true;
        }

        StartFade(savedPostExposure + basementPostExposureBoost,
            TryResolveVisionEyeColorAdjustments(), visionEyeBasementPostExposure);
    }

    public IEnumerator RestoreBeforeLeavingBasement()
    {
        if (!hasSavedExposure || !TryResolveColorAdjustments())
            yield break;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        yield return FadeTo(savedPostExposure,
            TryResolveVisionEyeColorAdjustments(), visionEyeNormalPostExposure);
        hasSavedExposure = false;
    }

    private void StartFade(float targetExposure, bool fadeVisionEye, float visionEyeTargetExposure)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeTo(targetExposure, fadeVisionEye, visionEyeTargetExposure));
    }

    private IEnumerator FadeTo(float targetExposure, bool fadeVisionEye, float visionEyeTargetExposure)
    {
        float startExposure = colorAdjustments.postExposure.value;
        float visionEyeStartExposure = fadeVisionEye ? visionEyeColorAdjustments.postExposure.value : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / fadeDuration);
            float curvedProgress = fadeCurve.Evaluate(progress);
            colorAdjustments.postExposure.value = Mathf.Lerp(startExposure, targetExposure, curvedProgress);

            if (fadeVisionEye)
            {
                visionEyeColorAdjustments.postExposure.value = Mathf.Lerp(
                    visionEyeStartExposure,
                    visionEyeTargetExposure,
                    curvedProgress);
            }
            yield return null;
        }

        colorAdjustments.postExposure.value = targetExposure;
        if (fadeVisionEye)
            visionEyeColorAdjustments.postExposure.value = visionEyeTargetExposure;

        fadeCoroutine = null;
    }

    private IEnumerator InitializeVisionEyeExposure()
    {
        yield return null;

        if (TryResolveVisionEyeColorAdjustments())
            visionEyeColorAdjustments.postExposure.value = visionEyeNormalPostExposure;
    }

    private bool TryResolveColorAdjustments()
    {
        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume == null || globalVolume.profile == null)
        {
            Debug.LogWarning($"{name}: no global Volume profile is available for basement exposure.", this);
            return false;
        }

        if (colorAdjustments == null && !globalVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.LogWarning($"{name}: the global Volume has no Color Adjustments override.", globalVolume);
            return false;
        }

        return colorAdjustments != null;
    }

    private bool TryResolveVisionEyeColorAdjustments()
    {
        if (visionEyeVolume == null && EagleVisionSystem.Instance != null)
            visionEyeVolume = EagleVisionSystem.Instance.eagleVisionVolume;

        if (visionEyeVolume == null || visionEyeVolume.profile == null)
            return false;

        return visionEyeColorAdjustments != null ||
               visionEyeVolume.profile.TryGet(out visionEyeColorAdjustments);
    }
}
