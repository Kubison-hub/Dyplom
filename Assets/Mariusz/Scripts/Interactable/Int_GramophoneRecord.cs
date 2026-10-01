using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Interactable))]
public sealed class Int_GramophoneRecord : Lvl3InteractionDialogueBase
{
    [Header("Character Dialogue Lines")]
    [SerializeField]
    private Lvl3DialogueLine sherlockDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Winylowe płyty, przyjrzyjmy się im z bliska",
        duration = 3f
    };

    [SerializeField]
    private Lvl3DialogueLine watsonDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        text = "Kolekcja płyt winylowych",
        duration = 3f
    };

    [SerializeField]
    private Lvl3DialogueLine watsonPickupDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Watson,
        text = "Duchy przeszłości - myślę, że nada się w sam raz",
        duration = 3f
    };

    [Header("Record Clue Point")]
    [Tooltip("Root containing the clue point visual and its QuestionFX. It starts hidden.")]
    [SerializeField] private GameObject cluePointRoot;
    [SerializeField]
    private Lvl3DialogueLine cluePointDialogue = new Lvl3DialogueLine
    {
        speaker = Lvl3DialogueSpeaker.Sherlock,
        text = "Duchy przeszłości — znamienne.",
        duration = 3f
    };

    [Header("Inventory Pickup")]
    [SerializeField] private ItemType inventoryItemType = ItemType.GramophoneRecord;
    [SerializeField] private Sprite inventoryIcon;
    [Tooltip("Visible record object hidden after a successful pickup. Leave empty to hide this whole interaction.")]
    [SerializeField] private GameObject objectToHideOnCollect;
    [SerializeField, TextArea] private string inventoryFullText = "Nie mam miejsca w ekwipunku.";

    [Header("Pickup Audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupAudioClip;

    private Lvl3DialogueLine[] sherlockLines;
    private Lvl3DialogueLine[] watsonLines;
    private Lvl3DialogueLine[] watsonPickupLines;
    private Lvl3DialogueLine[] cluePointLines;
    private bool cluePointUnlocked;
    private bool cluePointDiscovered;
    private bool collected;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => System.Array.Empty<Lvl3DialogueLine>();

    private void Reset() => Setup();
    private void OnValidate() => Setup();

    private void Awake()
    {
        Setup();
        sherlockLines = new[] { sherlockDialogue };
        watsonLines = new[] { watsonDialogue };
        watsonPickupLines = new[] { watsonPickupDialogue };
        cluePointLines = new[] { cluePointDialogue };

        if (cluePointRoot != null)
            cluePointRoot.SetActive(false);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (collected)
        {
            ClearPlayerInteraction(player);
            return;
        }

        bool isWatson = IsWatson(player);
        if (cluePointDiscovered || isWatson)
        {
            CollectRecord(player);
            return;
        }

        if (!isWatson && cluePointUnlocked && !cluePointDiscovered)
        {
            DiscoverCluePoint(player);
            return;
        }

        UnlockCluePoint();
        PlayDialogue(player, sherlockLines);
    }

    private void UnlockCluePoint()
    {
        if (cluePointUnlocked || cluePointDiscovered)
            return;

        cluePointUnlocked = true;
        if (cluePointRoot != null)
            cluePointRoot.SetActive(true);
    }

    private void DiscoverCluePoint(PlayerController player)
    {
        cluePointDiscovered = true;

        if (cluePointRoot != null)
            cluePointRoot.SetActive(false);

        PlayDialogue(player, cluePointLines);
    }

    private void CollectRecord(PlayerController player)
    {
        bool isWatson = IsWatson(player);
        if (InventoryManager.Instance == null ||
            !InventoryManager.Instance.TryAddItem(inventoryItemType, inventoryIcon))
        {
            if (isWatson)
                PlayerTopText.Instance?.ShowWatsonTopText(inventoryFullText);
            else
                PlayerTopText.Instance?.ShowTopText(inventoryFullText, string.Empty);

            ClearPlayerInteraction(player);
            return;
        }

        collected = true;

        // Zapis: bez tego plyta wraca na swoje miejsce po wczytaniu gry,
        // mimo ze jest juz w ekwipunku.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);

        if (isWatson)
            PlayDialogue(player, watsonPickupLines);

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable.MarkCompleted();
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);

            interactable.interactiveShader = null;
        }

        foreach (Collider interactionCollider in GetComponentsInChildren<Collider>(true))
        {
            if (interactionCollider != null)
                interactionCollider.enabled = false;
        }

        ClearPlayerInteraction(player);

        GameObject target = objectToHideOnCollect != null ? objectToHideOnCollect : gameObject;
        foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (targetRenderer != null)
                targetRenderer.enabled = false;
        }
    }

    private void Setup()
    {
        SetupInteractable(InteractionType.Int_GramophoneRecord);
        GetComponent<Interactable>()?.SetWatsonInteractionAllowed(true);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerB") ||
                SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1);
    }

    private static void ClearPlayerInteraction(PlayerController player)
    {
        if (player == null)
            return;

        player.currentInteractable = null;
        player.currentInteractionPoint = null;
        player.ClearAutoInteractionApproachPoint();
        player.SetWaitingForInteractionReaction(false);
    }
}