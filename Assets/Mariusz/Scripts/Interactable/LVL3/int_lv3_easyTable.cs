using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class int_lv3_easyTable : MonoBehaviour
{
    [Header("Table Figure Points")]
    [SerializeField] private Transform greenFigurePoint;
    [SerializeField] private GameObject greenFigurePrefab;
    [SerializeField] private Transform redFigurePoint;
    [SerializeField] private GameObject redFigurePrefab;
    [SerializeField] private Transform blueFigurePoint;
    [SerializeField] private GameObject blueFigurePrefab;

    [Header("Placed Figure Animation")]
    [SerializeField, Min(0.05f)] private float placedFigureAnimationDuration = 0.45f;
    [SerializeField] private float placedFigureLocalYRotation = 180f;

    [Header("Completion Sequence")]
    [SerializeField, Min(0f)] private float narrowCameraSettleDuration = 1f;
    [SerializeField, Min(1f)] private float completionFigureAnimationMultiplier = 3f;

    [Header("Watson Completion Movement")]
    [SerializeField] private NavMeshAgent watsonAgent;
    [SerializeField] private Transform watsonDestination;
    [SerializeField, Min(0.1f)] private float watsonDestinationSampleRadius = 1f;

    [Header("Completion Reveal")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private GameObject realRoom;
    [SerializeField] private Renderer[] backgroundBoxRenderers;
    [SerializeField, Min(0.01f)] private float backgroundBoxFadeDuration = 1f;
    [SerializeField] private GameObject ghostRoom;

    [Header("Quest Cleanup")]
    [SerializeField] private Interactable[] interactablesToDeactivateOnCompletion;

    [Header("Wall Door")]
    [FormerlySerializedAs("secretDoorPrefab")]
    [SerializeField] private GameObject secretDoorObject;
    [SerializeField] private Animator secretDoorAnimator;
    [SerializeField] private string openTriggerName = "Open";
    [SerializeField] private string openedBoolName = "Opened";

    [Header("Narration")]
    [SerializeField, TextArea] private string missingFiguresText = "Czegoś brakuje.";

    private Interactable interactable;
    private Collider interactionCollider;
    private bool greenFigurePlaced;
    private bool redFigurePlaced;
    private bool blueFigurePlaced;
    private bool completed;
    private Coroutine backgroundBoxFadeCoroutine;
    private readonly System.Collections.Generic.List<Transform> placedFigures =
        new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<BackgroundMaterialTarget> backgroundMaterialTargets =
        new System.Collections.Generic.List<BackgroundMaterialTarget>();
    private MaterialPropertyBlock backgroundPropertyBlock;
    public bool Opened { get; private set; }

    private struct BackgroundMaterialTarget
    {
        public Renderer renderer;
        public int materialIndex;
        public int colorPropertyId;
        public Color originalColor;
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();
        backgroundPropertyBlock = new MaterialPropertyBlock();
        CacheBackgroundMaterialTargets();
        interactable.SetInteractionType(InteractionType.int_lv3_easyTable);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
            return;

        bool placedAnyFigure = false;
        if (InventoryManager.Instance != null)
        {
            placedAnyFigure |= TryPlaceFigure(
                ItemType.Zielona,
                greenFigurePoint,
                greenFigurePrefab,
                ref greenFigurePlaced);

            placedAnyFigure |= TryPlaceFigure(
                ItemType.Czerwona,
                redFigurePoint,
                redFigurePrefab,
                ref redFigurePlaced);

            placedAnyFigure |= TryPlaceFigure(
                ItemType.Niebieska,
                blueFigurePoint,
                blueFigurePrefab,
                ref blueFigurePlaced);
        }

        if (!placedAnyFigure && !HasAnyUnplacedFigureInInventory())
        {
            if (PlayerTopText.Instance != null)
                PlayerTopText.Instance.ShowTopText(missingFiguresText, "");
        }

        if (AllFiguresPlaced() && !completed)
        {
            completed = true;
            StartCoroutine(CompleteInteractionSequence());
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private bool TryPlaceFigure(
        ItemType itemType,
        Transform point,
        GameObject figurePrefab,
        ref bool placed)
    {
        if (placed || InventoryManager.Instance == null || !InventoryManager.Instance.items.Contains(itemType))
            return false;

        if (point == null || figurePrefab == null)
        {
            Debug.LogWarning($"{name}: Missing point or prefab for {itemType} figure.", this);
            return false;
        }

        GameObject placedFigure = Instantiate(figurePrefab, point.position, point.rotation, point);
        DisablePlacedFigureInteraction(placedFigure);
        placedFigures.Add(placedFigure.transform);
        placed = true;
        return true;
    }

    private IEnumerator AnimatePlacedFigure(Transform placedFigure, float duration)
    {
        if (placedFigure == null)
            yield break;

        Quaternion startRotation = placedFigure.localRotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(0f, placedFigureLocalYRotation, 0f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            placedFigure.localRotation = Quaternion.SlerpUnclamped(startRotation, endRotation, easedProgress);
            yield return null;
        }

        if (placedFigure != null)
            placedFigure.localRotation = endRotation;
    }

    private static void DisablePlacedFigureInteraction(GameObject placedFigure)
    {
        if (placedFigure == null)
            return;

        foreach (Interactable figureInteractable in placedFigure.GetComponentsInChildren<Interactable>(true))
        {
            figureInteractable.isInteractableActive = false;
            figureInteractable.allowQuestionFXWhenInactive = false;
            figureInteractable.SetQuestionFXEagleVisionState(false);

            if (figureInteractable.interactiveShader != null)
                figureInteractable.interactiveShader.SetActive(false);

            figureInteractable.interactiveShader = null;
            figureInteractable.enabled = false;
        }

        foreach (Collider figureCollider in placedFigure.GetComponentsInChildren<Collider>(true))
            figureCollider.enabled = false;
    }

    private bool HasAnyUnplacedFigureInInventory()
    {
        if (InventoryManager.Instance == null)
            return false;

        return !greenFigurePlaced && InventoryManager.Instance.items.Contains(ItemType.Zielona) ||
               !redFigurePlaced && InventoryManager.Instance.items.Contains(ItemType.Czerwona) ||
               !blueFigurePlaced && InventoryManager.Instance.items.Contains(ItemType.Niebieska);
    }

    private bool AllFiguresPlaced()
    {
        return greenFigurePlaced && redFigurePlaced && blueFigurePlaced;
    }

    private IEnumerator CompleteInteractionSequence()
    {
        cameraController?.SetZoomState(CameraZoomState.Narrow);
        MoveWatsonToCompletionDestination();

        if (narrowCameraSettleDuration > 0f)
            yield return new WaitForSeconds(narrowCameraSettleDuration);

        float figureAnimationDuration = placedFigureAnimationDuration * completionFigureAnimationMultiplier;
        foreach (Transform placedFigure in placedFigures)
            StartCoroutine(AnimatePlacedFigure(placedFigure, figureAnimationDuration));

        if (figureAnimationDuration > 0f)
            yield return new WaitForSeconds(figureAnimationDuration);

        RevealCompletedRoom();
        OpenSecretDoor();
        cameraController?.SetZoomState(CameraZoomState.Medium);
        Debug.Log("Koniec interakcji: wszystkie figurki znajdują się na stole.");

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }

    private void MoveWatsonToCompletionDestination()
    {
        if (watsonAgent == null || watsonDestination == null)
            return;

        if (!watsonAgent.isOnNavMesh)
        {
            Debug.LogWarning($"{name}: Watson must be placed on the NavMesh before the easy table is solved.", this);
            return;
        }

        if (!NavMesh.SamplePosition(
                watsonDestination.position,
                out NavMeshHit navMeshHit,
                watsonDestinationSampleRadius,
                NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Watson Destination is not close enough to the NavMesh.", watsonDestination);
            return;
        }

        watsonAgent.isStopped = false;
        watsonAgent.SetDestination(navMeshHit.position);
    }

    private void RevealCompletedRoom()
    {
        if (realRoom != null)
            realRoom.SetActive(true);

        if (ghostRoom != null)
            ghostRoom.SetActive(false);

        DeactivateCompletedQuestInteractables();

        if (backgroundMaterialTargets.Count == 0)
            return;

        if (backgroundBoxFadeCoroutine != null)
            StopCoroutine(backgroundBoxFadeCoroutine);

        backgroundBoxFadeCoroutine = StartCoroutine(FadeBackgroundBox());
    }

    private IEnumerator FadeBackgroundBox()
    {
        float elapsed = 0f;

        while (elapsed < backgroundBoxFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / backgroundBoxFadeDuration);
            SetBackgroundBoxVisibility(1f - progress);
            yield return null;
        }

        SetBackgroundBoxVisibility(0f);
        backgroundBoxFadeCoroutine = null;
    }

    private void DeactivateCompletedQuestInteractables()
    {
        if (interactablesToDeactivateOnCompletion == null)
            return;

        foreach (Interactable oldQuestInteractable in interactablesToDeactivateOnCompletion)
        {
            if (oldQuestInteractable == null)
                continue;

            oldQuestInteractable.isInteractableActive = false;
            oldQuestInteractable.allowQuestionFXWhenInactive = false;
            oldQuestInteractable.SetQuestionFXEagleVisionState(false);

            if (oldQuestInteractable.interactiveShader != null)
                oldQuestInteractable.interactiveShader.SetActive(false);

            oldQuestInteractable.interactiveShader = null;

            foreach (Collider oldQuestCollider in oldQuestInteractable.GetComponentsInChildren<Collider>(true))
                oldQuestCollider.enabled = false;
        }
    }

    private void CacheBackgroundMaterialTargets()
    {
        backgroundMaterialTargets.Clear();

        if (backgroundBoxRenderers == null)
            return;

        foreach (Renderer backgroundRenderer in backgroundBoxRenderers)
        {
            if (backgroundRenderer == null)
                continue;

            Material[] materials = backgroundRenderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                int colorPropertyId = material.HasProperty("_BaseColor")
                    ? Shader.PropertyToID("_BaseColor")
                    : material.HasProperty("_Color") ? Shader.PropertyToID("_Color") : -1;

                if (colorPropertyId < 0)
                    continue;

                backgroundMaterialTargets.Add(new BackgroundMaterialTarget
                {
                    renderer = backgroundRenderer,
                    materialIndex = materialIndex,
                    colorPropertyId = colorPropertyId,
                    originalColor = material.GetColor(colorPropertyId)
                });
            }
        }
    }

    private void SetBackgroundBoxVisibility(float visibility)
    {
        if (backgroundPropertyBlock == null)
            return;

        foreach (BackgroundMaterialTarget target in backgroundMaterialTargets)
        {
            if (target.renderer == null)
                continue;

            target.renderer.GetPropertyBlock(backgroundPropertyBlock, target.materialIndex);
            Color color = target.originalColor;
            color.a *= Mathf.Clamp01(visibility);
            backgroundPropertyBlock.SetColor(target.colorPropertyId, color);
            target.renderer.SetPropertyBlock(backgroundPropertyBlock, target.materialIndex);
        }
    }

    public void OpenSecretDoor()
    {
        if (Opened)
            return;

        Opened = true;

        if (secretDoorObject != null && !secretDoorObject.activeSelf)
            secretDoorObject.SetActive(true);

        if (secretDoorAnimator == null && secretDoorObject != null)
            secretDoorAnimator = secretDoorObject.GetComponentInChildren<Animator>(true);

        if (secretDoorAnimator == null)
        {
            Debug.LogWarning($"{name}: Secret door animator is not assigned.", this);
            return;
        }

        secretDoorAnimator.SetBool(openedBoolName, true);
        secretDoorAnimator.SetTrigger(openTriggerName);
    }
}
