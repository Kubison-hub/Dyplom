using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shows a Vision Eye reconstruction of a closed passage until the real passage is opened.
/// </summary>
public class Int_lv3_SecretPassageVision : MonoBehaviour
{
    [Header("Doors")]
    [SerializeField] private GameObject realDoorGameObject;
    [SerializeField] private GameObject visionDoor;
    [SerializeField] private Animator visionDoorAnimator;
    [SerializeField] private string openTrigger = "Open";

    [Header("Vision Reconstruction")]
    [SerializeField] private GameObject ghostRoom;
    [SerializeField, Min(0.01f)] private float ghostRoomFadeDuration = 0.75f;
    [Tooltip("Blackboard hiding the reconstructed room outside Vision Eye.")]
    [SerializeField] private GameObject blackBoard;
    [SerializeField] private Material blackBoardFadeMaterial;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 0.5f;

    [Header("Real Passage State")]
    [SerializeField] private Int_lv3_ClockSecretPassage realPassage;

    private bool visionWasActive;
    private bool completed;
    private Coroutine blackBoardFadeCoroutine;
    private Coroutine ghostRoomFadeCoroutine;
    private readonly List<GhostMaterialTarget> ghostMaterialTargets = new List<GhostMaterialTarget>();
    private MaterialPropertyBlock ghostPropertyBlock;
    private float ghostRoomVisibility;
    private static readonly int GhostVisibilityProperty = Shader.PropertyToID("_Visibility");

    private struct GhostMaterialTarget
    {
        public Renderer renderer;
        public int materialIndex;
    }

    private void Awake()
    {
        ghostPropertyBlock = new MaterialPropertyBlock();
        CacheGhostMaterialTargets();

        if (visionDoorAnimator == null && visionDoor != null)
            visionDoorAnimator = visionDoor.GetComponent<Animator>();

        SetDoorObjects(false);
        SetGhostRoomVisibleImmediately(false);
        SetBlackBoardVisibleImmediately(true);
    }

    private void OnEnable()
    {
        visionWasActive = false;
        completed = false;
    }

