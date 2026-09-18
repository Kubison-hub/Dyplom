using System.Collections;
using DialogueEditor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Ethel : MonoBehaviour
{
    [Header("Ethel Movement")]
    [SerializeField] private NavMeshAgent ethelAgent;
    [SerializeField] private Transform firstTarget;
    [SerializeField] private Transform secondTarget;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;
    [SerializeField] private bool runToFirstTarget = true;
    [SerializeField] private bool runToSecondTarget = true;
    [SerializeField, Min(0.1f)] private float walkingSpeed = 1.6f;
    [SerializeField, Min(0.1f)] private float runningSpeed = 3.5f;

    [Header("Ethel Animation")]
    [SerializeField] private Animator ethelAnimator;
    [SerializeField] private string isWalkingParameter = "IsWalking";
    [SerializeField] private string isRunningParameter = "IsRunning";

    [Header("Ethel Dialogue")]
    [Tooltip("Optional Dialogue Editor conversation started before Ethel begins moving.")]
    [SerializeField] private SmartNPC ethelSmartNPC;
    [TextArea]
    [SerializeField] private string firstInteractionText = "ChodĹşcie za mnÄ….";
    [SerializeField] private AudioSource ethelVoiceSource;
    [Tooltip("Played when Ethel's SmartNPC conversation begins.")]
    [SerializeField] private AudioClip conversationIntroAudio;
    [SerializeField] private AudioClip firstInteractionAudio;
    [SerializeField, Min(0.1f)] private float dialogueDuration = 2.5f;

    [Header("After Opening The Door")]
    [SerializeField, Min(0f)] private float doorOpenWaitDuration = 1.5f;
    [TextArea]
    [SerializeField] private string afterDoorText = "Tędy!";
    [SerializeField] private AudioClip afterDoorAudio;

    [Header("Secret Door")]
    [SerializeField] private Animator ethelSecretDoorAnimator;
    [SerializeField] private string openDoorTrigger = "Open";
    [SerializeField] private string openedDoorBool = "Opened";
    [SerializeField] private AudioSource secretDoorAudioSource;
    [Tooltip("Room content activated immediately before Ethel opens the secret door.")]
    [SerializeField] private GameObject roomToActivate;

    [Header("Blackboard Reveal")]
    [Tooltip("Temporary blackboard hiding the area beyond Ethel's secret door.")]
    [SerializeField] private GameObject blackBoardToDisableOnDoorOpen;
    [SerializeField] private Material blackBoardFadeMaterial;
    [Tooltip("Delay after the door Open trigger before fading the blackboard.")]
    [SerializeField, Min(0f)] private float blackBoardFadeDelay = 0.35f;
    [SerializeField, Min(0.01f)] private float blackBoardFadeDuration = 1f;

    [Header("Next Interaction")]
    [Tooltip("Whole GameObject with Int_lv3_Ethel_2. It is enabled after Ethel reaches Second Target.")]
    [SerializeField] private GameObject ethel2InteractionGameObject;

    [Header("Notebook")]
    [Tooltip("Opens this Interactable's Database Note after Ethel's first conversation.")]
    [SerializeField] private bool openNotebookNoteAfterConversation;
    [SerializeField, Min(0)] private int notebookNoteIndex;

    [Header("Inactive Player Movement")]
    [Tooltip("Sherlock moves here when Watson started Ethel's conversation.")]
    [SerializeField] private Transform inactiveSherlockTarget;
    [Tooltip("Watson moves here when Sherlock started Ethel's conversation.")]
    [SerializeField] private Transform inactiveWatsonTarget;
    [SerializeField, Min(0f)] private float inactivePlayerMoveDelay = 2f;

    private Interactable interactable;
    private bool sequenceStarted;

    private void Reset() => SetupInteractable();
    private void OnValidate() => SetupInteractable();

    private void Awake()
    {
        SetupInteractable();

        if (interactable != null && openNotebookNoteAfterConversation)
            interactable.addDatabaseNotesAutomatically = false;

        if (ethelAgent == null)
            ethelAgent = GetComponent<NavMeshAgent>();

        if (ethelAgent == null)
            ethelAgent = gameObject.AddComponent<NavMeshAgent>();

        if (ethelAnimator == null)
            ethelAnimator = GetComponentInChildren<Animator>(true);

    }

    public void PerformInteraction(PlayerController player)
    {
        if (sequenceStarted)
            return;

        sequenceStarted = true;
        if (interactable != null)
            interactable.isInteractableActive = false;

        if (player != null)
            player.currentInteractable = null;

        StartCoroutine(StartConversationThenRunSequence(player));
    }

    private IEnumerator StartConversationThenRunSequence(PlayerController player)
    {
        // Let the interaction finish its current frame, but do not wait for the
        // companion's approach before Ethel begins her conversation.
        yield return null;

        PlayerController inactivePlayer = GetInactivePlayer(player);
        inactivePlayer?.SetConversationMovementAllowed(true);

        StartEthelConversation(player);
        CluesLog.Instance?.RemoveFindEthelObjective();

        // SmartNPC changes ConversationManager state on the following frame.
        yield return null;
        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        InventoryManager.Instance?.TryRemoveItem(ItemType.SmallBox);
        yield return OpenNotebookNoteAfterConversation();

        CluesLog.Instance?.RegisterBasementEvidence("EthelFirstConversation");
        CluesLog.Instance?.SetEthelFirstConversationDescription();
        inactivePlayer?.SetConversationMovementAllowed(false);
        StartCoroutine(MoveInactivePlayerAfterDelay(player));
        StartCoroutine(RunEthelSequence());
    }

    private IEnumerator OpenNotebookNoteAfterConversation()
    {
        if (!openNotebookNoteAfterConversation || interactable == null)
            yield break;

        interactable.AddAndOpenNote(notebookNoteIndex, -60f, true);
        yield return null;

        while (NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen)
            yield return null;
    }

    private void StartEthelConversation(PlayerController player)
    {
        if (ethelVoiceSource != null && conversationIntroAudio != null)
            ethelVoiceSource.PlayOneShot(conversationIntroAudio);

        if (ethelSmartNPC == null)
            return;

        ethelSmartNPC.SprawdzIZacznijRozmowe();

        if (player == null || ConversationManager.Instance == null || ConversationManager.Instance.IsConversationActive)
            return;

        NPCConversation conversation = player.playerCharacter == PlayerCharacter.Watson
            ? ethelSmartNPC.rozmowaDlaPostaciB
            : ethelSmartNPC.rozmowaDlaPostaciA;

        if (conversation == null)
        {
            Debug.LogWarning($"{name}: Ethel has no conversation assigned for the active player.", this);
            return;
        }

        string playerId = player.playerCharacter == PlayerCharacter.Watson ? "PlayerB" : "PlayerA";
        QuestManager.Instance?.OdnotujRozmowe(playerId, ethelSmartNPC.npcID);
        ethelSmartNPC.BeginDialogueCameraFocus(conversation);
        ConversationManager.Instance.StartConversation(conversation);

        if (ethelSmartNPC.noteIDToUnlock >= 0)
            JournalManager.Instance?.UnlockNote(ethelSmartNPC.noteIDToUnlock);
    }

    private IEnumerator RunEthelSequence()
    {
        ShowEthelText(firstInteractionText, firstInteractionAudio);

        yield return MoveEthelTo(firstTarget, runToFirstTarget);

        if (roomToActivate != null)
            roomToActivate.SetActive(true);

        if (ethelSecretDoorAnimator != null && !string.IsNullOrWhiteSpace(openDoorTrigger))
            ethelSecretDoorAnimator.SetTrigger(openDoorTrigger);

        if (ethelSecretDoorAnimator != null && !string.IsNullOrWhiteSpace(openedDoorBool))
            ethelSecretDoorAnimator.SetBool(openedDoorBool, true);

        secretDoorAudioSource?.Play();
        StartCoroutine(FadeBlackBoardAfterDoorOpen());

        if (doorOpenWaitDuration > 0f)
            yield return new WaitForSeconds(doorOpenWaitDuration);

        ShowEthelText(afterDoorText, afterDoorAudio);
        yield return MoveEthelTo(secondTarget, runToSecondTarget);

        if (ethel2InteractionGameObject != null)
            ethel2InteractionGameObject.SetActive(true);

        interactable?.MarkCompleted();
    }

    private IEnumerator MoveInactivePlayerAfterDelay(PlayerController activePlayer)
    {
        if (inactivePlayerMoveDelay > 0f)
            yield return new WaitForSeconds(inactivePlayerMoveDelay);

        PlayerController inactivePlayer = GetInactivePlayer(activePlayer);
        if (inactivePlayer == null)
            yield break;

        Transform target = inactivePlayer.playerCharacter == PlayerCharacter.Watson
            ? inactiveWatsonTarget
            : inactiveSherlockTarget;
        if (target == null || inactivePlayer.navMeshAgent == null || !inactivePlayer.navMeshAgent.isOnNavMesh)
            yield break;

        if (!NavMeshWallGuard.TryGetClearPath(inactivePlayer.navMeshAgent, target.position, out NavMeshPath path))
        {
            Debug.LogWarning($"{name}: inactive {inactivePlayer.playerCharacter} cannot reach '{target.name}'.", this);
            yield break;
        }

        inactivePlayer.currentInteractable = null;
        inactivePlayer.currentInteractionPoint = null;
        inactivePlayer.ClearAutoInteractionApproachPoint();
        inactivePlayer.navMeshAgent.isStopped = false;
        inactivePlayer.navMeshAgent.updateRotation = true;
        inactivePlayer.navMeshAgent.SetPath(path);
    }

    private static PlayerController GetInactivePlayer(PlayerController activePlayer)
    {
        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate != activePlayer)
                return candidate;
        }

        return null;
    }

    private IEnumerator MoveEthelTo(Transform target, bool run)
    {
        if (target == null || ethelAgent == null)
        {
            Debug.LogWarning($"{nameof(Int_lv3_Ethel)} on '{name}' is missing a NavMeshAgent or movement target.", this);
            yield break;
        }

        if (!ethelAgent.isOnNavMesh)
        {
            Debug.LogWarning($"{nameof(Int_lv3_Ethel)} on '{name}' is not placed on the NavMesh.", this);
            yield break;
        }

        ethelAgent.speed = run ? runningSpeed : walkingSpeed;
        SetMovementAnimation(!run, run);
        ethelAgent.isStopped = false;
        ethelAgent.SetDestination(target.position);

        while (ethelAgent.pathPending)
            yield return null;

        if (ethelAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            SetMovementAnimation(false, false);
            Debug.LogWarning($"{nameof(Int_lv3_Ethel)} cannot reach '{target.name}' on the NavMesh.", this);
            yield break;
        }

        while (ethelAgent.remainingDistance > Mathf.Max(arrivalDistance, ethelAgent.stoppingDistance))
            yield return null;

        ethelAgent.ResetPath();
        SetMovementAnimation(false, false);
        transform.rotation = target.rotation;
    }

    private void SetMovementAnimation(bool walking, bool running)
    {
        if (ethelAnimator == null)
            return;

        if (!string.IsNullOrWhiteSpace(isWalkingParameter))
            ethelAnimator.SetBool(isWalkingParameter, walking);

        if (!string.IsNullOrWhiteSpace(isRunningParameter))
            ethelAnimator.SetBool(isRunningParameter, running);
    }

    private void ShowEthelText(string text, AudioClip clip)
    {
        PlayerTopText.Instance?.ShowEthelTopText(text, dialogueDuration);

        if (ethelVoiceSource != null && clip != null)
            ethelVoiceSource.PlayOneShot(clip);
    }

    private IEnumerator FadeBlackBoardAfterDoorOpen()
    {
        if (blackBoardFadeDelay > 0f)
            yield return new WaitForSeconds(blackBoardFadeDelay);

        if (blackBoardToDisableOnDoorOpen == null)
            yield break;

        Renderer blackBoardRenderer = blackBoardToDisableOnDoorOpen.GetComponent<Renderer>();
        if (blackBoardRenderer == null)
        {
            blackBoardToDisableOnDoorOpen.SetActive(false);
            yield break;
        }

        if (blackBoardFadeMaterial != null)
            blackBoardRenderer.material = blackBoardFadeMaterial;

        Material material = blackBoardRenderer.material;
        string colorProperty = material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color";
        if (!material.HasProperty(colorProperty))
        {
            blackBoardToDisableOnDoorOpen.SetActive(false);
            yield break;
        }

        Color color = material.GetColor(colorProperty);
        float startAlpha = color.a;
        float elapsed = 0f;

        while (elapsed < blackBoardFadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / blackBoardFadeDuration);
            material.SetColor(colorProperty, color);
            yield return null;
        }

        color.a = 0f;
        material.SetColor(colorProperty, color);
        blackBoardToDisableOnDoorOpen.SetActive(false);
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_lv3_Ethel);
    }
}
