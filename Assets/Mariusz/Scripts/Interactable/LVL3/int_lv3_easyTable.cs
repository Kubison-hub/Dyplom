using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class int_lv3_easyTable : Lvl3InteractionDialogueBase
{
    [Header("Table Figure")]
    [SerializeField] private ItemType requiredItem = ItemType.Zielona;
    [FormerlySerializedAs("greenFigurePrefab")]
    [FormerlySerializedAs("greenFigureVisual")]
    [SerializeField] private GameObject figureVisual;

    [Header("Placed Figure Animation")]
    [SerializeField, Min(0.05f)] private float placedFigureAnimationDuration = 0.45f;
    [SerializeField] private float placedFigureLocalYRotation = 180f;

    [Header("Completion Sequence")]
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
    [SerializeField] private GameObject realRoom;
    [SerializeField] private Renderer[] backgroundBoxRenderers;
    [SerializeField, Min(0.01f)] private float backgroundBoxFadeDuration = 1f;
    [SerializeField] private GameObject ghostRoom;

    [Header("Quest Cleanup")]
    [SerializeField] private Interactable[] interactablesToDeactivateOnCompletion;
    [SerializeField] private WatsonEscortNPC watsonEscortNpcToDisable;

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
    [SerializeField] private Lvl3DialogueLine[] loupeHoverDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Hmm, w stole wyraźnie brakuje elementu mechanizmu.",
            duration = 3f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] firstInteractionDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ten stół jest częścią mechanizmu. Brakuje elementu.",
            duration = 3f
        }
    };
    [FormerlySerializedAs("missingFiguresDialogue")]
    [SerializeField] private Lvl3DialogueLine[] interactionAfterHoverDialogue =
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

    [Header("Loupe Hover")]
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField] private Collider loupeHoverCollider;
    [SerializeField, Min(0f)] private float loupeHoverDuration = 1.5f;
    [SerializeField] private SherlockWatsonHintConditions sherlockWatsonHintConditions;

    private Interactable interactable;
    private Collider interactionCollider;
    private bool figurePlaced;
    private bool completed;
    private Coroutine backgroundBoxFadeCoroutine;
    private readonly System.Collections.Generic.List<Transform> placedFigures =
        new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<BackgroundMaterialTarget> backgroundMaterialTargets =
        new System.Collections.Generic.List<BackgroundMaterialTarget>();
    private MaterialPropertyBlock backgroundPropertyBlock;
    private bool prePuzzleNoteAdded;
    private bool firstInteractionPerformed;
    private bool loupeHoverDiscovered;
    private float loupeHoverStartedAt = -1f;
    public bool Opened { get; private set; }

    protected override Lvl3DialogueLine[] DefaultDialogueLines => firstInteractionDialogue;

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
        if (loupeHoverCollider == null)
            loupeHoverCollider = interactionCollider;

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (sherlockWatsonHintConditions == null)
        {
            sherlockWatsonHintConditions = FindFirstObjectByType<SherlockWatsonHintConditions>(
                FindObjectsInactive.Include);
        }

        backgroundPropertyBlock = new MaterialPropertyBlock();
        CacheBackgroundMaterialTargets();
        interactable.SetInteractionType(InteractionType.int_lv3_easyTable);
        interactable.addDatabaseNotesAutomatically = false;
    }

    private void Update()
    {
        if (loupeHoverDiscovered || completed || magnifier == null || loupeHoverCollider == null)
            return;

        if (!magnifier.TryGetActiveLoupeHit(out RaycastHit hit) || !IsLoupeHoverHit(hit.collider))
        {
            loupeHoverStartedAt = -1f;
            return;
        }

        if (loupeHoverStartedAt < 0f)
            loupeHoverStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoverStartedAt < loupeHoverDuration)
            return;

        loupeHoverDiscovered = true;
        sherlockWatsonHintConditions?.MarkTableHover();

        if (loupeHoverDialogue != null && loupeHoverDialogue.Length > 0)
            PlayDialogue(null, loupeHoverDialogue);
    }

    private bool IsLoupeHoverHit(Collider hitCollider)
    {
        if (hitCollider == null || loupeHoverCollider == null)
            return false;

        return hitCollider == loupeHoverCollider ||
               hitCollider.transform.IsChildOf(loupeHoverCollider.transform) ||
               loupeHoverCollider.transform.IsChildOf(hitCollider.transform);
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
            PlayDialogue(player, GetInspectionClickDialogue());

            if (player != null)
                player.currentInteractable = null;

            return;
        }

        bool placedAnyFigure = false;
        if (InventoryManager.Instance != null)
            placedAnyFigure = TryPlaceRequiredFigure();

        if (placedAnyFigure)
        {
            if (IsRequiredFigurePlaced())
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
            PlayDialogue(player, GetInspectionClickDialogue());
        }

        if (player != null)
            player.currentInteractable = null;
    }

    private Lvl3DialogueLine[] GetInspectionClickDialogue()
    {
        return loupeHoverDiscovered ? interactionAfterHoverDialogue : firstInteractionDialogue;
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
        GameObject figureVisual,
        ref bool placed)
    {
        if (placed || InventoryManager.Instance == null || !InventoryManager.Instance.items.Contains(itemType))
            return false;

        if (figureVisual == null)
        {
            Debug.LogWarning($"{name}: Missing table visual for {itemType} figure.", this);
            return false;
        }

        if (!InventoryManager.Instance.TryRemoveItem(itemType))
            return false;

        figureVisual.SetActive(true);
        DisablePlacedFigureInteraction(figureVisual);
        placedFigures.Add(figureVisual.transform);
        placed = true;
        return true;
    }

    private bool TryPlaceRequiredFigure()
    {
        return TryPlaceFigure(requiredItem, figureVisual, ref figurePlaced);
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

        return !figurePlaced && InventoryManager.Instance.items.Contains(requiredItem);
    }

    private bool IsRequiredFigurePlaced()
    {
        return figurePlaced;
    }

    private IEnumerator CompleteInteractionSequence()
    {
        MoveWatsonToCompletionDestination();

        float figureAnimationDuration = placedFigureAnimationDuration * completionFigureAnimationMultiplier;
        foreach (Transform placedFigure in placedFigures)
            StartCoroutine(AnimatePlacedFigure(placedFigure, figureAnimationDuration));

        if (figureAnimationDuration > 0f)
            yield return new WaitForSeconds(figureAnimationDuration);

        RevealCompletedRoom();
        ReparentObjectBeforeDoorOpens();
        OpenSecretDoor();
        DisableWatsonEscortNpc();
        CluesLog.Instance?.CompleteSecretDoorOpeningPuzzle();
        sherlockWatsonHintConditions?.MarkEasyTableComplete();
        PlayDialogue(null, completionDialogue);
        AddCompletedPuzzleNotebookNote();
        StartCoroutine(RotateCharactersAfterDoorOpens());
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
        {
            ReleaseWatsonAfterCompletionMovement();
            yield break;
        }

        while (watsonAgent.remainingDistance > watsonAgent.stoppingDistance + completionRotationTolerance)
            yield return null;

        ReleaseWatsonAfterCompletionMovement();
    }

    private void ReleaseWatsonAfterCompletionMovement()
    {
        if (watsonAgent == null || !watsonAgent.isOnNavMesh)
            return;

        watsonAgent.ResetPath();
        watsonAgent.isStopped = false;
        watsonAgent.updateRotation = true;
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

    private void DisableWatsonEscortNpc()
    {
        if (watsonEscortNpcToDisable == null)
            return;

        WatsonEscortController escortController = WatsonEscortController.Instance;
        if (escortController != null && escortController.IsEscortingNpc(watsonEscortNpcToDisable))
            escortController.ForceFarewell();

        watsonEscortNpcToDisable.enabled = false;
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
