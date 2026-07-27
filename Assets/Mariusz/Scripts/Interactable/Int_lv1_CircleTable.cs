using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_CircleTable : MonoBehaviour
{
    [SerializeField] private DetectiveIdeaPoint circleTableIdeaPoint;
    [SerializeField] private TableFigurePuzzle tableFigurePuzzle;
    [SerializeField, TextArea] private string topText =
        "Okra\u0328g\u0142y st\u00f3\u0142 ca\u0142y pokryty jest runami. To nie jest dekoracja.";
    public GameObject[] nextInteractions;
    private Interactable interactable;
    private bool performed;

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

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(topText, "");

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

        if (player != null)
            player.currentInteractable = null;
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
