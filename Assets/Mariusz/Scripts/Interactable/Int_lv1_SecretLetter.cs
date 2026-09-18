using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SecretLetter : Lvl3InteractionDialogueBase
{
    [Header("Completion")]
    [SerializeField] private Int_lv1_SelmaPortrait selmaPortrait;

    private static readonly Lvl3DialogueLine[] DefaultLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "1A4 - to warto zapamiętać",
            duration = 2f
        }
    };

    protected override Lvl3DialogueLine[] DefaultDialogueLines => DefaultLines;

    private bool found;

    private void Start()
    {
        SetupInteractable(InteractionType.Int_lv1_SecretLetter);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (found)
            return;

        found = true;
        PlayInteractionDialogue(player);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        CompleteOwnInteraction();
        selmaPortrait?.CompleteInteraction();
    }

    private void CompleteOwnInteraction()
    {
        Interactable interactable = GetComponent<Interactable>();
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

        foreach (Collider interactionCollider in GetComponents<Collider>())
            interactionCollider.enabled = false;
    }
}
