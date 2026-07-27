using UnityEngine;

[DisallowMultipleComponent]
public sealed class InteractionShaderFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.25f;
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
    [SerializeField, Min(0f)] private float pulseSpeed = 1.5f;
    [SerializeField] private string visibilityProperty = "_Visibility";
    [SerializeField] private string pulseSpeedProperty = "_PulseSpeed";

    private struct RendererMaterialTarget
    {
        public Renderer renderer;
        public int materialIndex;
    }

    private RendererMaterialTarget[] materialTargets;
    private MaterialPropertyBlock propertyBlock;
    private float currentAlpha;
    private float targetAlpha;
    private bool hideAfterFade;

    private void Awake()
    {
        CacheRenderers();
        SetAlphaImmediate(0f);
    }

    private void OnEnable()
    {
        CacheRenderers();
        SetAlphaImmediate(0f);
        targetAlpha = 0f;
        hideAfterFade = false;
    }

    private void Update()
    {
        if (Mathf.Approximately(currentAlpha, targetAlpha))
        {
            if (hideAfterFade && targetAlpha <= 0f)
                gameObject.SetActive(false);

            return;
        }

        float duration = targetAlpha > currentAlpha ? fadeInDuration : fadeOutDuration;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.unscaledDeltaTime / duration);
        ApplyAlpha(currentAlpha);
    }

    public void FadeIn()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        hideAfterFade = false;
        targetAlpha = visibleAlpha;
    }

    public void FadeOut()
    {
        if (!gameObject.activeSelf)
            return;

        targetAlpha = 0f;
        hideAfterFade = true;
    }

    public void HideImmediately()
    {
        targetAlpha = 0f;
        hideAfterFade = false;
        SetAlphaImmediate(0f);
        gameObject.SetActive(false);
    }

    public void Configure(float newVisibleAlpha, float newPulseSpeed)
    {
        visibleAlpha = Mathf.Clamp01(newVisibleAlpha);
        pulseSpeed = Mathf.Max(0f, newPulseSpeed);

        if (gameObject.activeSelf)
            ApplyAlpha(currentAlpha);
    }

    private void CacheRenderers()
    {
        if (materialTargets != null && materialTargets.Length > 0)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
        int targetCount = 0;

        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.HasProperty(visibilityProperty))
                    targetCount++;
            }
        }

        materialTargets = new RendererMaterialTarget[targetCount];
        int targetIndex = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty(visibilityProperty))
                    continue;

                materialTargets[targetIndex++] = new RendererMaterialTarget
                {
                    renderer = renderer,
                    materialIndex = materialIndex
                };
            }
        }
    }

    private void SetAlphaImmediate(float alpha)
    {
        currentAlpha = alpha;
        ApplyAlpha(alpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (materialTargets == null)
            return;

        for (int i = 0; i < materialTargets.Length; i++)
        {
            RendererMaterialTarget target = materialTargets[i];
            Renderer renderer = target.renderer;
            if (renderer == null)
                continue;

            propertyBlock.Clear();
            propertyBlock.SetFloat(visibilityProperty, alpha);

            Material material = renderer.sharedMaterials[target.materialIndex];
            if (material != null && material.HasProperty(pulseSpeedProperty))
                propertyBlock.SetFloat(pulseSpeedProperty, pulseSpeed);

            renderer.SetPropertyBlock(propertyBlock, target.materialIndex);
        }
    }
}
