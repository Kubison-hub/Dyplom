using UnityEngine;
using UnityEngine.Splines;

public class Int_NiebieskaFigurka : MonoBehaviour
{
    public ItemType itemType;
    public GameObject spline;
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
            PlayerTopText.Instance.ShowTopText("Hmm... Niebieska Figurka");
            spline.SetActive(false);
        }
        else
        {
            //Debug.LogError("B£¥D: Brak InventoryManager na scenie!");
        }

        // 2. Usuñ obiekt ze sceny
        Destroy(gameObject);
    }
}
