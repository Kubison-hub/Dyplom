using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_LibraryBooks : Lvl3InteractionDialogueBase, IVioletRoomInteractionGate
{
    [Header("Bookshelf Dialogue")]
    [SerializeField, Min(0f)] private float dialogueDelay = 0.5f;
    [SerializeField] private Lvl3DialogueLine[] bookshelfDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Jak szukać, to tylko tutaj...",
            duration = 3f
        }
    };

    [Header("Violet Gate")]
    [SerializeField] private Int_VioletDialog violetDialog;
    [SerializeField] private Transform waitInteractionPoint;
    [SerializeField] private GameObject activateOnFirstVioletGate;
    [SerializeField] private Transform intPointWatson;

    [Header("Violet Present Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] violetPresentDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Lepiej by\u0142oby pomyszkowa\u0107 tu w samotno\u015bci.",
            duration = 3f
        }
    };

    [Header("Violet Observes From Outside Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] violetObservesLibraryDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Violet nadal zerka w stron\u0119 biblioteki, John.",
            duration = 3f
        }
    };

    [Header("Debug")]
    [SerializeField] private bool debugIsVioletInRoom;
    [SerializeField] private bool debugIsLibraryObserved;
    [SerializeField] private bool debugVioletObservesFromOutside;
    [SerializeField] private bool debugVioletGateRedirected;

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool isWalkingToWaitPoint;
    private bool firstVioletGateTriggered;
    private bool noteAdded;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => bookshelfDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.addDatabaseNotesAutomatically = false;

        interactionCollider = GetComponent<Collider>();

    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        AddLibraryNoteIfNeeded();

        StartCoroutine(PlayBookshelfDialogueAfterDelay(player));

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

        if (player != null)
            player.currentInteractable = null;
    }

    public bool RedirectWhenVioletIsInRoom(PlayerController player)
    {
        debugVioletGateRedirected = IsVioletInRoom();
        if (!debugVioletGateRedirected)
            return false;

        if (player == null)
            return true;

        player.currentInteractable = null;

        if (waitInteractionPoint == null)
        {
            isWalkingToWaitPoint = true;
            StartCoroutine(PlayVioletGateDialogue(player));
            return true;
        }

        if (!isWalkingToWaitPoint)
        {
            isWalkingToWaitPoint = true;
            player.MoveToPoint(waitInteractionPoint.position);
            StartCoroutine(ShowVioletTextAtWaitPoint(player));
        }

        return true;
    }

    private void AddLibraryNoteIfNeeded()
    {
        if (noteAdded || interactable == null)
            return;

        noteAdded = true;
        interactable.AddAllDatabaseNotes();
    }

    private bool IsVioletInRoom()
    {
        if (violetDialog == null)
            violetDialog = Int_VioletDialog.Instance;

        violetDialog?.RefreshLibraryAwareness();
        debugIsVioletInRoom = violetDialog != null && violetDialog.isVioletInRoom;
        debugIsLibraryObserved = violetDialog != null && violetDialog.IsLibraryObserved;
        debugVioletObservesFromOutside = violetDialog != null && violetDialog.IsOutsideLibraryButObserving;
        return debugIsVioletInRoom || debugIsLibraryObserved;
    }

    private IEnumerator ShowVioletTextAtWaitPoint(PlayerController player)
    {
        while (player != null && player.navMeshAgent != null)
        {
            Vector3 toWaitPoint = waitInteractionPoint.position - player.transform.position;
            toWaitPoint.y = 0f;
            bool reachedWaitPoint = toWaitPoint.magnitude <= player.navMeshAgent.stoppingDistance + 0.15f;
            bool arrived = !player.navMeshAgent.pathPending &&
                           reachedWaitPoint &&
                           player.navMeshAgent.remainingDistance <= player.navMeshAgent.stoppingDistance + 0.05f &&
                           (!player.navMeshAgent.hasPath || player.navMeshAgent.velocity.sqrMagnitude <= 0.01f);
            if (arrived)
                break;

            yield return null;
        }

        if (player != null)
            yield return PlayVioletGateDialogue(player);

    }

    private IEnumerator PlayVioletGateDialogue(PlayerController player)
    {
        yield return RotateSherlockTowardsBooks(player);

        AddLibraryNoteIfNeeded();
        PlayDialogue(null, debugVioletObservesFromOutside ? violetObservesLibraryDialogue : violetPresentDialogue);
        while (IsDialoguePlaying)
            yield return null;

        ActivateFirstVioletGateObject();
        MoveWatsonToGatePoint();
        isWalkingToWaitPoint = false;
    }

    private IEnumerator RotateSherlockTowardsBooks(PlayerController player)
    {
        if (player == null ||
            (player.playerCharacter != PlayerCharacter.Sherlock && !player.CompareTag("PlayerA")))
            yield break;

        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        bool restoreAgentRotation = player.navMeshAgent != null && player.navMeshAgent.updateRotation;
        if (player.navMeshAgent != null)
            player.navMeshAgent.updateRotation = false;

        float rotationSpeed = Mathf.Max(1f, player.interactionPointRotationSpeed);
        while (Quaternion.Angle(player.transform.rotation, targetRotation) > 1f)
        {
            player.transform.rotation = Quaternion.RotateTowards(
                player.transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
            yield return null;
        }

        player.transform.rotation = targetRotation;
        if (player.navMeshAgent != null)
            player.navMeshAgent.updateRotation = restoreAgentRotation;
    }

    private void ActivateFirstVioletGateObject()
    {
        if (firstVioletGateTriggered)
            return;

        firstVioletGateTriggered = true;
        FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include)
            ?.MarkLibraryGateOpened();

        if (activateOnFirstVioletGate != null)
            activateOnFirstVioletGate.SetActive(true);
    }

    private void MoveWatsonToGatePoint()
    {
        if (intPointWatson == null || SwitchCharacter.Instance == null ||
            SwitchCharacter.Instance.players == null || SwitchCharacter.Instance.players.Length < 2 ||
            SwitchCharacter.Instance.players[1] == null)
            return;

        PlayerController watson = SwitchCharacter.Instance.players[1].GetComponent<PlayerController>();
        watson?.MoveToPoint(intPointWatson.position);
    }

    private IEnumerator PlayBookshelfDialogueAfterDelay(PlayerController player)
    {
        if (dialogueDelay > 0f)
            yield return new WaitForSeconds(dialogueDelay);

        PlayDialogue(player, bookshelfDialogue);
    }
}
