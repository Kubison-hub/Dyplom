using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reveals a reconstructed secret passage only while Vision Eye is active.
/// Put this component on the HiddenPassage root, activated by the puzzle.
/// </summary>
public class HiddenPassageVisionReveal : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DetectiveSequencePuzzle requiredPuzzle;
    [SerializeField] private Renderer[] realWallRenderers;
    [SerializeField] private Collider[] realWallColliders;
    [SerializeField] private bool includeChildRenderers = true;

    [Tooltip("Optional roots whose renderers, including all children, receive the ghost alpha fade.")]
    [SerializeField] private Transform[] ghostFadeRoots;

    [Header("Ghost Wall Animation")]
    [SerializeField] private Animator ghostWallAnimator;
    [SerializeField] private string visionActiveParameter = "VisionActive";
    [SerializeField] private bool lockGhostWallAnimationAfterMaxPlays;
    [SerializeField, Min(1)] private int maxGhostWallAnimationPlays = 2;

    [Header("Ghost Room Fade")]
    [SerializeField, Min(0.01f)] private float ghostFadeDuration = 0.75f;

    [Header("Ghost Room Blackboard")]
    [Tooltip("Blackboard covering the reconstructed room outside EagleVision.")]
    [SerializeField] private GameObject ghostRoomBlackboard;
    [SerializeField] private Material ghostRoomBlackboardFadeMaterial;
    [SerializeField, Min(0.01f)] private float ghostRoomBlackboardFadeDuration = 0.5f;

    private Renderer[] visionRenderers;
    private bool[] realWallInitialStates;
    private bool[] realWallColliderInitialStates;
    private bool[] visionInitialStates;
    private bool visionWasActive;
    private int ghostWallAnimationPlayCount;
    private bool ghostWallAnimationLockedOpen;
    private const string GhostVisibilityProperty = "_Visibility";
    private readonly List<GhostRendererMaterialTarget> ghostMaterialTargets = new List<GhostRendererMaterialTarget>();
    private MaterialPropertyBlock propertyBlock;
    private Coroutine ghostFadeCoroutine;
    private Coroutine blackboardFadeCoroutine;
    private float ghostVisibility;

    private struct GhostRendererMaterialTarget
    {
        public Renderer renderer;
        public int materialIndex;
    }

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (realWallRenderers == null)
            realWallRenderers = new Renderer[0];

        visionRenderers = GetVisionRenderers();

        realWallInitialStates = GetRendererStates(realWallRenderers);
        realWallColliderInitialStates = GetColliderStates(realWallColliders);
        visionInitialStates = GetRendererStates(visionRenderers);
        CacheGhostMaterialTargets();

        SetGhostVisibility(0f);
        SetVisionRenderersVisible(false);
        SetGhostRoomBlackboardVisibleImmediately(true);
        RestoreRealWallRenderers();
        SetGhostWallAnimation(false);
    }

    private void Update()
    {
        if (requiredPuzzle == null || !requiredPuzzle.IsSolved)
        {
            HideReconstruction();
            return;
        }

        bool visionActive = EagleVisionSystem.Instance != null &&
                            EagleVisionSystem.Instance.isActive &&
                            IsSherlockActive();

        if (visionActive != visionWasActive)
        {
            visionWasActive = visionActive;

            if (visionActive)
            {
                HideRealWallRenderers();
                SetRealWallCollidersEnabled(false);
                SetGhostVisibility(0f);
                FadeGhostRoom(1f);
                FadeGhostRoomBlackboard(false);
            }
            else
            {
                RestoreRealWallRenderers();
                RestoreRealWallColliders();
                FadeGhostRoom(0f);
                FadeGhostRoomBlackboard(true);
            }

            SetGhostWallAnimation(visionActive);
        }
    }

    private void OnDisable()
    {
        HideReconstruction();
    }

    private static bool IsSherlockActive()
    {
        return SwitchCharacter.Instance == null || SwitchCharacter.Instance.activePlayerIndex == 0;
    }

    private void HideReconstruction()
    {
        visionWasActive = false;
        RestoreRealWallRenderers();
        RestoreRealWallColliders();
        StopGhostFade();
        SetGhostVisibility(0f);
        SetVisionRenderersVisible(false);
        StopGhostRoomBlackboardFade();
        SetGhostRoomBlackboardVisibleImmediately(true);
        SetGhostWallAnimation(false);
    }

    private void SetGhostWallAnimation(bool visionActive)
    {
        if (ghostWallAnimator == null || string.IsNullOrWhiteSpace(visionActiveParameter))
            return;

        // First play: solving the puzzle while in Eagle Vision. Second play: entering it again.
        // Afterwards the bool deliberately remains true, preserving the animation's final frame.
        if (lockGhostWallAnimationAfterMaxPlays && ghostWallAnimationLockedOpen)
        {
            ghostWallAnimator.SetBool(visionActiveParameter, true);
            return;
        }

        if (visionActive)
        {
            ghostWallAnimationPlayCount++;
            ghostWallAnimator.SetBool(visionActiveParameter, true);

            if (lockGhostWallAnimationAfterMaxPlays &&
                ghostWallAnimationPlayCount >= maxGhostWallAnimationPlays)
                ghostWallAnimationLockedOpen = true;

            return;
        }

        ghostWallAnimator.SetBool(visionActiveParameter, false);
    }

    private void HideRealWallRenderers()
    {
        for (int i = 0; i < realWallRenderers.Length; i++)
        {
            if (realWallRenderers[i] != null)
                realWallRenderers[i].enabled = false;
        }
    }

    private void RestoreRealWallRenderers()
    {
        for (int i = 0; i < realWallRenderers.Length; i++)
        {
            if (realWallRenderers[i] != null)
                realWallRenderers[i].enabled = GetInitialState(realWallInitialStates, i);
        }
    }

    private void SetRealWallCollidersEnabled(bool enabled)
    {
        if (realWallColliders == null)
            return;

        for (int i = 0; i < realWallColliders.Length; i++)
        {
            if (realWallColliders[i] != null)
                realWallColliders[i].enabled = enabled;
        }
    }

    private void RestoreRealWallColliders()
    {
        if (realWallColliders == null)
            return;

        for (int i = 0; i < realWallColliders.Length; i++)
        {
            if (realWallColliders[i] != null)
                realWallColliders[i].enabled = GetInitialState(realWallColliderInitialStates, i);
        }
    }

    private void SetVisionRenderersVisible(bool visible)
    {
        for (int i = 0; i < visionRenderers.Length; i++)
        {
            if (visionRenderers[i] != null)
                visionRenderers[i].enabled = visible && GetInitialState(visionInitialStates, i);
        }
    }

    private void CacheGhostMaterialTargets()
    {
        ghostMaterialTargets.Clear();

        for (int rendererIndex = 0; rendererIndex < visionRenderers.Length; rendererIndex++)
        {
            Renderer renderer = visionRenderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                if (!material.HasProperty(GhostVisibilityProperty))
                    continue;

                ghostMaterialTargets.Add(new GhostRendererMaterialTarget
                {
                    renderer = renderer,
                    materialIndex = materialIndex
                });
            }
        }
    }

    private Renderer[] GetVisionRenderers()
    {
        if (ghostFadeRoots == null || ghostFadeRoots.Length == 0)
        {
            Renderer[] renderers = includeChildRenderers
                ? GetComponentsInChildren<Renderer>(true)
                : GetComponents<Renderer>();
            return ExcludeGhostRoomBlackboardRenderers(renderers);
        }

        HashSet<Renderer> collectedRenderers = new HashSet<Renderer>();
        foreach (Transform fadeRoot in ghostFadeRoots)
        {
            if (fadeRoot == null)
                continue;

            foreach (Renderer renderer in fadeRoot.GetComponentsInChildren<Renderer>(true))
                collectedRenderers.Add(renderer);
        }

        Renderer[] result = new Renderer[collectedRenderers.Count];
        collectedRenderers.CopyTo(result);
        return ExcludeGhostRoomBlackboardRenderers(result);
    }

    private void FadeGhostRoom(float targetVisibility)
    {
        StopGhostFade();

        if (targetVisibility > 0f)
            SetVisionRenderersVisible(true);

        ghostFadeCoroutine = StartCoroutine(FadeGhostRoomRoutine(targetVisibility));
    }

    private IEnumerator FadeGhostRoomRoutine(float targetVisibility)
    {
        float startVisibility = ghostVisibility;
        float elapsed = 0f;

        while (elapsed < ghostFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ghostFadeDuration);
            SetGhostVisibility(Mathf.Lerp(startVisibility, targetVisibility, progress));
            yield return null;
        }

        SetGhostVisibility(targetVisibility);
        ghostFadeCoroutine = null;
    }

    private void StopGhostFade()
    {
        if (ghostFadeCoroutine == null)
            return;

        StopCoroutine(ghostFadeCoroutine);
        ghostFadeCoroutine = null;
    }

    private void SetGhostVisibility(float visibility)
    {
        ghostVisibility = Mathf.Clamp01(visibility);

        foreach (GhostRendererMaterialTarget target in ghostMaterialTargets)
        {
            if (target.renderer == null)
                continue;

            propertyBlock.Clear();
            propertyBlock.SetFloat(GhostVisibilityProperty, ghostVisibility);
            target.renderer.SetPropertyBlock(propertyBlock, target.materialIndex);
        }
    }

    private Renderer[] ExcludeGhostRoomBlackboardRenderers(Renderer[] renderers)
    {
        if (ghostRoomBlackboard == null || renderers == null)
            return renderers;

        List<Renderer> filtered = new List<Renderer>();
        Transform blackboardTransform = ghostRoomBlackboard.transform;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.transform == blackboardTransform ||
                renderer.transform.IsChildOf(blackboardTransform))
            {
                continue;
            }

            filtered.Add(renderer);
        }

        return filtered.ToArray();
    }

    private void FadeGhostRoomBlackboard(bool shouldBeVisible)
    {
        if (ghostRoomBlackboard == null)
            return;

        StopGhostRoomBlackboardFade();
        blackboardFadeCoroutine = StartCoroutine(FadeGhostRoomBlackboardRoutine(shouldBeVisible));
    }

    private IEnumerator FadeGhostRoomBlackboardRoutine(bool shouldBeVisible)
    {
        bool wasActive = ghostRoomBlackboard.activeSelf;
        if (shouldBeVisible && !wasActive)
            ghostRoomBlackboard.SetActive(true);

        Renderer[] renderers = ghostRoomBlackboard.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            ghostRoomBlackboard.SetActive(shouldBeVisible);
            blackboardFadeCoroutine = null;
            yield break;
        }

        if (ghostRoomBlackboardFadeMaterial != null)
        {
            foreach (Renderer renderer in renderers)
                renderer.material = ghostRoomBlackboardFadeMaterial;
        }

        float startAlpha = shouldBeVisible && !wasActive ? 0f : GetBlackboardAlpha(renderers[0]);
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        SetBlackboardAlpha(renderers, startAlpha);

        float elapsed = 0f;
        while (elapsed < ghostRoomBlackboardFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBlackboardAlpha(renderers, Mathf.Lerp(startAlpha, targetAlpha, elapsed / ghostRoomBlackboardFadeDuration));
            yield return null;
        }

        SetBlackboardAlpha(renderers, targetAlpha);
        if (!shouldBeVisible)
            ghostRoomBlackboard.SetActive(false);

        blackboardFadeCoroutine = null;
    }

    private void StopGhostRoomBlackboardFade()
    {
        if (blackboardFadeCoroutine == null)
            return;

        StopCoroutine(blackboardFadeCoroutine);
        blackboardFadeCoroutine = null;
    }

    private void SetGhostRoomBlackboardVisibleImmediately(bool visible)
    {
        if (ghostRoomBlackboard == null)
            return;

        ghostRoomBlackboard.SetActive(true);
        Renderer[] renderers = ghostRoomBlackboard.GetComponentsInChildren<Renderer>(true);
        if (ghostRoomBlackboardFadeMaterial != null)
        {
            foreach (Renderer renderer in renderers)
                renderer.material = ghostRoomBlackboardFadeMaterial;
        }

        SetBlackboardAlpha(renderers, visible ? 1f : 0f);
        if (!visible)
            ghostRoomBlackboard.SetActive(false);
    }

    private static float GetBlackboardAlpha(Renderer renderer)
    {
        if (renderer == null)
            return 1f;

        Material material = renderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        return material.HasProperty(colorProperty) ? material.GetColor(colorProperty).a : 1f;
    }

    private static void SetBlackboardAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material material = renderer.material;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (!material.HasProperty(colorProperty))
                continue;

            Color color = material.GetColor(colorProperty);
            color.a = alpha;
            material.SetColor(colorProperty, color);
        }
    }

    private static bool[] GetRendererStates(Renderer[] renderers)
    {
        if (renderers == null)
            return new bool[0];

        bool[] states = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            states[i] = renderers[i] != null && renderers[i].enabled;

        return states;
    }

    private static bool[] GetColliderStates(Collider[] colliders)
    {
        if (colliders == null)
            return new bool[0];

        bool[] states = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            states[i] = colliders[i] != null && colliders[i].enabled;

        return states;
    }

    private static bool GetInitialState(bool[] states, int index)
    {
        return states != null && index >= 0 && index < states.Length && states[index];
    }
}
