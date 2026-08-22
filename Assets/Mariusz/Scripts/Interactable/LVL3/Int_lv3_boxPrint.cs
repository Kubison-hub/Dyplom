using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_boxPrint : Lvl3InteractionDialogueBase
{
    [Header("Loupe Discovery")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.1f)] private float fallbackLoupeHoldDuration = 1.5f;

    [Header("Result")]
    [SerializeField] private DetectiveIdeaPoint ideaPoint;

    private Interactable interactable;
    private float loupeHoldStartedAt = -1f;
    private bool discovered;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Zobacz, Watsonie, jak wyraźne ślady.",
            duration = 3f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Rzeczywiście!",
            duration = 2f
        }
    };

    private void Reset() => SetupInteractable(InteractionType.Int_lv3_boxPrint);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv3_boxPrint);

    private void Awake()
    {
        SetupInteractable(InteractionType.Int_lv3_boxPrint);
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (loupeCollider == null)
            loupeCollider = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (ideaPoint != null)
            ideaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;
    }

    private void Update()
    {
        if (discovered || magnifier == null || loupeCollider == null)
            return;

        bool isLoupeOverPrint = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) &&
                                 IsLoupeHitOnPrint(hit.collider);

        if (!isLoupeOverPrint)
        {
            loupeHoldStartedAt = -1f;
            return;
        }

        if (loupeHoldStartedAt < 0f)
            loupeHoldStartedAt = Time.unscaledTime;

        if (Time.unscaledTime - loupeHoldStartedAt >= GetLoupeHoldDuration())
            ResolveLoupeDiscovery();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;
    }

    private float GetLoupeHoldDuration()
    {
        return DetectiveIdeaManager.Instance != null
            ? DetectiveIdeaManager.Instance.magnifierDiscoveryDuration
            : fallbackLoupeHoldDuration;
    }

    private bool IsLoupeHitOnPrint(Collider hitCollider)
    {
        return hitCollider == loupeCollider ||
               hitCollider != null && hitCollider.transform.IsChildOf(loupeCollider.transform);
    }

    private void ResolveLoupeDiscovery()
    {
        if (discovered)
            return;

        discovered = true;
        loupeHoldStartedAt = -1f;

        ideaPoint?.RevealFromExternalSource();
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
}
