using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    // Nie potrzebujemy ju¿ publicznego pola 'inventory', bo u¿yjemy singletona
    private Inventory inventory;

    public Transform slotsParent; // Obiekt-rodzic, w którym bêd¹ tworzone sloty
    public GameObject slotPrefab; // Prefab slotu

    private List<SlotUI> slots = new List<SlotUI>();

    private void Start()
    {
        // U¿yj singletona, aby automatycznie znaleŸæ ekwipunek
        inventory = Inventory.instance;

        if (inventory == null)
        {
            Debug.LogError("Nie znaleziono instancji Ekwipunku (Inventory.instance)! Upewnij siê, ¿e obiekt z Inventory.cs jest na scenie.");
            return;
        }

        // Subskrybuj eventy
        inventory.OnItemAdded += UpdateUI;
        // Jeœli dodasz usuwanie przedmiotów, odkomentuj to:
        // inventory.OnItemRemoved += UpdateUI;

        CreateSlots();
        UpdateUI();
    }

    private void OnDestroy()
    {
        // Wa¿ne: odsubskrybuj eventy, aby unikn¹æ b³êdów
        if (inventory != null)
        {
            inventory.OnItemAdded -= UpdateUI;
            // inventory.OnItemRemoved -= UpdateUI;
        }
    }

    private void CreateSlots()
    {
        // Wyczyœæ stare sloty, jeœli jakieœ istniej¹ (na wszelki wypadek)
        foreach (Transform child in slotsParent)
        {
            Destroy(child.gameObject);
        }
        slots.Clear();

        // Stwórz sloty na podstawie maxSlots z instancji ekwipunku
        for (int i = 0; i < inventory.maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsParent);
            slotObj.name = "Slot " + i;
            SlotUI slotUI = slotObj.GetComponent<SlotUI>();

            if (slotUI != null)
            {
                slots.Add(slotUI);
            }
            else
            {
                Debug.LogError($"Prefab slotu ({slotPrefab.name}) nie ma komponentu SlotUI! Dodaj go do prefabrykatu.");
            }
        }
    }

    // Ta funkcja jest teraz wywo³ywana przez event
    public void UpdateUI()
    {
        if (slots.Count == 0)
        {
            // Ta wiadomoœæ mo¿e pojawiæ siê na starcie, jeœli CreateSlots() jeszcze siê nie wykona³o
            Debug.LogWarning("Brak slotów UI do zaktualizowania.");
            return;
        }

        // PrzejdŸ przez wszystkie sloty UI
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < inventory.items.Count)
            {
                // Jeœli mamy przedmiot na tej pozycji w ekwipunku, poka¿ go
                slots[i].SetItem(inventory.items[i]);
            }
            else
            {
                // W przeciwnym razie, wyczyœæ slot
                slots[i].ClearSlot();
            }
        }
    }
}