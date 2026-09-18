using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_SpecialBrick : Lvl3InteractionDialogueBase
{
    [Header("Result")]
    [Tooltip("GameObjects activated after the brick is discovered.")]
    [SerializeField] private GameObject[] activateOnDiscovered;

    private Interactable interactable;
    private bool discovered;
    private bool revealBasementExitAfterDialogue;

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
        revealBasementExitAfterDialogue = true;

        foreach (GameObject target in activateOnDiscovered)
        {
            if (target != null)
                target.SetActive(true);
        }

        PlayInteractionDialogue(player);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!revealBasementExitAfterDialogue)
            return;

        revealBasementExitAfterDialogue = false;
        CluesLog.Instance?.AddFindBasementExitObjective();
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_SpecialBrick);
    }
}
