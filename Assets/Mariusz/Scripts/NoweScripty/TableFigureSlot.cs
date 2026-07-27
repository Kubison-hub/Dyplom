using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TableFigureSlot : MonoBehaviour
{
    [SerializeField] private ItemType requiredFigure;
    [SerializeField] private TableFigurePuzzle puzzle;
    [SerializeField] private GameObject slotGuide;
    [SerializeField] private GameObject placedFigure;
    [SerializeField, TextArea] private string wrongFigureText =
        "Ta figurka nie pasuje do tego znaku.";

    private Collider slotCollider;
    private bool filled;

    public bool IsFilled => filled;

    private void Awake()
    {
        slotCollider = GetComponent<Collider>();

        if (puzzle == null)
            puzzle = GetComponentInParent<TableFigurePuzzle>();

        if (placedFigure != null)
            placedFigure.SetActive(false);
    }

    public void SetAvailable(bool available)
    {
        bool shouldBeAvailable = available && !filled;

        if (slotCollider != null)
            slotCollider.enabled = shouldBeAvailable;

        if (slotGuide != null)
            slotGuide.SetActive(shouldBeAvailable);
    }

    public bool TryPlace(ItemType figure)
    {
        if (filled || puzzle == null || !puzzle.IsPuzzleActive)
            return false;

        if (figure != requiredFigure)
        {
            if (PlayerTopText.Instance != null)
                PlayerTopText.Instance.ShowTopText(wrongFigureText, "");

            return false;
        }

        filled = true;

        if (placedFigure != null)
            placedFigure.SetActive(true);

        SetAvailable(false);
        puzzle.NotifySlotFilled(this);
        return true;
    }
}
