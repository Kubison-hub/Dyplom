using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_LibraryBooks : Lvl3InteractionDialogueBase, IVioletRoomInteractionGate
{
    [Header("Camera")]
    [SerializeField] private CameraController cameraController;
    [SerializeField] private float horizontalAxis = 116.8f;
    [SerializeField] private float orbitSpeed = 5f;

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

    [Header("Debug")]
    [SerializeField] private bool debugIsVioletInRoom;
    [SerializeField] private bool debugIsLibraryObserved;
    [SerializeField] private bool debugVioletGateRedirected;

    private Interactable interactable;
    private Collider interactionCollider;
    private bool performed;
    private bool isWalkingToWaitPoint;
    private bool firstVioletGateTriggered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => bookshelfDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();

        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;

        cameraController?.SetZoomState(CameraZoomState.Medium);
        cameraController?.OrbitHorizontalAxisTo(horizontalAxis, orbitSpeed);
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
            StartCoroutine(PlayVioletGateDialogue());
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

    private bool IsVioletInRoom()
    {
        if (violetDialog == null)
            violetDialog = Int_VioletDialog.Instance;

        violetDialog?.RefreshLibraryAwareness();
        debugIsVioletInRoom = violetDialog != null && violetDialog.isVioletInRoom;
        debugIsLibraryObserved = violetDialog != null && violetDialog.IsLibraryObserved;
        return debugIsVioletInRoom || debugIsLibraryObserved;
    }

    private IEnumerator ShowVioletTextAtWaitPoint(PlayerController player)
    {
        while (player != null && player.navMeshAgent != null)
        {
            bool arrived = !player.navMeshAgent.pathPending &&
                           player.navMeshAgent.remainingDistance <= player.navMeshAgent.stoppingDistance + 0.05f &&
                           (!player.navMeshAgent.hasPath || player.navMeshAgent.velocity.sqrMagnitude <= 0.01f);
            if (arrived)
                break;

            yield return null;
        }

        if (player != null)
            yield return PlayVioletGateDialogue();

    }

    private IEnumerator PlayVioletGateDialogue()
    {
        PlayDialogue(null, violetPresentDialogue);
        while (IsDialoguePlaying)
            yield return null;

        ActivateFirstVioletGateObject();
        MoveWatsonToGatePoint();
        isWalkingToWaitPoint = false;
    }

    private void ActivateFirstVioletGateObject()
    {
        if (firstVioletGateTriggered)
            return;

        firstVioletGateTriggered = true;
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
