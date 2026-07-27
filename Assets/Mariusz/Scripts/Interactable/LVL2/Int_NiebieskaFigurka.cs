using UnityEngine;
using UnityEngine.Splines;

public class Int_NiebieskaFigurka : MonoBehaviour
{
    public ItemType itemType;
    public GameObject spline;
    public bool performed = false;

    public void PerformInteraction(PlayerController player)
    {
        // 1. Dodaj do ekwipunku
        if (InventoryManager.Instance != null && !performed)
        {
            performed = true;
            InventoryManager.Instance.AddItem(itemType);
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
