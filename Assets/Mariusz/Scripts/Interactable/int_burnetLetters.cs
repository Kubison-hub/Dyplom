using UnityEngine;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public sealed class int_burnetLetters : Lvl3InteractionDialogueBase
{
    [Header("Loupe Hover")]
    [SerializeField] private Collider hoverCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0f)] private float hoverDuration = 1.5f;

    [Header("Sherlock Dialogue")]
    [SerializeField] private Lvl3DialogueLine sherlockLine = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        duration = 3f
    };

    private float hoverStartedAt = -1f;
    private bool discovered;
    private Lvl3DialogueLine[] lines;
    private Interactable interactable;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => SetupInteractable(InteractionType.Int_BurnedLetters);
    private void OnValidate() => SetupInteractable(InteractionType.Int_BurnedLetters);

    private void Awake()
    {
        SetupInteractable(InteractionType.Int_BurnedLetters);
        interactable = GetComponent<Interactable>();
        interactable.addDatabaseNotesAutomatically = false;

        if (hoverCollider == null)
            hoverCollider = GetComponent<Collider>();
        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        sherlockLine.speaker = Lvl3DialogueSpeaker.Sherlock;
        lines = new[] { sherlockLine };
    }

    private void Update()
    {
        if (discovered || magnifier == null || hoverCollider == null || IsAnyDialoguePlaying ||
            !magnifier.TryGetActiveLoupeHit(out RaycastHit hit) || !IsHoverHit(hit.collider))
        {
            hoverStartedAt = -1f;
            return;
        }

        if (hoverStartedAt < 0f)
            hoverStartedAt = Time.unscaledTime;
        if (Time.unscaledTime - hoverStartedAt < hoverDuration)
            return;

        Discover(null);
    }

    public void PerformInteraction(PlayerController player)
    {
        Discover(player);
    }

    private void Discover(PlayerController player)
    {
        if (discovered)
            return;

        discovered = true;
        interactable.AddAllDatabaseNotes();
        interactable.MarkCompleted();
        interactable.isInteractableActive = false;
        interactable.allowQuestionFXWhenInactive = false;
        interactable.SetQuestionFXEagleVisionState(false);
        if (interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(false);
        interactable.interactiveShader = null;
        if (hoverCollider != null)
            hoverCollider.enabled = false;
        PlayDialogue(player, lines);
    }

    private bool IsHoverHit(Collider hitCollider)
    {
        return hitCollider != null &&
               (hitCollider == hoverCollider || hitCollider.transform.IsChildOf(hoverCollider.transform));
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] completedLines)
    {
        if (completedLines == lines)
            enabled = false;
    }
}
