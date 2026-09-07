using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class int_lv3_easyTable : Lvl3InteractionDialogueBase
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

    [Header("Completion Character Rotation")]
    [SerializeField] private Transform sherlockLookTarget;
    [SerializeField] private Transform watsonLookTarget;
    [SerializeField, Min(1f)] private float completionRotationSpeed = 300f;
    [SerializeField, Min(0.01f)] private float completionRotationTolerance = 0.5f;

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
    [SerializeField] private AudioSource wallDoorAudioSource;
    [SerializeField] private AudioClip wallDoorOpenAudio;

    [Header("Reparent Before Door Opens")]
    [Tooltip("Optional object moved under New Parent immediately before the door opens.")]
    [SerializeField] private Transform objectToReparentBeforeDoorOpens;
    [SerializeField] private Transform newParentBeforeDoorOpens;
    [SerializeField] private bool keepWorldPositionWhenReparenting = true;

    [Header("Table Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] firstInteractionDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ten stół jest częścią mechanizmu. Brakuje kilku elementów.",
            duration = 3f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] missingFiguresDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Czegoś brakuje.",
            duration = 2f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] figurePlacedDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Jedna z figurek znalazła swoje miejsce.",
            duration = 2f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] completionDialogue;

    [Header("Notebook Notes")]
    [SerializeField] private int prePuzzleNoteIndex = 0;
    [SerializeField] private int completedPuzzleNoteIndex = 1;
    [SerializeField] private int completedPuzzleAdditionalNoteIndex = 2;

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
    private bool prePuzzleNoteAdded;
    private bool firstInteractionPerformed;
    public bool Opened { get; private set; }

    protected override Lvl3DialogueLine[] DefaultDialogueLines => missingFiguresDialogue;

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
        interactable.addDatabaseNotesAutomatically = false;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
            return;

        AddPrePuzzleNotebookNote();

        if (!firstInteractionPerformed)
        {
            firstInteractionPerformed = true;
            CluesLog.Instance?.StartTableMechanismObjective();
            PlayDialogue(player, firstInteractionDialogue);

            if (player != null)
                player.currentInteractable = null;

            return;
        }

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

        if (placedAnyFigure)
        {
            if (AllFiguresPlaced())
            {
                completed = true;
                GetComponent<Interactable>()?.MarkCompleted();
                CluesLog.Instance?.CompleteTableMechanismObjective();
                StartCoroutine(PlayPlacementDialogueThenComplete(player));
            }
            else
            {
                PlayDialogue(player, figurePlacedDialogue);
            }
        }
        else if (!HasAnyUnplacedFigureInInventory())
        {
            PlayDialogue(player, missingFiguresDialogue);
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private IEnumerator PlayPlacementDialogueThenComplete(PlayerController player)
    {
        if (figurePlacedDialogue != null && figurePlacedDialogue.Length > 0)
        {
            PlayDialogue(player, figurePlacedDialogue);
            while (IsDialoguePlaying)
                yield return null;
        }

        yield return CompleteInteractionSequence();
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

        if (!InventoryManager.Instance.TryRemoveItem(itemType))
            return false;

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
        ReparentObjectBeforeDoorOpens();
        OpenSecretDoor();
        CluesLog.Instance?.CompleteSecretDoorOpeningPuzzle();
        PlayDialogue(null, completionDialogue);
        AddCompletedPuzzleNotebookNote();
        StartCoroutine(RotateCharactersAfterDoorOpens());
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

    private void AddPrePuzzleNotebookNote()
    {
        if (prePuzzleNoteAdded || prePuzzleNoteIndex < 0 || interactable == null)
            return;

        prePuzzleNoteAdded = true;
        interactable.AddNote(prePuzzleNoteIndex);
    }

    private void ReparentObjectBeforeDoorOpens()
    {
        if (objectToReparentBeforeDoorOpens == null || newParentBeforeDoorOpens == null)
            return;

        objectToReparentBeforeDoorOpens.SetParent(
            newParentBeforeDoorOpens,
            keepWorldPositionWhenReparenting);
    }

    private void AddCompletedPuzzleNotebookNote()
    {
        if (interactable == null)
            return;

        if (completedPuzzleNoteIndex >= 0)
            interactable.AddNote(completedPuzzleNoteIndex);

        if (completedPuzzleAdditionalNoteIndex >= 0)
            interactable.AddNote(completedPuzzleAdditionalNoteIndex);
    }

    private void MoveWatsonToCompletionDestination()
    {
        if (watsonAgent == null || watsonDestination == null)
            return;

        // The interaction focus keeps Watson looking at the table and disables
        // agent rotation. The completion walk must be controlled by NavMesh only.
        WatsonCompanionController.Instance?.ClearInteractionFocus();

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

        watsonAgent.ResetPath();
        watsonAgent.isStopped = false;
        watsonAgent.updateRotation = true;
        watsonAgent.SetDestination(navMeshHit.position);
    }

    private IEnumerator RotateCharactersAfterDoorOpens()
    {
        yield return WaitForWatsonCompletionMovement();

        Transform sherlock = GetPlayerTransform(0);
        Transform watson = watsonAgent != null ? watsonAgent.transform : GetPlayerTransform(1);
        Quaternion sherlockTargetRotation = GetRotationTowardsTarget(sherlock, sherlockLookTarget);
        Quaternion watsonTargetRotation = GetRotationTowardsTarget(watson, watsonLookTarget);

        bool rotateSherlock = sherlock != null && sherlockLookTarget != null;
        bool rotateWatson = watson != null && watsonLookTarget != null;

        while (rotateSherlock || rotateWatson)
        {
            if (rotateSherlock)
            {
                sherlock.rotation = Quaternion.RotateTowards(
                    sherlock.rotation,
                    sherlockTargetRotation,
                    completionRotationSpeed * Time.deltaTime);
                rotateSherlock = Quaternion.Angle(sherlock.rotation, sherlockTargetRotation) > completionRotationTolerance;
            }

            if (rotateWatson)
            {
                watson.rotation = Quaternion.RotateTowards(
                    watson.rotation,
                    watsonTargetRotation,
                    completionRotationSpeed * Time.deltaTime);
                rotateWatson = Quaternion.Angle(watson.rotation, watsonTargetRotation) > completionRotationTolerance;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForWatsonCompletionMovement()
    {
        if (watsonAgent == null || watsonDestination == null || !watsonAgent.isOnNavMesh)
            yield break;

        while (watsonAgent.pathPending)
            yield return null;

        if (watsonAgent.pathStatus != NavMeshPathStatus.PathComplete)
            yield break;

        while (watsonAgent.remainingDistance > watsonAgent.stoppingDistance + completionRotationTolerance)
            yield return null;

        watsonAgent.ResetPath();
        watsonAgent.isStopped = true;
    }

    private static Transform GetPlayerTransform(int playerIndex)
    {
        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.players == null ||
            playerIndex < 0 || playerIndex >= SwitchCharacter.Instance.players.Length)
        {
            return null;
        }

        return SwitchCharacter.Instance.players[playerIndex] != null
            ? SwitchCharacter.Instance.players[playerIndex].transform
            : null;
    }

    private static Quaternion GetRotationTowardsTarget(Transform character, Transform lookTarget)
    {
        if (character == null || lookTarget == null)
            return Quaternion.identity;

        Vector3 direction = lookTarget.position - character.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : character.rotation;
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

        if (wallDoorAudioSource != null && wallDoorOpenAudio != null)
            wallDoorAudioSource.PlayOneShot(wallDoorOpenAudio);

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
