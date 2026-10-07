using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

public class lvl2_Int_Book : Lvl3InteractionDialogueBase
{
    private static readonly Lvl3DialogueLine[] DefaultLines =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Znalaz³em ma³y kluczyk.",
            duration = 2f
        }
    };

    private Interactable interactable;
    public GameObject spline1;
    public GameObject spline2;

    public bool performed = false;
    public lvl2_Int_HidenDoorSwitcher switcher;

    [SerializeField] private Renderer intRenderer;
    [SerializeField] private AudioSource audioFX;

    [Header("Inventory")]
    [SerializeField] private Sprite inventoryIcon;

    // Typ przedmiotu jako pole - InventoryManager wykrywa ikony
    // po parze pol (ItemType + Sprite). Bez tego ikona nie wraca
    // po wczytaniu zapisu.
    [SerializeField] private ItemType inventoryItemType = ItemType.Level2BookKey;
    [SerializeField, TextArea] private string inventoryFullText = "Nie mam miejsca w ekwipunku.";

    public bool keyFounded = false;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => DefaultLines;

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
        Debug.Log(interactable.name + ", interaction Performed");

        if (intRenderer != null)
            intRenderer.enabled = false;

        if (switcher != null)
            switcher.canOpen = true;

        interactable.AddClue(0);

        if (audioFX != null)
            audioFX.Play();

        PlayInteractionDialogue(player);

        if (spline1 != null)
            spline1.SetActive(false);

        if (spline2 != null)
            spline2.SetActive(false);
        keyFounded = true;

        interactable.isInteractableActive = false;
        if (player != null)
            player.currentInteractable = null;

        ////intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }




}