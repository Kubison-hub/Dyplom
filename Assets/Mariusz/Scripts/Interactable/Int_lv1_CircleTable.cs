using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_CircleTable : Lvl3InteractionDialogueBase
{
    [SerializeField] private DetectiveIdeaPoint circleTableIdeaPoint;
    [SerializeField] private TableFigurePuzzle tableFigurePuzzle;
    [Header("Circle Table Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] circleTableDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Okrągły stół cały pokryty jest runami. To nie jest dekoracja.",
            duration = 3f
        }
    };
    public GameObject[] nextInteractions;
    private Interactable interactable;
    private bool performed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => circleTableDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        FindCircleTableIdeaPointIfNeeded();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        GetComponent<Interactable>()?.MarkCompleted();

        PlayDialogue(player, circleTableDialogue);

        FindCircleTableIdeaPointIfNeeded();
        circleTableIdeaPoint?.RevealFromExternalSource();

        tableFigurePuzzle?.BeginPuzzle();

        ActiveNextInteractions(true);

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

    }

    private void FindCircleTableIdeaPointIfNeeded()
    {
        if (circleTableIdeaPoint != null)
            return;

        DetectiveIdeaPoint[] ideaPoints = FindObjectsByType<DetectiveIdeaPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (DetectiveIdeaPoint point in ideaPoints)
        {
            if (point.ideaId != "IdeaPoint_CircleTable" && point.gameObject.name != "IdeaPoint_CircleTable")
                continue;

            circleTableIdeaPoint = point;
            circleTableIdeaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;
            return;
        }

        Debug.LogWarning("Int_lv1_CircleTable: IdeaPoint_CircleTable was not found.");
    }
    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }
    }
}
