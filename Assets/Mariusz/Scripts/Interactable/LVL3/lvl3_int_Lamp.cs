using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_Lamp : MonoBehaviour
{
    private Interactable interactable;

    public ItemType itemType;
    public bool performed = false;

    private void Reset()
    {
        SetupInteractable();
    }

    private void OnValidate()
    {
        SetupInteractable();
    }

    private void Start()
    {
        SetupInteractable();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
        {
            player.currentInteractable = null;
            return;
        }

        performed = true;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemType);
        }
        else
        {
            Debug.LogWarning($"{name}: InventoryManager.Instance is null.");
        }

        if (lvl3_GameProgress.Instance != null)
        {
            lvl3_GameProgress.Instance.lampPickedUp = true;
        }
        else
        {
            Debug.LogWarning($"{name}: lvl3_GameProgress.Instance is null.");
        }

        if (PlayerTopText.Instance != null)
        {
            PlayerTopText.Instance.ShowTopText("Lampa", "Moze sie przydac w ciemnosci.");
        }

        player.currentInteractable = null;
        Destroy(gameObject);
    }

    private void SetupInteractable()
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);

        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_Lamp);
        }
    }
}
