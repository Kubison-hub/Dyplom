using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_SpecialBrick : Lvl3InteractionDialogueBase
{
    [Header("Loupe Discovery")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.1f)] private float loupeHoldDuration = 1f;

    [Header("Result")]
    [Tooltip("GameObjects activated after the brick is discovered.")]
    [SerializeField] private GameObject[] activateOnDiscovered;

    private Interactable interactable;
    private float loupeHoldStartedAt = -1f;
    private bool discovered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ta cegła wyraźnie różni się od pozostałych.",
            duration = 3f
        }
    };

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (loupeCollider == null)
            loupeCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();
    }

    private void Update()
    {
        if (discovered || magnifier == null || loupeCollider == null)
            return;

        bool isLoupeOverBrick = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                                IsLoupeHitOnBrick(hit.collider);
        if (!isLoupeOverBrick)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt >= loupeHoldDuration)
            Discover();
    }

    public void PerformInteraction(PlayerController player)
    {
        // This clue is intentionally available only through the loupe.
        if (player != null)
            player.currentInteractable = null;
    }

    private void Discover()
    {
        if (discovered)
            return;

        discovered = true;
        loupeHoldStartedAt = -1f;

        foreach (GameObject target in activateOnDiscovered)
        {
            if (target != null)
                target.SetActive(true);
        }

        PlayInteractionDialogue(null);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

        loupeCollider.enabled = false;
    }

    private bool IsLoupeHitOnBrick(Collider hitCollider)
    {
        return hitCollider == loupeCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeCollider.transform);
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_SpecialBrick);
    }
}
