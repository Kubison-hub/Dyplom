using UnityEngine;

public class Int_CzerwonaFigurka : MonoBehaviour
{
    public ItemType itemType;
    public GameObject spline;
    public bool performed = false;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Library Safe")]
    [SerializeField] private Animator safeAnimator;
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
            PlayerTopText.Instance.ShowTopText("Czerowona Figurka");
            if (spline != null)
                spline.SetActive(false);

            if (safeAnimator != null)
                safeAnimator.SetTrigger("Close");

        }
        else
        {
            //Debug.LogError("B£¥D: Brak InventoryManager na scenie!");
        }

        // 2. Usuñ obiekt ze sceny
        Destroy(gameObject);
    }
}
