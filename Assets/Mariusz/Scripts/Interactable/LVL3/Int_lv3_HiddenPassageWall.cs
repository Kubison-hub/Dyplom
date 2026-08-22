using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_HiddenPassageWall : Lvl3InteractionDialogueBase
{
    [Header("Stage Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] firstDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] secondDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] repeatDialogueLines;

    [Header("Line Renderer Visibility")]
    [Tooltip("Defaults to the MaskCanvas child. Its LineRenderers use the same Clues layer as Int_lv3_bsWallDoor.")]
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private string undiscoveredLayerName = "Clues";
    [SerializeField] private Color hiddenSegmentColor = new Color(0.08f, 0.22f, 0.12f, 0.35f);

    private int interactionCount;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset()
    {
        Setup();
    }

    private void OnValidate()
    {
        Setup();
    }

    private void Awake()
    {
        Setup();
        SetupLineRenderers();
    }

    public void PerformInteraction(PlayerController player)
    {
        interactionCount++;

        Lvl3DialogueLine[] selectedDialogue = interactionCount switch
        {
            1 => firstDialogueLines,
            2 => secondDialogueLines,
            _ => repeatDialogueLines
        };

        PlayDialogue(player, selectedDialogue);
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_HiddenPassageWall);

        if (firstDialogueLines == null || firstDialogueLines.Length == 0)
        {
            firstDialogueLines = new[]
            {
                CreateSherlockLine("Ta ściana nie jest wmurowana do reszty.", 3f)
            };
        }

        if (secondDialogueLines == null || secondDialogueLines.Length == 0)
        {
            secondDialogueLines = new[]
            {
                CreateSherlockLine("Nie widzę żadnych mechanizmów.", 3f)
            };
        }

        if (repeatDialogueLines == null || repeatDialogueLines.Length == 0)
        {
            repeatDialogueLines = new[]
            {
                CreateSherlockLine("Hmm...", 2f)
            };
        }
    }

    private void SetupLineRenderers()
    {
        if (segmentsParent == null)
            segmentsParent = transform.Find("MaskCanvas");

        if (segmentsParent == null)
            return;

        int cluesLayer = LayerMask.NameToLayer(undiscoveredLayerName);
        foreach (LineRenderer segment in segmentsParent.GetComponentsInChildren<LineRenderer>(true))
        {
            segment.startColor = hiddenSegmentColor;
            segment.endColor = hiddenSegmentColor;

            if (cluesLayer >= 0)
                segment.gameObject.layer = cluesLayer;
        }
    }

    private static Lvl3DialogueLine CreateSherlockLine(string text, float duration)
    {
        return new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = text,
            duration = duration
        };
    }
}
