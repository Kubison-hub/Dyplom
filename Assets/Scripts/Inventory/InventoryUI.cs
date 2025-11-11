using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public Transform slotsParent;
    public GameObject slotPrefab;

    private List<SlotUI> slots = new List<SlotUI>();

    private void Start()
    {
        inventory.OnItemAdded += UpdateUI;
        CreateSlots();
        UpdateUI();
    }

    private void CreateSlots()
    {
        for (int i = 0; i < inventory.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            slots.Add(slotObj.GetComponent<SlotUI>());
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < inventory.items.Count)
                slots[i].SetItem(inventory.items[i]);
            else
                slots[i].ClearSlot();
        }
    }
}
