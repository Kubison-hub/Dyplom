using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibraryBook : Lvl3InteractionDialogueBase, IVioletRoomInteractionGate
{
    private Interactable interactable;
    public bool performed;

    [Header("Audio")]
    [SerializeField] private AudioSource useAudio;
    [SerializeField, Min(0f)] private float audioCooldown = 0.45f;
    private float nextAudioTime;

    [Header("Book Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] bookDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "137, otwarta książka na 137 stronie.",
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

    private bool isWalkingToWaitPoint;
    private bool firstVioletGateTriggered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => bookDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (useAudio == null)
            useAudio = GetComponent<AudioSource>();
    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;

        if (useAudio != null && Time.time >= nextAudioTime)
        {
            useAudio.Play();
            nextAudioTime = Time.time + audioCooldown;
        }

        PlayDialogue(player, bookDialogue);

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
        debugVioletObservesFromOutside = violetDialog != null && violetDialog.IsOutsideLibraryButObserving;
        return debugIsVioletInRoom || debugIsLibraryObserved;
    }

    private System.Collections.IEnumerator ShowVioletTextAtWaitPoint(PlayerController player)
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

    private System.Collections.IEnumerator PlayVioletGateDialogue()
    {
        PlayDialogue(null, debugVioletObservesFromOutside ? violetObservesLibraryDialogue : violetPresentDialogue);
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
}
