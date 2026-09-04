using UnityEngine;

using Debug = UnityEngine.Debug;

public class Int_ZielonaFigurka : Lvl3InteractionDialogueBase
{
    public ItemType itemType;
    private bool collected;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Pickup Dialogue")]
    [SerializeField]
    private Lvl3DialogueLine[] pickupDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Ciekawe. Zielona figurka.", duration = 2f }
    };
    [SerializeField]
    private Lvl3DialogueLine[] noSpaceDialogue =
    {
        new Lvl3DialogueLine { speaker = Lvl3DialogueSpeaker.Sherlock, text = "Nie mam miejsca w ekwipunku.", duration = 2f }
    };

    private bool destroyAfterDialogue;
    protected override Lvl3DialogueLine[] DefaultDialogueLines => pickupDialogue;

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

        // NOWE: rejestrujemy podniesienie, zeby po wczytaniu zapisu
        // ten obiekt nie pojawil sie ponownie na scenie.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);
        else
            Debug.LogError("Int_ZielonaFigurka: brak SaveLoadManager.Instance - podniesienie NIE zostanie zapisane!");

        HideCollectedFigure();
        destroyAfterDialogue = true;
        PlayDialogue(player, pickupDialogue);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (destroyAfterDialogue && lines == pickupDialogue)
            Destroy(gameObject);
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