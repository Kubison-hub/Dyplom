using UnityEngine;

[RequireComponent(typeof(Interactable))]
public abstract class WoodBlockButtonPickupBase : Lvl3InteractionDialogueBase
{
    [Header("Inventory")]
    [SerializeField, TextArea] private string noSpaceText = "Nie mam miejsca, aby to podnieść.";

    [SerializeField] private Sprite inventoryIcon;

    [Header("World Object")]
    [Tooltip("Optional visual child to hide after collection. Leave empty to hide this object's renderers and colliders.")]
    [SerializeField] private GameObject worldVisual;

    [Header("Pickup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] pickupDialogue;

    [Header("Optional Hidden Door Key")]
    [Tooltip("When enabled, collecting this wooden block grants Has Key to the assigned Level 4 Hidden Door.")]
    [SerializeField] private bool setHiddenDoorHasKeyOnPickup;
    [SerializeField] private Int_lv4_HiddenDoor hiddenDoorToUnlock;

    private bool collected;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => pickupDialogue;
    protected abstract InteractionType AssignedInteractionType { get; }
    protected abstract ItemType CollectedItemType { get; }

    protected virtual void Awake()
    {
        SetupInteractable(AssignedInteractionType);
    }

    protected virtual void Reset() => SetupInteractable(AssignedInteractionType);
    protected virtual void OnValidate() => SetupInteractable(AssignedInteractionType);

    public void PerformInteraction(PlayerController player)
    {
        if (collected)
            return;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(CollectedItemType, inventoryIcon))
        {
            ShowNoSpaceText(player);
            return;
        }

        collected = true;
        GetComponent<Interactable>()?.MarkCompleted();

        // Zapis: bez tego przedmiot wraca na swoje miejsce po wczytaniu gry,
        // mimo ze jest juz w ekwipunku.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);
        if (setHiddenDoorHasKeyOnPickup && hiddenDoorToUnlock != null)
            hiddenDoorToUnlock.SetHasKey(true);

        PlayDialogue(player, pickupDialogue);
        HideWorldObject();
    }

    private void HideWorldObject()
    {
        // Do not deactivate this component or one of its parents here: that would stop
        // the persistent dialogue coroutine before it gets a chance to clear the TopText.
        bool worldVisualContainsThisPickup = worldVisual != null && transform.IsChildOf(worldVisual.transform);

        if (worldVisual != null && worldVisual != gameObject && !worldVisualContainsThisPickup)
        {
            worldVisual.SetActive(false);
        }
        else
        {
            foreach (Renderer currentRenderer in GetComponentsInChildren<Renderer>(true))
                currentRenderer.enabled = false;

            foreach (Collider currentCollider in GetComponentsInChildren<Collider>(true))
                currentCollider.enabled = false;
        }

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.isInteractableActive = false;
    }

    private static void ShowNoSpaceText(PlayerController player, string message = "Nie mam miejsca, aby to podnieść.")
    {
        if (player != null && player.playerCharacter == PlayerCharacter.Watson)
            PlayerTopText.Instance?.ShowWatsonTopText(message);
        else
            PlayerTopText.Instance?.ShowTopText(message, string.Empty);
    }

    private void ShowNoSpaceText(PlayerController player)
    {
        ShowNoSpaceText(player, noSpaceText);
    }
}