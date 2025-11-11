using UnityEngine;
using UnityEngine.UI;

public class SlotUI : MonoBehaviour
{
    public Image icon;

    public void SetItem(Item item)
    {
        if (item != null && item.icon != null)
        {
            Debug.Log("Ustawiam ikonê: " + item.icon.name);
            icon.sprite = item.icon;
            icon.enabled = true;
        }
        else
        {
            Debug.Log("Brak ikony dla: " + (item != null ? item.itemName : "null"));
            ClearSlot();
        }
    }


    public void ClearSlot()
    {
        icon.sprite = null;
        icon.enabled = false;
    }


}
