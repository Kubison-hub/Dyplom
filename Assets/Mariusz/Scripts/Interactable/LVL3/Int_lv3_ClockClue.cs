using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ClockClue : Lvl3InteractionDialogueBase
{
    [Header("Loupe Discovery")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.1f)] private float loupeHoldDuration = 1f;

    [Header("Mannequin Trap")]
    [SerializeField] private Int_lv3_Manequine mannequinTrap;
    [SerializeField] private DetectiveIdeaPoint oldClockIdeaPoint;
    [Tooltip("Full dialogue played after the mannequin trap has been triggered.")]
    [SerializeField] private Lvl3DialogueLine[] afterTrapDialogueLines;

    private float loupeHoldStartedAt = -1f;
    private bool discovered;
    private bool revealBasementExitAfterDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Stary zegar, ostatnie tyknięcie wydał z siebie równo o piątej.",
            duration = 3.5f
        }
    };

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    private void Start()
    {
        if (loupeCollider == null)
            loupeCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();
    }

    private void Update()
    {
        if (discovered || magnifier == null || loupeCollider == null)
            return;

        bool isLoupeOverClock = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                                IsLoupeHitOnClock(hit.collider);
        if (!isLoupeOverClock)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt >= loupeHoldDuration)
            ResolveClue(null);
    }

    public void PerformInteraction(PlayerController player)
    {
        ResolveClue(player);
    }

    private void ResolveClue(PlayerController player)
    {
        if (!discovered)
        {
            discovered = true;
            oldClockIdeaPoint?.RevealFromExternalSource();
            FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include)?
                .MarkOldClockFound();
            revealBasementExitAfterDialogue = true;
        }

        bool trapTriggered = mannequinTrap != null && mannequinTrap.IsTrapTriggered;
        if (!trapTriggered)
        {
            loupeHoldStartedAt = -1f;
            PlayInteractionDialogue(player);
            return;
        }

        loupeHoldStartedAt = -1f;
        PlayDialogue(player, afterTrapDialogueLines);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!revealBasementExitAfterDialogue)
            return;

        revealBasementExitAfterDialogue = false;
        CluesLog.Instance?.AddFindBasementExitObjective();
    }

    private bool IsLoupeHitOnClock(Collider hitCollider)
    {
        return hitCollider == loupeCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeCollider.transform);
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_ClockClue);

        if (afterTrapDialogueLines != null && afterTrapDialogueLines.Length > 0)
            return;

        afterTrapDialogueLines = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = "Stary zegar, ostatnie tyknięcie wydał z siebie równo o piątej.",
                duration = 3.5f
            },
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = "Mimo tego nadal dzwoni?",
                duration = 2.5f
            }
        };
    }
}
