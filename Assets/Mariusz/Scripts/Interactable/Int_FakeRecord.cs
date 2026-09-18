using UnityEngine;

[RequireComponent(typeof(Interactable))]
public sealed class Int_FakeRecord : Lvl3InteractionDialogueBase
{
    [Header("Fake Record Dialogue")]
    [SerializeField] private Lvl3DialogueLine sherlockDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Wygląda na to, że płyta przymocowana jest na stałe",
        duration = 4f
    };

    [Header("Notebook")]
    [SerializeField, Min(0)] private int noteIndex;

    private Interactable interactable;
    private Lvl3DialogueLine[] sherlockLines;
    private bool completed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();

    private void Awake()
    {
        Setup();
        interactable = GetComponent<Interactable>();
        sherlockLines = new[] { sherlockDialogue };
    }

    public void PerformInteraction(PlayerController player)
    {
        if (completed)
        {
            ClearPlayerInteraction(player);
            return;
        }

        PlayDialogue(player, sherlockLines);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!ReferenceEquals(lines, sherlockLines) || completed)
            return;

        completed = true;
        interactable?.AddNote(noteIndex);
        CompleteInteraction();
    }

    private void CompleteInteraction()
    {
        if (interactable != null)
        {
            interactable.MarkCompleted();
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        foreach (Collider interactionCollider in GetComponentsInChildren<Collider>(true))
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_FakeRecord);
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
