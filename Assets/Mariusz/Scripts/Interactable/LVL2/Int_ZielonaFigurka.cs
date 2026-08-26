using UnityEngine;

public class Int_ZielonaFigurka : MonoBehaviour
{

    public ItemType itemType;
    public bool performed = false;
    [SerializeField] private Sprite inventoryIcon;
   

    public void PerformInteraction(PlayerController player)
    {
        // 1. Dodaj do ekwipunku
        if (InventoryManager.Instance != null && !performed)
        {
            if (!InventoryManager.Instance.TryAddItem(itemType, inventoryIcon))
            {
                PlayerTopText.Instance?.ShowTopText("Nie mam miejsca w ekwipunku.");
                return;
            }

            performed = true;
            PlayerTopText.Instance.ShowTopText("Ciekawe. Zielona Figurka");
            
        }
        else
        {
            //Debug.LogError("B£¥D: Brak InventoryManager na scenie!");
        }

        // 2. Usuñ obiekt ze sceny
        Destroy(gameObject);
    }
}
