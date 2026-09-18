using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
public sealed class SkinnedMeshOverlayBinder : MonoBehaviour
{
    [Header("Animated Model Overlay")]
    [Tooltip("Główny, animowany model postaci. Jego kości sterują nakładką.")]
    [SerializeField] private Transform sourceModelRoot;
    [Tooltip("Kopia modelu używana jako Interactive Shader.")]
    [SerializeField] private Transform overlayModelRoot;
    [Tooltip("Wyłącza Animatory znajdujące się w shaderowej kopii, aby nie sterowały własnym szkieletem.")]
    [SerializeField] private bool disableOverlayAnimators = true;
    [Tooltip("Włącza Update When Offscreen na rendererach nakładki, aby efekt nie znikał przy błędnych boundsach.")]
    [SerializeField] private bool updateOverlayWhenOffscreen = true;
    [Tooltip("Wypisuje ostrzeżenie, jeśli nie uda się dopasować któregoś renderera nakładki.")]
    [SerializeField] private bool logBindingWarnings = true;

    private void Awake()
    {
        BindOverlayToSourceSkeleton();
    }

    [ContextMenu("Bind Overlay To Source Skeleton")]
    public void BindOverlayToSourceSkeleton()
    {
        if (sourceModelRoot == null || overlayModelRoot == null)
        {
            if (logBindingWarnings)
                Debug.LogWarning("SkinnedMeshOverlayBinder: przypisz Source Model Root oraz Overlay Model Root.", this);
            return;
        }

        if (disableOverlayAnimators)
        {
            foreach (Animator overlayAnimator in overlayModelRoot.GetComponentsInChildren<Animator>(true))
                overlayAnimator.enabled = false;
        }

        SkinnedMeshRenderer[] sourceRenderers =
            sourceModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        SkinnedMeshRenderer[] overlayRenderers =
            overlayModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        HashSet<SkinnedMeshRenderer> usedSources = new HashSet<SkinnedMeshRenderer>();
        int boundCount = 0;

        foreach (SkinnedMeshRenderer overlayRenderer in overlayRenderers)
        {
            SkinnedMeshRenderer sourceRenderer = FindMatchingSource(
                overlayRenderer,
                sourceRenderers,
                usedSources);

            if (sourceRenderer == null)
            {
                if (logBindingWarnings)
                {
                    Debug.LogWarning(
                        "SkinnedMeshOverlayBinder: nie znaleziono głównego renderera dla nakładki " +
                        overlayRenderer.name + ".",
                        overlayRenderer);
                }

                continue;
            }

            overlayRenderer.bones = sourceRenderer.bones;
            overlayRenderer.rootBone = sourceRenderer.rootBone;
            overlayRenderer.updateWhenOffscreen = updateOverlayWhenOffscreen;
            overlayRenderer.localBounds = sourceRenderer.localBounds;

            usedSources.Add(sourceRenderer);
            boundCount++;
        }

        if (logBindingWarnings && overlayRenderers.Length > 0 && boundCount == 0)
        {
            Debug.LogWarning(
                "SkinnedMeshOverlayBinder: nie udało się przepiąć żadnego SkinnedMeshRenderer. " +
                "Modele mogą używać różnych siatek lub układów kości.",
                this);
        }
    }

    private static SkinnedMeshRenderer FindMatchingSource(
        SkinnedMeshRenderer overlayRenderer,
        SkinnedMeshRenderer[] sourceRenderers,
        HashSet<SkinnedMeshRenderer> usedSources)
    {
        SkinnedMeshRenderer match = FindSource(
            overlayRenderer,
            sourceRenderers,
            usedSources,
            source => source.sharedMesh == overlayRenderer.sharedMesh);

        if (match != null)
            return match;

        match = FindSource(
            overlayRenderer,
            sourceRenderers,
            usedSources,
            source => source.name == overlayRenderer.name &&
                      source.bones.Length == overlayRenderer.bones.Length);

        if (match != null)
            return match;

        return FindSource(
            overlayRenderer,
            sourceRenderers,
            usedSources,
            source => source.bones.Length == overlayRenderer.bones.Length);
    }

    private static SkinnedMeshRenderer FindSource(
        SkinnedMeshRenderer overlayRenderer,
        SkinnedMeshRenderer[] sourceRenderers,
        HashSet<SkinnedMeshRenderer> usedSources,
        System.Predicate<SkinnedMeshRenderer> predicate)
    {
        foreach (SkinnedMeshRenderer sourceRenderer in sourceRenderers)
        {
            if (sourceRenderer == null ||
                sourceRenderer == overlayRenderer ||
                usedSources.Contains(sourceRenderer) ||
                !predicate(sourceRenderer))
            {
                continue;
            }

            return sourceRenderer;
        }

        return null;
    }
}
