using UnityEngine;

public class Int_ZielonaFigurka : MonoBehaviour
{

    public ItemType itemType;
    public bool performed = false;
   

    public void PerformInteraction(PlayerController player)
    {
        // 1. Dodaj do ekwipunku
        if (InventoryManager.Instance != null && !performed)
        {
            performed = true;
            InventoryManager.Instance.AddItem(itemType);
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
