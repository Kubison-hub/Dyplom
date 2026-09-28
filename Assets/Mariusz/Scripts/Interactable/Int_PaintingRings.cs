using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public sealed class Int_PaintingRings : Lvl3InteractionDialogueBase
{
    [Header("Loupe Hover")]
    [SerializeField] private Collider hoverCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0f)] private float hoverDuration = 1f;

    [Header("Ring CP")]
    [SerializeField] private Int_Edith_Ring ringCluePoint;

    [Header("Sherlock Dialogue")]
    [FormerlySerializedAs("sherlockLine")]
    [SerializeField] private Lvl3DialogueLine ringNotFoundLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        duration = 3f
    };
    [SerializeField] private Lvl3DialogueLine ringFoundLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        duration = 3f
    };

    private Interactable interactable;
    private Lvl3DialogueLine[] ringNotFoundLines;
    private Lvl3DialogueLine[] ringFoundLines;
    private float hoverStartedAt = -1f;
    private bool discovered;
    private bool waitingForRingCluePoint;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();

    private void Awake()
    {
        Setup();
        interactable = GetComponent<Interactable>();
        interactable.addDatabaseNotesAutomatically = false;
        interactable.isInteractableActive = false;

        if (hoverCollider == null)
            hoverCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (ringCluePoint == null)
            ringCluePoint = FindFirstObjectByType<Int_Edith_Ring>(FindObjectsInactive.Include);

        ringNotFoundLine.speaker = Lvl3DialogueSpeaker.Sherlock;
        ringFoundLine.speaker = Lvl3DialogueSpeaker.Sherlock;
        ringNotFoundLines = new[] { ringNotFoundLine };
        ringFoundLines = new[] { ringFoundLine };
    }

    private void Update()
    {
        if (waitingForRingCluePoint)
        {
            hoverStartedAt = -1f;

            if (IsRingFound() && !IsAnyDialoguePlaying)
                ReactivateAfterRingFound();

            return;
        }

        if (discovered || magnifier == null || hoverCollider == null || IsAnyDialoguePlaying ||
            !magnifier.TryGetActiveLoupeHit(out RaycastHit hit) || !IsHoverHit(hit.collider))
        {
            hoverStartedAt = -1f;
            return;
        }

        if (hoverStartedAt < 0f)
            hoverStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - hoverStartedAt >= hoverDuration)
            Discover();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player == null)
            return;

        player.currentInteractable = null;
        player.currentInteractionPoint = null;
        player.ClearAutoInteractionApproachPoint();
        player.SetWaitingForInteractionReaction(false);
    }

    private void Discover()
    {
        if (discovered)
            return;

        discovered = true;
        bool ringFound = IsRingFound();
        interactable.AddNote(ringFound ? 1 : 0);
        hoverCollider.enabled = false;

        if (!ringFound)
        {
            waitingForRingCluePoint = true;
            PlayDialogue(null, ringNotFoundLines);
            return;
        }

        CompleteInteraction();
        PlayDialogue(null, ringFoundLines);
    }

    private bool IsRingFound()
    {
        return ringCluePoint != null && ringCluePoint.performed;
    }

    private void ReactivateAfterRingFound()
    {
        waitingForRingCluePoint = false;
        discovered = false;
        hoverStartedAt = -1f;

        if (hoverCollider != null)
            hoverCollider.enabled = true;
    }

    private void CompleteInteraction()
    {
        interactable.MarkCompleted();
        interactable.isInteractableActive = false;
        interactable.allowQuestionFXWhenInactive = false;
        interactable.SetQuestionFXEagleVisionState(false);

        if (interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(false);

        interactable.interactiveShader = null;
    }

    private bool IsHoverHit(Collider hitCollider)
    {
        return hitCollider != null &&
               (hitCollider == hoverCollider || hitCollider.transform.IsChildOf(hoverCollider.transform));
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_PaintingRings);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] completedLines)
    {
        if (completedLines == ringFoundLines)
            enabled = false;
    }
}
