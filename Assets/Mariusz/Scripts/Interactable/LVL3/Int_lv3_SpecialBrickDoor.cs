using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_SpecialBrickDoor : Lvl3InteractionDialogueBase
{
    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "To przejście prowadzi dalej.",
            duration = 2.5f
        }
    };

    private void Reset() => SetupInteractable(InteractionType.Int_lv3_SpecialBrickDoor);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv3_SpecialBrickDoor);
    private void Awake() => SetupInteractable(InteractionType.Int_lv3_SpecialBrickDoor);

    public void PerformInteraction(PlayerController player)
    {
        PlayInteractionDialogue(player);
    }
}
