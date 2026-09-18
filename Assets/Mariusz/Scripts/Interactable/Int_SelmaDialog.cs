using DialogueEditor;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class Int_SelmaDialog : Lvl3InteractionDialogueBase
{
    public SmartNPC smartNPC;
    public Interactable interactable;

    public bool performed = false;
    public CinemachineCamera dialogCam;
    //public CinemachineSplineDolly splineDolly;
    public Transform cardPosition;

    [Header("Face Player Before Dialogue")]
    [SerializeField] private bool facePlayerBeforeDialogue = true;
    [SerializeField, Min(1f)] private float facePlayerTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float facePlayerTolerance = 1f;

    [Header("Intro Dialogue")]
    [Tooltip("Played after Selma faces the player and before the SmartNPC conversation begins.")]
    [SerializeField] private Lvl3DialogueLine[] introDialogue;

    [Header("Repeat Dialogue")]
    [Tooltip("Set to false after Selma's first completed conversation, so later visits can skip the introductory branch.")]
    [SerializeField] private string firstDialogueParameterName = "First";

    [Header("Move Selma During Dialogue")]
    [Tooltip("Destination used by ReturnCameraAndMoveSelmaToPosition, callable from a Dialogue Editor event.")]
    [SerializeField] private Transform selmaDialogueMovePosition;
    [SerializeField, Min(0.05f)] private float selmaDestinationSampleRadius = 1f;
    [SerializeField, Min(0.01f)] private float watsonCameraTransitionSpeed = 0.2f;
    [SerializeField, Min(0f)] private float selmaDepartureCameraTransitionDuration = 2.5f;
    [SerializeField, Min(1f)] private float selmaDestinationRotationSpeed = 85f;
    [SerializeField, Min(0f)] private float sherlockFaceStartDelay = 0.35f;
    [SerializeField, Min(0f)] private float watsonFaceStartDelay = 0.2f;
    [SerializeField, Min(1f)] private float characterFaceRotationSpeed = 42.5f;
    [SerializeField] private Animator selmaAnimator;
    [SerializeField] private string selmaWalkingParameter = "IsWalking";

    private bool dialogueStarting;
    private Coroutine selmaMoveCoroutine;
    private Coroutine characterFaceCoroutine;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => introDialogue;

    public WatsonEscortNPC LinkedEscortNpc => smartNPC != null
        ? smartNPC.GetComponent<WatsonEscortNPC>()
        : null;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (selmaAnimator == null && smartNPC != null)
            selmaAnimator = smartNPC.GetComponentInChildren<Animator>();

        //splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

    }

    private void Update()
    {
       
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!dialogueStarting && ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            player.currentInteractable = null;
            StartCoroutine(BeginDialogue(player));

        }
        else
        {
            Debug.LogWarning("Cannot start Selma dialog while another conversation is active.");
            player.currentInteractable = null;
        }

       
        

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private IEnumerator BeginDialogue(PlayerController player)
    {
        dialogueStarting = true;

        if (facePlayerBeforeDialogue && smartNPC != null)
        {
            yield return NpcDialogueFacingUtility.FacePlayer(
                smartNPC.transform,
                player != null ? player.transform : null,
                facePlayerTurnSpeed,
                facePlayerTolerance);
        }

        if (introDialogue != null && introDialogue.Length > 0)
        {
            PlayDialogue(player, introDialogue);
            while (IsDialoguePlaying)
                yield return null;
        }

        if (smartNPC == null)
        {
            Debug.LogWarning($"{name}: SmartNPC is not assigned.", this);
            dialogueStarting = false;
            yield break;
        }

        smartNPC.SprawdzIZacznijRozmowe();

        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
            StartConversationWithoutTrigger(player);

        if (ConversationManager.Instance == null || !ConversationManager.Instance.IsConversationActive)
        {
            dialogueStarting = false;
            yield break;
        }

        yield return null;
        while (ConversationManager.Instance != null && ConversationManager.Instance.IsConversationActive)
            yield return null;

        SetFirstDialogueParameter(false);
        CluesLog.Instance?.RegisterSessionWitnessInterview(smartNPC);
        dialogueStarting = false;
    }

    private void SetFirstDialogueParameter(bool value)
    {
        if (smartNPC == null || string.IsNullOrWhiteSpace(firstDialogueParameterName))
            return;

        smartNPC.rozmowaDlaPostaciA?.SetRuntimeBoolParameter(firstDialogueParameterName, value);
        smartNPC.rozmowaDlaPostaciB?.SetRuntimeBoolParameter(firstDialogueParameterName, value);
    }

    private void StartConversationWithoutTrigger(PlayerController player)
    {
        if (player == null)
            return;

        NPCConversation conversation = player.playerCharacter == PlayerCharacter.Watson
            ? smartNPC.rozmowaDlaPostaciB
            : smartNPC.rozmowaDlaPostaciA;

        if (conversation == null)
        {
            Debug.LogWarning($"{name}: The selected player has no assigned NPC conversation.", this);
            return;
        }

        string playerId = player.playerCharacter == PlayerCharacter.Watson ? "PlayerB" : "PlayerA";
        QuestManager.Instance?.OdnotujRozmowe(playerId, smartNPC.npcID);
        smartNPC.BeginDialogueCameraFocus(conversation);
        ConversationManager.Instance.StartConversation(conversation);

        if (smartNPC.noteIDToUnlock >= 0)
            JournalManager.Instance?.UnlockNote(smartNPC.noteIDToUnlock);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            //splineDolly.CameraPosition = .8f;
            dialogCam.Priority = 50;
        }
    }

    public void ReturnCameraAndMoveSelmaToPosition()
    {
        SetCameraTargetToWatson();
        SetCameraZoomToMedium();

        if (selmaDialogueMovePosition == null)
        {
            Debug.LogWarning($"{name}: Selma Dialogue Move Position is not assigned.", this);
            return;
        }

        NavMeshAgent agent = smartNPC != null ? smartNPC.navMeshAgent : GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(selmaDialogueMovePosition.position, out NavMeshHit destination,
                selmaDestinationSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Selma Dialogue Move Position is not on the NavMesh.", selmaDialogueMovePosition);
            return;
        }

        agent.ResetPath();
        agent.updateRotation = true;
        agent.isStopped = false;
        agent.SetDestination(destination.position);
        StartCharactersFacingEachOther();

        if (selmaMoveCoroutine != null)
            StopCoroutine(selmaMoveCoroutine);

        SetSelmaWalking(true);
        selmaMoveCoroutine = StartCoroutine(WaitForSelmaToReachDestination(agent));
    }

    private IEnumerator WaitForSelmaToReachDestination(NavMeshAgent agent)
    {
        yield return null;

        while (agent != null && agent.isOnNavMesh &&
               (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.05f ||
                agent.velocity.sqrMagnitude > 0.01f))
        {
            yield return null;
        }

        agent.isStopped = true;
        agent.ResetPath();
        SetSelmaWalking(false);
        yield return RotateSelmaToDestinationRotation(agent.transform);
        selmaMoveCoroutine = null;
    }

    private IEnumerator RotateSelmaToDestinationRotation(Transform selma)
    {
        if (selma == null || selmaDialogueMovePosition == null)
            yield break;

        Quaternion targetRotation = selmaDialogueMovePosition.rotation;
        while (Quaternion.Angle(selma.rotation, targetRotation) > 0.5f)
        {
            selma.rotation = Quaternion.RotateTowards(
                selma.rotation,
                targetRotation,
                selmaDestinationRotationSpeed * Time.deltaTime);
            yield return null;
        }

        selma.rotation = targetRotation;
    }

    private void SetSelmaWalking(bool isWalking)
    {
        if (selmaAnimator != null && !string.IsNullOrWhiteSpace(selmaWalkingParameter))
            selmaAnimator.SetBool(selmaWalkingParameter, isWalking);
    }

    private void StartCharactersFacingEachOther()
    {
        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.players == null ||
            SwitchCharacter.Instance.players.Length < 2)
            return;

        Transform sherlock = SwitchCharacter.Instance.players[0] != null
            ? SwitchCharacter.Instance.players[0].transform
            : null;
        Transform watson = SwitchCharacter.Instance.players[1] != null
            ? SwitchCharacter.Instance.players[1].transform
            : null;
        if (sherlock == null || watson == null)
            return;

        if (characterFaceCoroutine != null)
            StopCoroutine(characterFaceCoroutine);

        characterFaceCoroutine = StartCoroutine(RotateCharactersTowardsEachOther(sherlock, watson));
    }

    private IEnumerator RotateCharactersTowardsEachOther(Transform sherlock, Transform watson)
    {
        Vector3 sherlockDirection = watson.position - sherlock.position;
        sherlockDirection.y = 0f;
        if (sherlockDirection.sqrMagnitude < 0.0001f)
        {
            characterFaceCoroutine = null;
            yield break;
        }

        Quaternion sherlockTargetRotation = Quaternion.LookRotation(sherlockDirection.normalized, Vector3.up);
        Quaternion watsonTargetRotation = Quaternion.LookRotation(-sherlockDirection.normalized, Vector3.up);

        if (sherlockFaceStartDelay > 0f)
            yield return new WaitForSeconds(sherlockFaceStartDelay);

        float watsonDelayRemaining = watsonFaceStartDelay;
        bool watsonStarted = watsonDelayRemaining <= 0f;

        while (Quaternion.Angle(sherlock.rotation, sherlockTargetRotation) > 0.5f ||
               Quaternion.Angle(watson.rotation, watsonTargetRotation) > 0.5f)
        {
            sherlock.rotation = Quaternion.RotateTowards(
                sherlock.rotation, sherlockTargetRotation, characterFaceRotationSpeed * Time.deltaTime);

            if (!watsonStarted)
            {
                watsonDelayRemaining -= Time.deltaTime;
                watsonStarted = watsonDelayRemaining <= 0f;
            }

            if (watsonStarted)
            {
                watson.rotation = Quaternion.RotateTowards(
                    watson.rotation, watsonTargetRotation, characterFaceRotationSpeed * Time.deltaTime);
            }
            yield return null;
        }

        sherlock.rotation = sherlockTargetRotation;
        watson.rotation = watsonTargetRotation;
        characterFaceCoroutine = null;
    }

    private void SetCameraTargetToWatson()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter == null)
            return;

        Transform watson = switchCharacter.watsonTransform != null
            ? switchCharacter.watsonTransform
            : switchCharacter.players != null && switchCharacter.players.Length > 1 && switchCharacter.players[1] != null
                ? switchCharacter.players[1].transform
                : null;
        if (watson == null || switchCharacter.playersCamera == null)
            return;

        int activeIndex = switchCharacter.activePlayerIndex;
        if (activeIndex < 0 || activeIndex >= switchCharacter.playersCamera.Length ||
            switchCharacter.playersCamera[activeIndex] == null)
            return;

        CameraController activeCamera = switchCharacter.playersCamera[activeIndex]
            .GetComponent<CameraController>();
        activeCamera?.OverrideLookAtTargetSmooth(watson, watsonCameraTransitionSpeed);
    }

    private void SetCameraZoomToMedium()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter == null || switchCharacter.playersCamera == null)
            return;

        int activeIndex = switchCharacter.activePlayerIndex;
        if (activeIndex < 0 || activeIndex >= switchCharacter.playersCamera.Length ||
            switchCharacter.playersCamera[activeIndex] == null)
            return;

        CameraController activeCamera = switchCharacter.playersCamera[activeIndex]
            .GetComponent<CameraController>();
        if (activeCamera == null)
            return;

        activeCamera.SetDialogueReturnZoomPreset("Medium");
        activeCamera.SetZoomPreset("Medium", selmaDepartureCameraTransitionDuration);
    }


    


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
