using System.Collections;
using UnityEngine;

public sealed class WatsonExaminationVisionEffect : MonoBehaviour
{
    [Header("Effect Origin")]
    [SerializeField] private Transform effectOrigin;

    [Header("Ring")]
    [SerializeField] private LineRenderer ring;
    [SerializeField] private Material ringMaterial;
    [SerializeField] private Color ringColor = new Color(0.25f, 1f, 0.45f, 0.8f);
    [SerializeField, Min(0.1f)] private float maximumRadius = 4f;
    [SerializeField, Min(0.01f)] private float expansionDuration = 0.75f;
    [SerializeField] private AnimationCurve expansionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0.001f)] private float ringWidth = 0.04f;
    [SerializeField, Range(16, 128)] private int ringSegments = 64;
    [SerializeField] private float ringHeight = 0.03f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.35f;

    [Header("Color Mask - Inside")]
    [SerializeField, Range(-100f, 100f)] private float insideSaturation = -35f;
    [SerializeField, Range(-2f, 2f)] private float insideBrightness;
    [SerializeField, Range(-100f, 100f)] private float insideContrast;
    [SerializeField, ColorUsage(true, true)] private Color insideTint = Color.white;

    [Header("Color Mask - Outside")]
    [SerializeField, Range(-100f, 100f)] private float outsideSaturation = -100f;
    [SerializeField, Range(-2f, 2f)] private float outsideBrightness;
    [SerializeField, Range(-100f, 100f)] private float outsideContrast;
    [SerializeField, ColorUsage(true, true)] private Color outsideTint = Color.white;

    [Header("Color Mask - Boundary")]
    [SerializeField, Min(0.001f)] private float boundarySoftness = 0.2f;
    [SerializeField, ColorUsage(true, true)] private Color boundaryTint = new Color(1f, 0.38f, 0.04f, 0.12f);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip enterClip;
    [SerializeField] private AudioClip exitClip;

    private Coroutine effectCoroutine;
    private Material runtimeMaterial;
    private float currentRadius;
    private float currentStrength;
    private bool effectActive;

    public void BeginEffect()
    {
        EnsureRing();
        StopRunningCoroutine();
        effectActive = true;
        currentRadius = 0f;
        currentStrength = 1f;
        SetRingVisible(true);
        PlayClip(enterClip);
        effectCoroutine = StartCoroutine(ExpandRing());
    }

    public void EndEffect()
    {
        if (!effectActive && currentStrength <= 0f)
            return;

        StopRunningCoroutine();
        effectCoroutine = StartCoroutine(FadeOut());
    }

    private void LateUpdate()
    {
        if (!effectActive && currentStrength <= 0f)
            return;

        Vector3 center = GetEffectCenter();
        UpdateRing(center, currentRadius, currentStrength);
        ApplyColorMask(center, currentRadius, currentStrength);
    }

    private IEnumerator ExpandRing()
    {
        float elapsed = 0f;
        while (elapsed < expansionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / expansionDuration);
            float easedProgress = expansionCurve != null && expansionCurve.length > 0
                ? Mathf.Clamp01(expansionCurve.Evaluate(progress))
                : progress;
            currentRadius = maximumRadius * easedProgress;
            yield return null;
        }

        currentRadius = maximumRadius;
        effectCoroutine = null;
    }

    private IEnumerator FadeOut()
    {
        PlayClip(exitClip);
        float startStrength = currentStrength;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentStrength = Mathf.Lerp(startStrength, 0f, Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }

        currentStrength = 0f;
        effectActive = false;
        effectCoroutine = null;
        SetRingVisible(false);
        ClearColorMask();
    }

    private void EnsureRing()
    {
        if (ring == null)
        {
            GameObject ringObject = new GameObject("Watson Examination Vision Ring");
            ringObject.transform.SetParent(transform, false);
            ring = ringObject.AddComponent<LineRenderer>();
        }

        int sherlockLayer = LayerMask.NameToLayer("Sherlock");
        if (sherlockLayer >= 0)
            ring.gameObject.layer = sherlockLayer;

        ring.useWorldSpace = true;
        ring.loop = true;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.receiveShadows = false;
        ring.textureMode = LineTextureMode.Stretch;
        ring.alignment = LineAlignment.View;

        if (ringMaterial != null)
        {
            ring.material = ringMaterial;
        }
        else if (ring.sharedMaterial == null)
        {
            Material resource = Resources.Load<Material>("EagleVisionScanRing");
            if (resource != null)
            {
                ring.material = resource;
            }
            else
            {
                Shader shader = Shader.Find("Mariusz/Eagle Vision Scan Ring");
                if (shader != null)
                {
                    runtimeMaterial = new Material(shader)
                    {
                        name = "Watson Examination Vision Ring (Runtime)"
                    };
                    ring.material = runtimeMaterial;
                }
            }
        }

        SetRingVisible(false);
    }

    private void UpdateRing(Vector3 center, float radius, float strength)
    {
        if (ring == null)
            return;

        int segmentCount = Mathf.Max(16, ringSegments);
        center.y += ringHeight;
        ring.enabled = true;
        ring.positionCount = segmentCount;
        ring.startWidth = ringWidth;
        ring.endWidth = ringWidth;

        Color visibleColor = ringColor;
        visibleColor.a *= Mathf.Clamp01(strength);
        ring.startColor = visibleColor;
        ring.endColor = visibleColor;

        for (int index = 0; index < segmentCount; index++)
        {
            float angle = index * Mathf.PI * 2f / segmentCount;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            ring.SetPosition(index, point);
        }
    }

    private void ApplyColorMask(Vector3 center, float radius, float strength)
    {
        Shader.SetGlobalVector("_EagleVisionScanCenter", center);
        Shader.SetGlobalFloat("_EagleVisionScanRadius", radius);
        Shader.SetGlobalFloat("_EagleVisionColorMaskEnabled", 1f);
        Shader.SetGlobalFloat("_EagleVisionColorMaskStrength", Mathf.Clamp01(strength));
        Shader.SetGlobalFloat("_EagleVisionInsideSaturation", insideSaturation);
        Shader.SetGlobalFloat("_EagleVisionInsideBrightness", insideBrightness);
        Shader.SetGlobalFloat("_EagleVisionInsideContrast", insideContrast);
        Shader.SetGlobalColor("_EagleVisionInsideTint", insideTint);
        Shader.SetGlobalFloat("_EagleVisionOutsideSaturation", outsideSaturation);
        Shader.SetGlobalFloat("_EagleVisionOutsideBrightness", outsideBrightness);
        Shader.SetGlobalFloat("_EagleVisionOutsideContrast", outsideContrast);
        Shader.SetGlobalColor("_EagleVisionOutsideTint", outsideTint);
        Shader.SetGlobalFloat("_EagleVisionBoundarySoftness", boundarySoftness);
        Shader.SetGlobalColor("_EagleVisionBoundaryTint", boundaryTint);

        if (EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.eagleVisionVolume != null)
            EagleVisionSystem.Instance.eagleVisionVolume.weight = Mathf.Clamp01(strength);
    }

    private static void ClearColorMask()
    {
        Shader.SetGlobalFloat("_EagleVisionColorMaskStrength", 0f);
        Shader.SetGlobalFloat("_EagleVisionColorMaskEnabled", 0f);
    }

    private Vector3 GetEffectCenter()
    {
        return effectOrigin != null ? effectOrigin.position : transform.position;
    }

    private void SetRingVisible(bool visible)
    {
        if (ring == null)
            return;

        ring.enabled = visible;
        if (!visible)
            ring.positionCount = 0;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    private void StopRunningCoroutine()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = null;
    }

    private void OnDisable()
    {
        StopRunningCoroutine();
        effectActive = false;
        currentStrength = 0f;
        SetRingVisible(false);
        ClearColorMask();
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }
}
