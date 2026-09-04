using UnityEngine;

using Debug = UnityEngine.Debug;

public class PickupItem : MonoBehaviour
{
    [Header("Jaki to przedmiot?")]
    public ItemType itemType;
    [SerializeField] private Sprite inventoryIcon;

    // --- FAZA 1: "Chodz do mnie" ---
    public void Interact(PlayerController player)
    {
        player.targetPosition = transform.position;
        player.currentInteractable = GetComponent<Interactable>();
        player.isWalking = true;
    }

    // --- FAZA 2: "Podnies mnie" ---
    public void PerformInteraction()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("PickupItem: InventoryManager is missing.");
            return;
        }

        if (!InventoryManager.Instance.TryAddItem(itemType, inventoryIcon))
        {
            PlayerTopText.Instance?.ShowTopText("Nie mam miejsca w ekwipunku.");
            return;
        }

        // NOWE: rejestrujemy podniesienie dla systemu zapisu.
        if (SaveLoadManager.Instance != null)
            SaveLoadManager.Instance.MarkCollected(gameObject);
        else
            Debug.LogError("PickupItem: brak SaveLoadManager.Instance - podniesienie NIE zostanie zapisane!");

        PlayerTopText.Instance?.ShowTopText("To moze sie przydac.");
        Destroy(gameObject);
    }
}