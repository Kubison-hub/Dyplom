using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ControlUnit : Lvl3InteractionDialogueBase
{
    [Header("Stage Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] firstDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] repeatDialogueLines;

    private bool hasBeenExamined;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    public void PerformInteraction(PlayerController player)
    {
        if (!hasBeenExamined)
        {
            CluesLog.Instance?.RegisterBasementEvidence("ControlUnit");
            CluesLog.Instance?.SetControlUnitDescription();
        }

        PlayDialogue(player, hasBeenExamined ? repeatDialogueLines : firstDialogueLines);
        hasBeenExamined = true;
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_ControlUnit);

        if (firstDialogueLines == null || firstDialogueLines.Length == 0)
        {
            firstDialogueLines = new[]
            {
                CreateLine(Lvl3DialogueSpeaker.Sherlock, "Spójrz, Watsonie, jaki skomplikowany mechanizm.", 3f),
                CreateLine(Lvl3DialogueSpeaker.Watson, "Rzeczywiście, intrygujące.", 2.5f),
                CreateLine(Lvl3DialogueSpeaker.Sherlock, "Wygląda na to, że to dzięki temu duchy przeszłości nawiedzają dom.", 4f)
            };
        }

        if (repeatDialogueLines == null || repeatDialogueLines.Length == 0)
        {
            repeatDialogueLines = new[]
            {
                CreateLine(Lvl3DialogueSpeaker.Sherlock, "Na razie lepiej niczego tu nie ruszać.", 3f)
            };
        }
    }

    private static Lvl3DialogueLine CreateLine(Lvl3DialogueSpeaker speaker, string text, float duration)
    {
        return new Lvl3DialogueLine
        {
            speaker = speaker,
            text = text,
            duration = duration
        };
    }
}
