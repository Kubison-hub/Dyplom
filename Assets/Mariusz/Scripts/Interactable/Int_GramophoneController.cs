using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public sealed class Int_GramophoneController : Int_lv1_NpcDialogBase, IInteractionApproachPointProvider
{
    [Header("Character Introduction Lines")]
    [SerializeField] private Lvl3DialogueLine sherlockIntroductionLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Podejdź bliżej Watsonie, spójrz na ten osobliwy mechanizm",
        duration = 4f
    };

    [SerializeField] private Lvl3DialogueLine watsonIntroductionLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        text = "Sherlock, musisz to zobaczyć!",
        duration = 3f
    };

    [Header("Record Mechanism Dialogue")]
    [SerializeField] private Lvl3DialogueLine recordInsertedLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Gotowe, teraz wystarczy uruchomić mechanizm",
        duration = 3f
    };

    [SerializeField] private Lvl3DialogueLine sherlockMissingRecordLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Brakuje płyty",
        duration = 2f
    };

    [SerializeField] private Lvl3DialogueLine watsonMissingRecordLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        text = "Brakuje płyty",
        duration = 2f
    };

    [Header("Record Collection")]
    [SerializeField] private bool canCollect;
    [SerializeField] private ItemType requiredRecord = ItemType.GramophoneRecord;
    [Tooltip("Record model shown after inserting the inventory item into the mechanism.")]
    [SerializeField] private GameObject insertedRecordVisual;
    [Tooltip("Local Z-axis rotation speed after starting the mechanism.")]
    [SerializeField] private float recordRotationSpeed = 18f;

    [Header("Record Audio")]
    [Tooltip("Dedicated source used for the sound played by the inserted record.")]
    [SerializeField] private AudioSource recordAudioSource;
    [SerializeField] private AudioClip recordAudioClip;
    [SerializeField] private bool loopRecordAudio = true;

    [Header("Progress After Conversation")]
    [SerializeField] private Int_StairsUp stairsUp;
    [Tooltip("Escort components enabled after the gramophone conversation, for example Selma.")]
    [SerializeField] private WatsonEscortNPC[] nextWatsonEscortComponents;

    [Header("Gramophone Animation")]
    [SerializeField] private Animator gramophoneAnimator;
    [Tooltip("The Interactable component belonging to the gramophone itself.")]
    [SerializeField] private Interactable gramophoneInteractable;

    [Header("Violet Gramophone Reaction")]
    [SerializeField] private SmartNPC violetSmartNpc;
    [SerializeField] private Animator violetAnimator;
    [SerializeField] private string violetWalkingParameter = "IsWalking";
    [SerializeField] private Transform violetGramPoint;
    [SerializeField, Min(0f)] private float violetMoveDelay = 2f;
    [SerializeField, Min(1f)] private float violetFinalRotationSpeed = 180f;

    [Header("Detective Gramophone Reaction")]
    [SerializeField] private PlayerController sherlock;
    [SerializeField] private PlayerController watson;
    [SerializeField] private Transform sherlockGramPoint;
    [SerializeField] private Transform watsonGramPoint;
    [SerializeField, Min(0.05f)] private float gramPointArrivalDistance = 0.15f;
    [SerializeField, Min(0.05f)] private float gramPointNavMeshSampleRadius = 1f;
    [SerializeField, Min(1f)] private float gramophoneMovementTimeout = 15f;
    [SerializeField, Min(1f)] private float detectiveFinalRotationSpeed = 360f;
    [Tooltip("Conversation started after Violet, Sherlock and Watson reach their gramophone points.")]
    [SerializeField] private SmartNPC afterMovementSmartNpc;

    [Header("After Movement Conversation Intro Audio")]
    [SerializeField] private AudioSource afterMovementIntroAudioSource;
    [SerializeField] private AudioClip afterMovementIntroAudioClip;

    [Header("Door Closing At Dialogue Start")]
    [SerializeField] private Animator doorAnimator;

    private static readonly int DoorOpenedBool = Animator.StringToHash("Opened");
    private static readonly int DoorCloseTrigger = Animator.StringToHash("Close");

    private static readonly int PlayGramTrigger = Animator.StringToHash("PlayGram");

    [Header("Two Character Approach")]
    [Tooltip("Exact destination always used by Watson. Sherlock always uses Interactable > Interaction Point.")]
    [SerializeField] private Transform reactionPoint;
    [SerializeField, Min(0.1f)] private float arrivalTolerance = 0.65f;
    [SerializeField, Min(0.01f)] private float finalRotationDuration = 0.35f;

    private Interactable interactable;
    private Coroutine beginConversationCoroutine;
    private Lvl3DialogueLine[] sherlockIntroductionLines;
    private Lvl3DialogueLine[] watsonIntroductionLines;
    private Lvl3DialogueLine[] recordInsertedLines;
    private Lvl3DialogueLine[] sherlockMissingRecordLines;
    private Lvl3DialogueLine[] watsonMissingRecordLines;
    private bool introductionDialogueCompleted;
    private bool conversationCompleted;
    private bool recordInserted;
    private bool mechanismActivated;
    private bool afterMovementConversationCompleted;
    private Coroutine gramophoneSequenceCoroutine;
    private System.Action releaseGramophoneSequence;
    private readonly System.Collections.Generic.List<Coroutine> gramophoneMovementCoroutines =
        new System.Collections.Generic.List<Coroutine>();

    public bool CanCollect => canCollect;
    public bool ConversationCompleted => conversationCompleted;

    protected override InteractionType RequiredInteractionType => InteractionType.Int_GramophoneController;

    public Transform GetInteractionApproachPoint(PlayerController player, Transform defaultPoint)
    {
        return IsWatson(player) && reactionPoint != null
            ? reactionPoint
            : defaultPoint;
    }

    protected override void Start()
    {
        interactable = GetComponent<Interactable>();
        sherlockIntroductionLines = new[] { sherlockIntroductionLine };
        watsonIntroductionLines = new[] { watsonIntroductionLine };
        recordInsertedLines = new[] { recordInsertedLine };
        sherlockMissingRecordLines = new[] { sherlockMissingRecordLine };
        watsonMissingRecordLines = new[] { watsonMissingRecordLine };

        if (insertedRecordVisual != null)
            insertedRecordVisual.SetActive(recordInserted);

        if (stairsUp == null)
            stairsUp = FindFirstObjectByType<Int_StairsUp>();

        ConfigureCompanionApproach();
        base.Start();
    }

    public new void PerformInteraction(PlayerController player)
    {
        if (afterMovementConversationCompleted || gramophoneSequenceCoroutine != null ||
            beginConversationCoroutine != null || IsDialoguePlaying)
        {
            ClearPlayerInteraction(player);
            return;
        }

        if (mechanismActivated)
        {
            ClearPlayerInteraction(player);
            gramophoneSequenceCoroutine = StartCoroutine(MoveCharactersToGramophoneAfterDelay());
            return;
        }

        if (conversationCompleted)
        {
            HandleRecordMechanismInteraction(player);
            return;
        }

        introductionDialogueCompleted = false;
        PlayDialogue(player, IsWatson(player) ? watsonIntroductionLines : sherlockIntroductionLines);
        beginConversationCoroutine = StartCoroutine(BeginConversationWhenBothCharactersArrive(player));
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (ReferenceEquals(lines, sherlockIntroductionLines) ||
            ReferenceEquals(lines, watsonIntroductionLines))
            introductionDialogueCompleted = true;
    }

    protected override void OnNpcDialogueFinished()
    {
        if (conversationCompleted)
            return;

        canCollect = true;
        conversationCompleted = true;
        SherlockWatsonHintConditions hintConditions =
            FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include);
        hintConditions?.MarkGramophoneConversationComplete();
        hintConditions?.MarkEthelPassageDoorUsed();
        CluesLog.Instance?.SetFindEthelUpstairsObjective();
        UnlockStairsAndEscortComponents();

        // The companion is needed for the initial conversation only. Later clicks
        // insert the record or operate the mechanism without repositioning both characters.
        if (interactable != null)
        {
            interactable.rotateWatsonToInteraction = false;
            interactable.rotateSherlockToInteraction = false;
        }
    }

    public void SetCanCollect(bool value)
    {
        canCollect = value;
    }

    private void UnlockStairsAndEscortComponents()
    {
        if (stairsUp != null)
            stairsUp.isSherlockWantToGoUpstairs = true;
        else
            Debug.LogWarning($"{name}: Int_StairsUp is not assigned.", this);

        if (nextWatsonEscortComponents == null)
            return;

        foreach (WatsonEscortNPC escortComponent in nextWatsonEscortComponents)
        {
            if (escortComponent != null)
                escortComponent.enabled = true;
        }
    }

    private void HandleRecordMechanismInteraction(PlayerController player)
    {
        if (!recordInserted)
        {
            InventoryManager inventory = InventoryManager.Instance;
            if (inventory == null || !inventory.TryRemoveItem(requiredRecord))
            {
                PlayDialogue(player, IsWatson(player)
                    ? watsonMissingRecordLines
                    : sherlockMissingRecordLines);
                return;
            }

            recordInserted = true;
            if (insertedRecordVisual != null)
                insertedRecordVisual.SetActive(true);

            PlayDialogue(player, recordInsertedLines);
            return;
        }

        mechanismActivated = true;
        ClearPlayerInteraction(player);

        if (gramophoneAnimator != null)
            gramophoneAnimator.SetTrigger(PlayGramTrigger);

        PlayRecordAudio();
        StartCoroutine(RotateInsertedRecord());
        gramophoneSequenceCoroutine = StartCoroutine(MoveCharactersToGramophoneAfterDelay());
        Debug.Log("URUCHOMIENIE MECHANIZMU", this);
    }

    private void PlayRecordAudio()
    {
        if (recordAudioSource == null || recordAudioClip == null)
            return;

        recordAudioSource.Stop();
        recordAudioSource.clip = recordAudioClip;
        recordAudioSource.loop = loopRecordAudio;
        recordAudioSource.Play();
    }

    private IEnumerator MoveCharactersToGramophoneAfterDelay()
    {
        ResolveDetectives();
        bool previousWorldInputLock = PlayerController.IsWorldInputLocked;
        bool previousCanSwitch = SwitchCharacter.Instance == null || SwitchCharacter.Instance.canSwitch;
        NotebookManager notebook = NotebookManager.Instance;
        PauseMenuManager pauseMenu = FindFirstObjectByType<PauseMenuManager>(FindObjectsInactive.Include);
        bool previousNotebookEnabled = notebook != null && notebook.enabled;
        bool previousPauseMenuEnabled = pauseMenu != null && pauseMenu.enabled;
        PlayerController.SetWorldInputLocked(true);
        if (SwitchCharacter.Instance != null)
            SwitchCharacter.Instance.canSwitch = false;
        if (notebook != null)
            notebook.enabled = false;
        if (pauseMenu != null)
            pauseMenu.enabled = false;

        releaseGramophoneSequence = () =>
        {
            StopGramophoneMovement();
            RestoreDetectiveAgentRotation(sherlock);
            RestoreDetectiveAgentRotation(watson);
            PlayerController.SetWorldInputLocked(previousWorldInputLock);
            if (SwitchCharacter.Instance != null)
                SwitchCharacter.Instance.canSwitch = previousCanSwitch;
            if (notebook != null)
                notebook.enabled = previousNotebookEnabled;
            if (pauseMenu != null)
                pauseMenu.enabled = previousPauseMenuEnabled;
            gramophoneSequenceCoroutine = null;
        };

        try
        {
            yield return RunGramophoneMovementAndConversation();
        }
        finally
        {
            ReleaseGramophoneSequence();
        }
    }

    private IEnumerator RunGramophoneMovementAndConversation()
    {
        SmartNPC violet = violetSmartNpc;
        if (violet == null && Int_VioletDialog.Instance != null)
            violet = Int_VioletDialog.Instance.smartNPC;
        if (violetSmartNpc == null)
            violetSmartNpc = violet;

        bool violetDone = false;
        bool sherlockDone = false;
        bool watsonDone = false;

        if (violet != null && violetGramPoint != null)
            gramophoneMovementCoroutines.Add(StartCoroutine(MoveVioletAfterDelay(violet, () => violetDone = true)));
        else
            Debug.LogWarning($"{name}: Violet or VioletGramPoint is not assigned.", this);

        gramophoneMovementCoroutines.Add(StartCoroutine(MoveDetectiveToGramPoint(sherlock, sherlockGramPoint, () => sherlockDone = true)));
        gramophoneMovementCoroutines.Add(StartCoroutine(MoveDetectiveToGramPoint(watson, watsonGramPoint, () => watsonDone = true)));

        float elapsed = 0f;
        while ((!violetDone || !sherlockDone || !watsonDone) && elapsed < gramophoneMovementTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        bool allCharactersArrived = violetDone && sherlockDone && watsonDone;
        if (!allCharactersArrived)
        {
            StopGramophoneMovement();
            SetVioletWalking(false);
            Debug.LogWarning($"{name}: gramophone character movement timed out.", this);
        }

        if (allCharactersArrived)
        {
            if (afterMovementSmartNpc != null)
            {
                if (StartAfterMovementConversation())
                {
                    yield return null;
                    while (DialogueEditor.ConversationManager.Instance != null &&
                           DialogueEditor.ConversationManager.Instance.IsConversationActive)
                        yield return null;

                    if (DialogueEditor.ConversationManager.Instance != null)
                    {
                        afterMovementConversationCompleted = true;
                        FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include)?
                            .MarkSelmaEscortConversationComplete();
                        CompleteInteraction();
                        CompleteInteractable(gramophoneInteractable);
                    }
                }
            }
            else
                Debug.LogWarning($"{name}: After Movement Smart NPC is not assigned.", this);
        }

        if (!afterMovementConversationCompleted)
            Debug.LogWarning($"{name}: Violet conversation is still pending. Use the gramophone again to retry.", this);
    }

    private void ReleaseGramophoneSequence()
    {
        System.Action release = releaseGramophoneSequence;
        releaseGramophoneSequence = null;
        release?.Invoke();
    }

    private void OnDisable()
    {
        if (gramophoneSequenceCoroutine != null)
            StopCoroutine(gramophoneSequenceCoroutine);
        ReleaseGramophoneSequence();
        ClearDialogueOnDisable();
    }

    private bool StartAfterMovementConversation()
    {
        if (afterMovementSmartNpc == null ||
            DialogueEditor.ConversationManager.Instance == null ||
            DialogueEditor.ConversationManager.Instance.IsConversationActive)
            return false;

        bool watsonActive = SwitchCharacter.Instance != null &&
                            SwitchCharacter.Instance.activePlayerIndex == 1;
        DialogueEditor.NPCConversation conversation = watsonActive
            ? afterMovementSmartNpc.rozmowaDlaPostaciB
            : afterMovementSmartNpc.rozmowaDlaPostaciA;

        if (conversation == null)
        {
            Debug.LogWarning(
                $"{name}: After Movement Smart NPC has no conversation assigned for the active character.",
                afterMovementSmartNpc);
            return false;
        }

        string playerId = watsonActive ? "PlayerB" : "PlayerA";
        if (QuestManager.Instance != null)
            QuestManager.Instance.OdnotujRozmowe(playerId, afterMovementSmartNpc.npcID);

        if (doorAnimator != null)
        {
            doorAnimator.SetBool(DoorOpenedBool, false);
            doorAnimator.SetTrigger(DoorCloseTrigger);
        }

        if (afterMovementIntroAudioSource != null && afterMovementIntroAudioClip != null)
            afterMovementIntroAudioSource.PlayOneShot(afterMovementIntroAudioClip);

        afterMovementSmartNpc.BeginDialogueCameraFocus(conversation);
        DialogueEditor.ConversationManager.Instance.StartConversation(conversation);

        if (!DialogueEditor.ConversationManager.Instance.IsConversationActive)
            return false;

        if (afterMovementSmartNpc.noteIDToUnlock >= 0 && JournalManager.Instance != null)
            JournalManager.Instance.UnlockNote(afterMovementSmartNpc.noteIDToUnlock);
        return true;
    }

    private IEnumerator MoveVioletAfterDelay(SmartNPC violet, System.Action completed)
    {
        yield return new WaitForSeconds(violetMoveDelay);

        NavMeshAgent agent = violet.navMeshAgent;
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(violetGramPoint.position, out NavMeshHit target,
                gramPointNavMeshSampleRadius, NavMesh.AllAreas))
            yield break;

        agent.updateRotation = true;
        agent.isStopped = false;
        SetVioletWalking(true);
        if (!agent.SetDestination(target.position))
            yield break;
        while (agent.pathPending)
            yield return null;
        while (agent.isActiveAndEnabled && agent.isOnNavMesh &&
               agent.pathStatus == NavMeshPathStatus.PathComplete &&
               (agent.remainingDistance > Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance) ||
                HorizontalDistance(violet.transform.position, target.position) >
                    Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance)))
            yield return null;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh ||
            HorizontalDistance(violet.transform.position, target.position) >
                Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance))
            yield break;
        SetVioletWalking(false);
        yield return FinalizeVioletAtGramPoint(violet, completed);
    }

    private IEnumerator FinalizeVioletAtGramPoint(SmartNPC violet, System.Action completed)
    {
        if (violet.navMeshAgent != null && violet.navMeshAgent.isOnNavMesh)
        {
            violet.navMeshAgent.ResetPath();
            violet.navMeshAgent.isStopped = true;
            violet.navMeshAgent.updateRotation = false;
        }

        Quaternion targetRotation = Quaternion.Euler(0f, violetGramPoint.eulerAngles.y, 0f);
        while (Quaternion.Angle(violet.transform.rotation, targetRotation) > 0.5f)
        {
            violet.transform.rotation = Quaternion.RotateTowards(
                violet.transform.rotation,
                targetRotation,
                violetFinalRotationSpeed * Time.deltaTime);
            yield return null;
        }

        violet.transform.rotation = targetRotation;
        completed?.Invoke();
    }

    private void SetVioletWalking(bool isWalking)
    {
        if (violetAnimator == null && violetSmartNpc != null)
            violetAnimator = violetSmartNpc.GetComponentInChildren<Animator>();

        if (violetAnimator == null || string.IsNullOrWhiteSpace(violetWalkingParameter))
            return;

        foreach (AnimatorControllerParameter parameter in violetAnimator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool &&
                parameter.name == violetWalkingParameter)
            {
                violetAnimator.SetBool(violetWalkingParameter, isWalking);
                return;
            }
        }
    }

    private IEnumerator MoveDetectiveToGramPoint(
        PlayerController detective,
        Transform destination,
        System.Action completed)
    {
        if (detective == null || destination == null)
        {
            Debug.LogWarning($"{name}: detective or gramophone destination is not assigned.", this);
            yield break;
        }

        NavMeshAgent agent = detective.navMeshAgent != null
            ? detective.navMeshAgent
            : detective.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(
                destination.position,
                out NavMeshHit target,
                gramPointNavMeshSampleRadius,
                NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: {detective.name} or '{destination.name}' is outside the NavMesh.", this);
            yield break;
        }

        agent.isStopped = false;
        agent.updateRotation = true;
        if (!agent.SetDestination(target.position))
            yield break;

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.isActiveAndEnabled && agent.isOnNavMesh &&
                   agent.pathStatus == NavMeshPathStatus.PathComplete &&
                   (agent.remainingDistance > Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance) ||
                    HorizontalDistance(detective.transform.position, target.position) >
                        Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance)))
                yield return null;

            if (!agent.isActiveAndEnabled || !agent.isOnNavMesh ||
                HorizontalDistance(detective.transform.position, target.position) >
                    Mathf.Max(gramPointArrivalDistance, agent.stoppingDistance))
                yield break;

            agent.ResetPath();
            agent.isStopped = true;
            agent.updateRotation = false;

            Vector3 forward = destination.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
                while (Quaternion.Angle(detective.transform.rotation, targetRotation) > 0.5f)
                {
                    detective.transform.rotation = Quaternion.RotateTowards(
                        detective.transform.rotation,
                        targetRotation,
                        detectiveFinalRotationSpeed * Time.deltaTime);
                    yield return null;
                }

                detective.transform.rotation = targetRotation;
            }
        }
        else
        {
            Debug.LogWarning($"{name}: {detective.name} cannot reach '{destination.name}'.", this);
            yield break;
        }

        completed?.Invoke();
    }

    private void StopGramophoneMovement()
    {
        foreach (Coroutine movement in gramophoneMovementCoroutines)
            if (movement != null)
                StopCoroutine(movement);
        gramophoneMovementCoroutines.Clear();
        if (!afterMovementConversationCompleted)
        {
            ResetGramophoneAgent(sherlock != null ? sherlock.navMeshAgent : null);
            ResetGramophoneAgent(watson != null ? watson.navMeshAgent : null);
            ResetGramophoneAgent(violetSmartNpc != null ? violetSmartNpc.navMeshAgent : null);
            SetVioletWalking(false);
        }
    }

    private static void ResetGramophoneAgent(NavMeshAgent agent)
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;
        agent.ResetPath();
        agent.updateRotation = true;
        agent.isStopped = false;
    }

    private static void RestoreDetectiveAgentRotation(PlayerController detective)
    {
        if (detective == null || detective.navMeshAgent == null ||
            !detective.navMeshAgent.isActiveAndEnabled || !detective.navMeshAgent.isOnNavMesh)
            return;

        detective.navMeshAgent.updateRotation = true;
        detective.navMeshAgent.isStopped = false;
    }

    private void ResolveDetectives()
    {
        if (sherlock != null && watson != null)
            return;

        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate == null)
                continue;

            if (sherlock == null && candidate.playerCharacter == PlayerCharacter.Sherlock)
                sherlock = candidate;
            else if (watson == null && candidate.playerCharacter == PlayerCharacter.Watson)
                watson = candidate;
        }
    }

    private IEnumerator RotateInsertedRecord()
    {
        while (mechanismActivated && insertedRecordVisual != null)
        {
            insertedRecordVisual.transform.Rotate(
                0f,
                0f,
                recordRotationSpeed * Time.deltaTime,
                Space.Self);
            yield return null;
        }
    }

    private IEnumerator BeginConversationWhenBothCharactersArrive(PlayerController activePlayer)
    {
        // Interactable starts the companion reaction immediately before calling PerformInteraction.
        yield return null;

        while (interactable != null && interactable.IsCompanionReactionApproachInProgress)
            yield return null;

        ResolveDetectives();
        if (!CharactersReachedTheirPoints())
        {
            Debug.LogWarning($"{name}: gramophone controller dialogue cancelled because both characters did not reach their points.", this);
            ClearPlayerInteraction(activePlayer);
            beginConversationCoroutine = null;
            yield break;
        }

        yield return RotateCharactersToPoints();

        while (IsDialoguePlaying)
            yield return null;

        if (!introductionDialogueCompleted)
        {
            beginConversationCoroutine = null;
            yield break;
        }

        beginConversationCoroutine = null;
        base.PerformInteraction(activePlayer);
    }

    private bool CharactersReachedTheirPoints()
    {
        if (sherlock == null || watson == null || interactable == null ||
            interactable.interactabePoint == null || reactionPoint == null)
            return false;

        return HorizontalDistance(sherlock.transform.position, interactable.interactabePoint.position) <= arrivalTolerance &&
               HorizontalDistance(watson.transform.position, reactionPoint.position) <= arrivalTolerance;
    }

    private IEnumerator RotateCharactersToPoints()
    {
        if (sherlock == null || watson == null || interactable == null ||
            interactable.interactabePoint == null || reactionPoint == null)
            yield break;

        Quaternion sherlockStartRotation = sherlock.transform.rotation;
        Quaternion watsonStartRotation = watson.transform.rotation;
        Quaternion sherlockTargetRotation = Quaternion.Euler(0f, interactable.interactabePoint.eulerAngles.y, 0f);
        Quaternion watsonTargetRotation = Quaternion.Euler(0f, reactionPoint.eulerAngles.y, 0f);
        float elapsed = 0f;

        while (elapsed < finalRotationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / finalRotationDuration);
            sherlock.transform.rotation = Quaternion.Slerp(sherlockStartRotation, sherlockTargetRotation, progress);
            watson.transform.rotation = Quaternion.Slerp(watsonStartRotation, watsonTargetRotation, progress);
            yield return null;
        }

        sherlock.transform.rotation = sherlockTargetRotation;
        watson.transform.rotation = watsonTargetRotation;
    }

    private void ConfigureCompanionApproach()
    {
        if (interactable == null)
            return;

        interactable.SetWatsonInteractionAllowed(true);
        interactable.rotateWatsonToInteraction = true;
        interactable.watsonApproachMode = CompanionApproachMode.SpecificTransform;
        interactable.watsonSpecificApproachPoint = reactionPoint;
        interactable.rotateSherlockToInteraction = true;
        interactable.sherlockApproachMode = CompanionApproachMode.SpecificTransform;
        interactable.sherlockSpecificApproachPoint = interactable.interactabePoint;
    }

    private void CompleteInteraction()
    {
        CompleteInteractable(interactable);
    }

    private static void CompleteInteractable(Interactable target)
    {
        if (target == null)
            return;

        target.MarkCompleted();
        target.isInteractableActive = false;
        target.allowQuestionFXWhenInactive = false;
        target.SetQuestionFXEagleVisionState(false);

        if (target.interactiveShader != null)
            target.interactiveShader.SetActive(false);

        target.interactiveShader = null;

        foreach (Collider interactionCollider in target.GetComponents<Collider>())
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerB") ||
                SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1);
    }

    private static void ClearPlayerInteraction(PlayerController player)
    {
        if (player == null)
            return;

        player.currentInteractable = null;
        player.currentInteractionPoint = null;
        player.ClearAutoInteractionApproachPoint();
        player.SetWaitingForInteractionReaction(false);
    }
}
