using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Jaki to przedmiot?")]
    public ItemType itemType;

    // --- FAZA 1: "ChodŸ do mnie" ---
    // Tê metodê wywo³uje Interactable po klikniêciu. 
    // Dzia³a tak samo jak w DoorTrigger - ustawia cel dla gracza.
    public void Interact(PlayerController player)
    {
        // Debug.Log("PickupItem: Gracz idzie po przedmiot.");

        // 1. Ustaw cel ruchu gracza na pozycjê tego przedmiotu
        player.targetPosition = transform.position;

        // 2. Przypisz Interactable (który jest na tym samym obiekcie) jako cel interakcji
        // Dziêki temu, jak gracz dojdzie, PlayerController wywo³a PerformInteraction
        player.currentInteractable = GetComponent<Interactable>();

        // 3. W³¹cz silnik ruchu
        player.isWalking = true;
    }

    // --- FAZA 2: "Podnieœ mnie" ---
    // Tê metodê wywo³uje Interactable, gdy gracz ju¿ dojdzie na miejsce (stoppingDistance)
    public void PerformInteraction()
    {
        // 1. Dodaj do ekwipunku
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemType);
            PlayerTopText.Instance.ShotTopText("To mo¿e siê przydaæ");
        }
        else
        {
            Debug.LogError("B£¥D: Brak InventoryManager na scenie!");
        }

        // 2. Usuñ obiekt ze sceny
        Destroy(gameObject);
    }
}