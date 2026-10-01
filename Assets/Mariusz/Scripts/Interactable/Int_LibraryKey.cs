using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibraryKey : Lvl3InteractionDialogueBase
{
    private Interactable interactable;

    [SerializeField] private Int_LibrarySafe librarySafe;
    [SerializeField] private Renderer keyRenderer;
    [SerializeField] private AudioSource pickupAudio;

    [Header("Inventory")]
    [SerializeField] private ItemType inventoryItemType = ItemType.LibraryKey;
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField, TextArea] private string inventoryFullText = "Nie mam miejsca w ekwipunku.";

    [Header("Key Pickup Dialogue")]
    [SerializeField]
    private Lvl3DialogueLine[] keyPickupDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "I jest kluczyk, jakie to proste...",
            duration = 3f
        }
    };

    public bool performed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => keyPickupDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        if (InventoryManager.Instance == null ||
            !InventoryManager.Instance.TryAddItem(inventoryItemType, inventoryIcon))
        {
            if (player != null && player.playerCharacter == PlayerCharacter.Watson)
                PlayerTopText.Instance?.ShowWatsonTopText(inventoryFullText);
            else
                PlayerTopText.Instance?.ShowTopText(inventoryFullText, string.Empty);

            if (player != null)
                player.currentInteractable = null;
            return;
        }

        // Zapis: bez tego przedmiot wraca na swoje miejsce po wczytaniu gry,
        // mimo ze jest juz w ekwipunku.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);

        performed = true;
        interactable?.MarkCompleted();

        if (keyRenderer != null)
            keyRenderer.enabled = false;

        if (pickupAudio != null)
            pickupAudio.Play();

        if (librarySafe != null)
            librarySafe.SetKeyFound(true);

        MagnifierGlassController.ForceCloseLoupeUntilKeyReleased();

        PlayDialogue(player, keyPickupDialogue);

        interactable.isInteractableActive = false;
        interactable.interactiveShader = null;

        if (player != null)
            player.currentInteractable = null;
    }
}