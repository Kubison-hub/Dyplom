using System.Collections;
using UnityEngine;

public class TableFigurePuzzle : MonoBehaviour
{
    public static TableFigurePuzzle ActivePuzzle { get; private set; }

    [SerializeField, TextArea] private string startText =
        "Trzy barwne pola. Figurki najwyraźniej mają tu swoje miejsce.";
    [SerializeField, Min(0f)] private float startTextDelay = 1.25f;
    [SerializeField, TextArea] private string completedText =
        "Trzy figury, trzy miejsca. Stół odpowiedział bez słowa.";
    [SerializeField] private TableFigureSlot[] slots;
    [SerializeField] private GameObject[] activateOnSolved;

    private bool puzzleActive;
    private bool solved;

    public bool IsPuzzleActive => puzzleActive && !solved;

    private void Start()
    {
        SetSlotsAvailable(false);
    }

    private void OnDisable()
    {
        if (ActivePuzzle == this)
            ActivePuzzle = null;
    }

    public void BeginPuzzle()
    {
        if (solved)
            return;

        ActivePuzzle = this;
        puzzleActive = true;
        SetSlotsAvailable(true);

        StartCoroutine(ShowStartText());
    }

    public void NotifySlotFilled(TableFigureSlot slot)
    {
        if (!IsPuzzleActive || slot == null || !AreAllSlotsFilled())
            return;

        solved = true;
        puzzleActive = false;
        ActivePuzzle = null;
        SetSlotsAvailable(false);

        foreach (GameObject target in activateOnSolved)
        {
            if (target != null)
                target.SetActive(true);
        }

        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(completedText, "");
    }

    private bool AreAllSlotsFilled()
    {
        if (slots == null || slots.Length == 0)
            return false;

        foreach (TableFigureSlot slot in slots)
        {
            if (slot == null || !slot.IsFilled)
                return false;
        }

        return true;
    }

    private IEnumerator ShowStartText()
    {
        if (startTextDelay > 0f)
            yield return new WaitForSeconds(startTextDelay);

        if (IsPuzzleActive && PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(startText, "");
    }

    private void SetSlotsAvailable(bool available)
    {
        if (slots == null)
            return;

        foreach (TableFigureSlot slot in slots)
        {
            if (slot != null)
                slot.SetAvailable(available);
        }
    }
}
