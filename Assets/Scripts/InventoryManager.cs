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
    public GameObject iconWoodBlockLevel1;
    public GameObject iconWoodBlockLevel2;
    // Zmienne pomocnicze
    public bool keyInInv = false;

    [Header("Wood Block Selection")]
    [SerializeField] private ItemType selectedWoodBlock;
    [SerializeField] private bool hasSelectedWoodBlock;
    public ItemType SelectedWoodBlock => selectedWoodBlock;

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

            case ItemType.WoodBlockLevel1:
                if (iconWoodBlockLevel1) iconWoodBlockLevel1.SetActive(true);
                Debug.Log("Ekwipunek: Drewniany blok - poziom 1");
                break;

            case ItemType.WoodBlockLevel2:
                if (iconWoodBlockLevel2) iconWoodBlockLevel2.SetActive(true);
                Debug.Log("Ekwipunek: Drewniany blok - poziom 2");
                break;
        }


        if (IsWoodBlock(itemType))
        {
            selectedWoodBlock = itemType;
            hasSelectedWoodBlock = true;
        }
        return true;
    }

    public bool TrySelectWoodBlock(ItemType itemType)
    {
        if (!IsWoodBlock(itemType) || !items.Contains(itemType))
            return false;

        selectedWoodBlock = itemType;
        hasSelectedWoodBlock = true;
        return true;
    }

    public bool TryTakeSelectedWoodBlock(out ItemType itemType)
    {
        itemType = default;
        if (!TryGetSelectedWoodBlock(out itemType))
            return false;

        return TryRemoveItem(itemType);
    }

    public bool TryRemoveItem(ItemType itemType)
    {
        if (!items.Remove(itemType))
            return false;

        if (itemType == ItemType.WoodBlockLevel1 && iconWoodBlockLevel1 != null)
            iconWoodBlockLevel1.SetActive(false);
        else if (itemType == ItemType.WoodBlockLevel2 && iconWoodBlockLevel2 != null)
            iconWoodBlockLevel2.SetActive(false);

        if (hasSelectedWoodBlock && selectedWoodBlock == itemType)
            hasSelectedWoodBlock = TryGetAnyWoodBlock(out selectedWoodBlock);

        return true;
    }

    private bool TryGetSelectedWoodBlock(out ItemType itemType)
    {
        if (hasSelectedWoodBlock && items.Contains(selectedWoodBlock))
        {
            itemType = selectedWoodBlock;
            return true;
        }

        return TryGetAnyWoodBlock(out itemType);
    }

    private bool TryGetAnyWoodBlock(out ItemType itemType)
    {
        if (items.Contains(ItemType.WoodBlockLevel1))
        {
            itemType = ItemType.WoodBlockLevel1;
            return true;
        }

        if (items.Contains(ItemType.WoodBlockLevel2))
        {
            itemType = ItemType.WoodBlockLevel2;
            return true;
        }

        itemType = default;
        return false;
    }

    private static bool IsWoodBlock(ItemType itemType)
    {
        return itemType == ItemType.WoodBlockLevel1 || itemType == ItemType.WoodBlockLevel2;
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
    Lalka,
    WoodBlockLevel1,
    WoodBlockLevel2
}
