using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_BoxWall : Lvl3InteractionDialogueBase
{
    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Te skrzynie tutaj nie stały.",
            duration = 2.5f
        },
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = "Rzeczywiście.",
            duration = 2f
        }
    };

    private void Reset() => SetupInteractable(InteractionType.Int_lv3_BoxWall);
    private void OnValidate() => SetupInteractable(InteractionType.Int_lv3_BoxWall);
    private void Awake() => SetupInteractable(InteractionType.Int_lv3_BoxWall);

    public void PerformInteraction(PlayerController player) => PlayInteractionDialogue(player);
}
