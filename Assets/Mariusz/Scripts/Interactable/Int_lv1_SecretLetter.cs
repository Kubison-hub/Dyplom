using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_SecretLetter : Lvl3InteractionDialogueBase
{
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

    private void Start()
    {
        SetupInteractable(InteractionType.Int_lv1_SecretLetter);
    }

    public void PerformInteraction(PlayerController player)
    {
        PlayInteractionDialogue(player);
    }
}
