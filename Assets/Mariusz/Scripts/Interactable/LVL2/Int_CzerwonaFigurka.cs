using UnityEngine;

public class Int_CzerwonaFigurka : Lvl3InteractionDialogueBase
{
    public ItemType itemType;
    public GameObject spline;
    private Interactable interactable;
    private bool collected;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Library Safe")]
    [SerializeField] private Animator safeAnimator;

    [Header("Pickup Dialogue")]
    [SerializeField]
    private Lvl3DialogueLine[] pickupDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Czerwona figurka.", duration = 2f }
    };
    [SerializeField]
    private Lvl3DialogueLine[] noSpaceDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Nie mam miejsca w ekwipunku.", duration = 2f }
    };

    private bool destroyAfterDialogue;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => pickupDialogue;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.addDatabaseNotesAutomatically = false;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (collected)
            return;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(itemType, inventoryIcon))
        {
            PlayDialogue(player, noSpaceDialogue);
            return;
        }

        collected = true;
        interactable?.MarkCompleted();

        // Zapis: bez tego figurka wroci na podloge po wczytaniu gry,
        // mimo ze bedzie juz w ekwipunku.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);

        CluesLog.Instance?.RegisterTableMechanismElement(ItemType.Czerwona);
        SherlockWatsonHintConditions hintConditions =
            FindFirstObjectByType<SherlockWatsonHintConditions>(FindObjectsInactive.Include);
        hintConditions?.MarkMagicBallFound();

        bool tableHoverDiscovered = hintConditions != null && hintConditions.IsTableHoverDiscovered;
        int noteIndex = tableHoverDiscovered ? 2 : 0;
        int additionalNoteIndex = tableHoverDiscovered ? 3 : 1;
        interactable?.AddNote(noteIndex);
        interactable?.AddNote(additionalNoteIndex);

        if (spline != null)
            spline.SetActive(false);

        if (safeAnimator != null)
            safeAnimator.SetTrigger("Close");

        HideCollectedFigure();
        destroyAfterDialogue = true;
        PlayDialogue(player, pickupDialogue);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (destroyAfterDialogue && lines == pickupDialogue)
        {
            Destroy(gameObject);
        }
    }

    private void HideCollectedFigure()
    {
        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);
            interactable.interactiveShader = null;
        }

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = false;

        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }
}