    private void Update()
    {
        if (completed)
            return;

        if (realPassage != null && realPassage.IsOpened)
        {
            CompleteVision();
            return;
        }

        bool visionActive = EagleVisionSystem.Instance != null &&
                            EagleVisionSystem.Instance.isActive &&
                            IsSherlockActive();
        if (visionActive == visionWasActive)
            return;

        visionWasActive = visionActive;
        SetDoorObjects(visionActive);
        FadeGhostRoom(visionActive ? 1f : 0f);
        FadeBlackBoard(!visionActive);

        if (visionActive && visionDoorAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            visionDoorAnimator.SetTrigger(openTrigger);
    }

    private static bool IsSherlockActive()
    {
        return SwitchCharacter.Instance == null || SwitchCharacter.Instance.activePlayerIndex == 0;
    }

    private void SetDoorObjects(bool visionActive)
    {
        if (realDoorGameObject != null)
            realDoorGameObject.SetActive(!visionActive);

        if (visionDoor != null)
            visionDoor.SetActive(visionActive);
    }

    private void CompleteVision()
    {
        completed = true;
        GetComponent<Interactable>()?.MarkCompleted();

        if (realDoorGameObject != null)
            realDoorGameObject.SetActive(true);

        if (visionDoor != null)
            visionDoor.SetActive(false);

        StopGhostRoomFade();
        SetGhostRoomVisibleImmediately(false);

        gameObject.SetActive(false);
    }

    private void CacheGhostMaterialTargets()
    {
        ghostMaterialTargets.Clear();
        if (ghostRoom == null)
            return;

        foreach (Renderer targetRenderer in ghostRoom.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = targetRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty(GhostVisibilityProperty))
                    continue;

                ghostMaterialTargets.Add(new GhostMaterialTarget
                {
                    renderer = targetRenderer,
                    materialIndex = materialIndex
                });
            }
        }
    }

    private void FadeGhostRoom(float targetVisibility)
    {
        StopGhostRoomFade();

        if (ghostRoom == null)
            return;

        if (targetVisibility > 0f && !ghostRoom.activeSelf)
            ghostRoom.SetActive(true);

        ghostRoomFadeCoroutine = StartCoroutine(FadeGhostRoomRoutine(targetVisibility));
    }

    private IEnumerator FadeGhostRoomRoutine(float targetVisibility)
    {
        float startVisibility = ghostRoomVisibility;
        float elapsed = 0f;

        while (elapsed < ghostRoomFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / ghostRoomFadeDuration);
            SetGhostRoomVisibility(Mathf.SmoothStep(startVisibility, targetVisibility, progress));
            yield return null;
        }

        SetGhostRoomVisibility(targetVisibility);
        if (targetVisibility <= 0f && ghostRoom != null)
            ghostRoom.SetActive(false);

        ghostRoomFadeCoroutine = null;
    }

    private void StopGhostRoomFade()
    {
        if (ghostRoomFadeCoroutine == null)
            return;

        StopCoroutine(ghostRoomFadeCoroutine);
        ghostRoomFadeCoroutine = null;
    }

    private void SetGhostRoomVisibleImmediately(bool visible)
    {
        if (ghostRoom == null)
            return;

        ghostRoom.SetActive(true);
        SetGhostRoomVisibility(visible ? 1f : 0f);
        if (!visible)
            ghostRoom.SetActive(false);
    }

    private void SetGhostRoomVisibility(float visibility)
    {
        ghostRoomVisibility = Mathf.Clamp01(visibility);

        foreach (GhostMaterialTarget target in ghostMaterialTargets)
        {
            if (target.renderer == null)
                continue;

            ghostPropertyBlock.Clear();
            target.renderer.GetPropertyBlock(ghostPropertyBlock, target.materialIndex);
            ghostPropertyBlock.SetFloat(GhostVisibilityProperty, ghostRoomVisibility);
            target.renderer.SetPropertyBlock(ghostPropertyBlock, target.materialIndex);
        }
    }

    private void FadeBlackBoard(bool shouldBeVisible)
    {
        if (blackBoard == null)
            return;

        if (blackBoardFadeCoroutine != null)
            StopCoroutine(blackBoardFadeCoroutine);

        blackBoardFadeCoroutine = StartCoroutine(FadeBlackBoardRoutine(shouldBeVisible));
    }

    private IEnumerator FadeBlackBoardRoutine(bool shouldBeVisible)
    {
        if (shouldBeVisible && !blackBoard.activeSelf)
            blackBoard.SetActive(true);

        Renderer[] renderers = blackBoard.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            blackBoard.SetActive(shouldBeVisible);
            blackBoardFadeCoroutine = null;
            yield break;
        }

        if (blackBoardFadeMaterial != null)
        {
            foreach (Renderer targetRenderer in renderers)
                targetRenderer.material = blackBoardFadeMaterial;
        }

        float startAlpha = GetBlackBoardAlpha(renderers[0]);
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetBlackBoardAlpha(renderers, Mathf.Lerp(startAlpha, targetAlpha, elapsed / blackBoardFadeDuration));
            yield return null;
        }

        SetBlackBoardAlpha(renderers, targetAlpha);
        if (!shouldBeVisible)
            blackBoard.SetActive(false);

        blackBoardFadeCoroutine = null;
    }

    private void SetBlackBoardVisibleImmediately(bool visible)
    {
        if (blackBoard == null)
            return;

        blackBoard.SetActive(true);
        Renderer[] renderers = blackBoard.GetComponentsInChildren<Renderer>(true);

        if (blackBoardFadeMaterial != null)
        {
            foreach (Renderer targetRenderer in renderers)
                targetRenderer.material = blackBoardFadeMaterial;
        }

        SetBlackBoardAlpha(renderers, visible ? 1f : 0f);
        if (!visible)
            blackBoard.SetActive(false);
    }

    private static float GetBlackBoardAlpha(Renderer targetRenderer)
    {
        if (targetRenderer == null)
            return 1f;

        Material material = targetRenderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        return material.HasProperty(colorProperty) ? material.GetColor(colorProperty).a : 1f;
    }

    private static void SetBlackBoardAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            Material material = targetRenderer.material;
            string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
            if (!material.HasProperty(colorProperty))
                continue;

            Color color = material.GetColor(colorProperty);
            color.a = alpha;
            material.SetColor(colorProperty, color);
        }
    }
}
