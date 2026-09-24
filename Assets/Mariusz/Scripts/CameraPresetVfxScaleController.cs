using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public sealed class CameraPresetVfxScaleController : MonoBehaviour
{
    [System.Serializable]
    private struct PresetScale
    {
        [Min(0)] public int presetIndex;
        [Min(0.01f)] public float scale;
    }

    [Header("References")]
    [Tooltip("Assign both Sherlock and Watson CameraController components.")]
    [SerializeField] private CameraController[] cameraControllers;
    [SerializeField] private VisualEffect[] visualEffects;
    [Tooltip("Allows referenced ParticleFX objects to stay disabled in the scene while editing. They are enabled when Play Mode starts.")]
    [SerializeField] private bool enableVisualEffectObjectsOnAwake = true;

    [Header("VFX Property")]
    [SerializeField] private string scalePropertyName = "ParticleScaleMultiplier";
    [SerializeField, Min(0.01f)] private float baseScale = 1f;
    [SerializeField, Min(0f)] private float transitionDuration = 0.45f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private PresetScale[] presetScales =
    {
        new PresetScale { presetIndex = 0, scale = 0.5f },
        new PresetScale { presetIndex = 1, scale = 0.7f },
        new PresetScale { presetIndex = 2, scale = 0.9f },
        new PresetScale { presetIndex = 3, scale = 1f },
        new PresetScale { presetIndex = 4, scale = 1.2f }
    };

    private float lastAppliedScale = float.NaN;
    private float currentScale = 1f;
    private float transitionStartScale = 1f;
    private float transitionTargetScale = 1f;
    private float transitionElapsed;
    private int activePresetIndex = -1;
    private bool missingPropertyWarningLogged;

    private void Awake()
    {
        EnableVisualEffectObjects();
        ResolveReferences();
        ApplyScale(true);
    }

    private void OnEnable()
    {
        ResolveReferences();
        ApplyScale(true);
    }

    private void LateUpdate()
    {
        UpdateScaleForActivePreset();
        ApplyScale(false);
    }

    private void OnValidate()
    {
        baseScale = Mathf.Max(0.01f, baseScale);
        lastAppliedScale = float.NaN;
    }

    private void ResolveReferences()
    {
        if (cameraControllers == null || cameraControllers.Length == 0)
            cameraControllers = FindObjectsByType<CameraController>(FindObjectsSortMode.None);

        if (visualEffects == null || visualEffects.Length == 0)
            visualEffects = GetComponentsInChildren<VisualEffect>(true);
    }

    private void EnableVisualEffectObjects()
    {
        if (!enableVisualEffectObjectsOnAwake || visualEffects == null)
            return;

        foreach (VisualEffect visualEffect in visualEffects)
        {
            if (visualEffect == null)
                continue;

            visualEffect.gameObject.SetActive(true);
            visualEffect.enabled = true;
            visualEffect.Reinit();
            visualEffect.Play();
        }
    }

    private void ApplyScale(bool force)
    {
        CameraController cameraController = GetActiveCameraController();
        if (cameraController == null || visualEffects == null ||
            string.IsNullOrWhiteSpace(scalePropertyName))
            return;

        float scale = baseScale * currentScale;
        if (!force && Mathf.Approximately(scale, lastAppliedScale))
            return;

        bool propertyFound = false;
        foreach (VisualEffect visualEffect in visualEffects)
        {
            if (visualEffect == null || !visualEffect.HasFloat(scalePropertyName))
                continue;

            visualEffect.SetFloat(scalePropertyName, scale);
            propertyFound = true;
        }

        if (!propertyFound && !missingPropertyWarningLogged)
        {
            missingPropertyWarningLogged = true;
            Debug.LogWarning(
                $"{name}: Assigned Visual Effects do not expose a float property named " +
                $"'{scalePropertyName}'. Expose it in the VFX Graph and multiply particle scale by it.",
                this);
        }

        lastAppliedScale = scale;
    }

    private void UpdateScaleForActivePreset()
    {
        CameraController cameraController = GetActiveCameraController();
        if (cameraController == null)
            return;

        int presetIndex = cameraController.PresetScaleIndex;
        if (presetIndex != activePresetIndex)
        {
            activePresetIndex = presetIndex;
            transitionStartScale = currentScale;
            transitionTargetScale = GetScaleForPreset(presetIndex);
            transitionElapsed = 0f;
        }

        if (transitionDuration <= 0f)
        {
            currentScale = transitionTargetScale;
            return;
        }

        transitionElapsed = Mathf.Min(transitionElapsed + Time.unscaledDeltaTime, transitionDuration);
        float normalizedTime = transitionElapsed / transitionDuration;
        float easedTime = transitionCurve == null || transitionCurve.length == 0
            ? Mathf.SmoothStep(0f, 1f, normalizedTime)
            : Mathf.Clamp01(transitionCurve.Evaluate(normalizedTime));
        currentScale = Mathf.Lerp(transitionStartScale, transitionTargetScale, easedTime);
    }

    private float GetScaleForPreset(int presetIndex)
    {
        if (presetScales != null)
        {
            foreach (PresetScale presetScale in presetScales)
            {
                if (presetScale.presetIndex == presetIndex)
                    return Mathf.Max(0.01f, presetScale.scale);
            }
        }

        return 1f;
    }

    private CameraController GetActiveCameraController()
    {
        if (cameraControllers == null)
            return null;

        CameraController fallback = null;
        foreach (CameraController controller in cameraControllers)
        {
            if (controller == null)
                continue;

            fallback ??= controller;
            if (controller.IsCurrentGameplayCamera)
                return controller;
        }

        return fallback;
    }
}
