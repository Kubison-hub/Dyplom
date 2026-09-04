using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_SmallBox : Lvl3InteractionDialogueBase
{
    [Header("Sherlock Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] firstDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] secondDialogueLines;

    [Header("Watson Dialogue Lines")]
    [SerializeField] private Lvl3DialogueLine[] watsonFirstDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] watsonSecondDialogueLines;

    [Header("Second Interaction")]
    [Tooltip("Assign the visible box model here. Its renderers are hidden after the second interaction.")]
    [SerializeField] private GameObject boxVisualToHide;
    [SerializeField] private Collider[] collidersToDisable;

    [Header("Pickup Audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupAudioClip;

    [Header("Inventory")]
    [SerializeField] private Sprite inventoryIcon;
    [SerializeField] private ItemType inventoryItemType = ItemType.SmallBox;
    [SerializeField] private string inventoryFullText = "Nie mam miejsca w ekwipunku.";

    private int interactionCount;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();
    private void Awake() => Setup();

    public void PerformInteraction(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (interactionCount == 0)
        {
            interactionCount = 1;
            PlayDialogue(player, isWatson ? watsonFirstDialogueLines : firstDialogueLines);
            return;
        }

        if (interactionCount != 1)
            return;

        if (InventoryManager.Instance == null ||
            !InventoryManager.Instance.TryAddItem(inventoryItemType, inventoryIcon))
        {
            ShowInventoryFullText(player, isWatson);
            return;
        }

        interactionCount = 2;
        CluesLog.Instance?.RegisterBasementEvidence("SmallBox");
        PlayDialogue(player, isWatson ? watsonSecondDialogueLines : secondDialogueLines);

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);

        HideBoxAndCompleteInteraction();
    }

    private void ShowInventoryFullText(PlayerController player, bool isWatson)
    {
        if (isWatson)
            PlayerTopText.Instance?.ShowWatsonTopText(inventoryFullText);
        else
            PlayerTopText.Instance?.ShowTopText(inventoryFullText);
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_lv3_SmallBox);

        if (firstDialogueLines == null || firstDialogueLines.Length == 0)
        {
            firstDialogueLines = new[]
            {
                CreateSherlockLine("Mała drewniana skrzynka.", 2.5f)
            };
        }

        if (secondDialogueLines == null || secondDialogueLines.Length == 0)
        {
            secondDialogueLines = new[]
            {
                CreateSherlockLine("Pudełeczko jest zamknięte na mały kluczyk. Nie będę niszczyć go wytrychem.", 4f)
            };
        }

        if (watsonFirstDialogueLines == null || watsonFirstDialogueLines.Length == 0)
        {
            watsonFirstDialogueLines = new[]
            {
                CreateWatsonLine("Mała drewniana skrzynka. Ciekawe, co skrywa w środku.", 3f)
            };
        }

        if (watsonSecondDialogueLines == null || watsonSecondDialogueLines.Length == 0)
        {
            watsonSecondDialogueLines = new[]
            {
                CreateWatsonLine("Zamknięta na mały kluczyk. Bez niego nic tu nie wskóramy.", 3.5f)
            };
        }
    }

    private void HideBoxAndCompleteInteraction()
    {
        if (boxVisualToHide != null)
        {
            foreach (Renderer boxRenderer in boxVisualToHide.GetComponentsInChildren<Renderer>(true))
                boxRenderer.enabled = false;
        }

        foreach (Collider interactionCollider in collidersToDisable)
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }

        Interactable interactable = GetComponent<Interactable>();
        if (interactable == null)
            return;

        interactable.isInteractableActive = false;
        interactable.allowQuestionFXWhenInactive = false;
        interactable.SetQuestionFXEagleVisionState(false);

        if (interactable.interactiveShader != null)
            interactable.interactiveShader.SetActive(false);
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

    private static Lvl3DialogueLine CreateWatsonLine(string text, float duration)
    {
        return new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Watson,
            text = text,
            duration = duration
        };
    }
}
