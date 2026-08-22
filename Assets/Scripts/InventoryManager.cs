using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // <--- WAï¿½NE: To pozwala uï¿½ywaï¿½ Listy

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Gï¿½ï¿½wne Okno (dla Pauzy)")]
    public GameObject inventoryWindow; // Przypisz tu panel caï¿½ego ekwipunku
    public bool isInventoryOpen = false;

    [Header("Ikony w UI (Canvas)")]
    public GameObject iconKey;
    public GameObject iconHammer;
    public GameObject iconGlass;

    public GameObject iconPendulum;
    public GameObject iconDoll;
    // Zmienne pomocnicze
    public bool keyInInv = false;

    // --- NOWOï¿½ï¿½: Lista przedmiotï¿½w (Dla Systemu Zapisu) ---
    // SaveLoadManager bierze tï¿½ listï¿½ i zapisuje do pliku.
    public List<ItemType> items = new List<ItemType>();
    [Header("Slots")]
    [Min(1)] public int maxSlots = 3;
    // ------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Na starcie ukrywamy okno, jeï¿½li jest przypisane
        if (inventoryWindow != null) inventoryWindow.SetActive(false);
    }

    private void Update()
    {
        // Otwieranie na TAB (moï¿½esz zmieniï¿½ lub usunï¿½ï¿½, jeï¿½li masz to w PlayerController)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    // --- Obsï¿½uga otwierania/zamykania ---
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryWindow != null) inventoryWindow.SetActive(isInventoryOpen);
    }

    // Funkcja dla Menu Pauzy
    public void ForceCloseInventory()
    {
        if (inventoryWindow != null) inventoryWindow.SetActive(false);
        isInventoryOpen = false;
    }

    // --- Gï¿½ï¿½wna funkcja dodawania przedmiotï¿½w ---
    public bool TryAddItem(ItemType itemType)
    {
        if (items.Contains(itemType))
            return true;

        if (items.Count >= maxSlots)
        {
            Debug.Log("Brak miejsca w ekwipunku!");
            return false;
        }

        // List order is the slot order: first free position is slot 1, then 2, then 3.
        items.Add(itemType);

        switch (itemType)
        {
            case ItemType.Czerwona:
                if (iconKey) iconKey.SetActive(true);
                keyInInv = true;
                Debug.Log("Ekwipunek: Czerwona Figurka");
                break;

            case ItemType.Zielona:
                if (iconHammer) iconHammer.SetActive(true);
                Debug.Log("Ekwipunek: Zielona Figurka");
                break;

            case ItemType.Niebieska:
                if (iconGlass) iconGlass.SetActive(true);
                Debug.Log("Ekwipunek: Niebieska Figurka");
                break;

            case ItemType.Wahadlo:
                if (iconPendulum) iconPendulum.SetActive(true);
                Debug.Log("Ekwipunek: Wahad³o od zegara");
                break;

            case ItemType.Lalka:
                if (iconDoll) iconDoll.SetActive(true);
                Debug.Log("Ekwipunek: Lalka");
                break;
        }

        return true;
    }

    public void AddItem(ItemType itemType)
    {
        TryAddItem(itemType);
    }
}

// Enum pozostaje bez zmian
public enum ItemType
{
    Czerwona,
    Zielona,
    Niebieska,
    Wahadlo,
    Lalka
}