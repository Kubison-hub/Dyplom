using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_HeavyBox : Lvl3InteractionDialogueBase
{
    [Header("Watson Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] watsonDialogueLines;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ciężka drewniana skrzynia.",
            duration = 2.5f
        }
    };

    private void Reset() => SetupHeavyBox();
    private void OnValidate() => SetupHeavyBox();
    private void Awake() => SetupHeavyBox();

    public void PerformInteraction(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        PlayDialogue(player, isWatson ? watsonDialogueLines : ConfiguredDialogueLines);
    }

    private void SetupHeavyBox()
    {
        SetupInteractable(InteractionType.Int_lv3_HeavyBox);

        if (watsonDialogueLines == null || watsonDialogueLines.Length == 0)
        {
            watsonDialogueLines = new[]
            {
                new Lvl3DialogueLine
                {
                    speaker = Lvl3DialogueSpeaker.Watson,
                    text = "Ciężka drewniana skrzynia.",
                    duration = 2.5f
                }
            };
        }
    }
}
