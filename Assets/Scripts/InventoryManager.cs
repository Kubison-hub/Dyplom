using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class InventoryManager : MonoBehaviour
{
    [Serializable]
    private class InventorySlot
    {
        public bool occupied;
        public ItemType itemType;
        public Sprite icon;
    }

    // Mapowanie typu przedmiotu na ikone.
    // Potrzebne, bo po wczytaniu zapisu obiekty przedmiotow juz nie istnieja,
    // a to one wczesniej dostarczaly sprite'y.
    [Serializable]
    public class ItemIconEntry
    {
        public ItemType itemType;
        public Sprite icon;
    }

    public static InventoryManager Instance;

    [Header("Main Window")]
    public GameObject inventoryWindow;
    public bool isInventoryOpen = false;

    [Header("Legacy Item Icons")]
    public GameObject iconKey;
    public GameObject iconHammer;
    public GameObject iconGlass;
    public GameObject iconPendulum;
    public GameObject iconDoll;
    public GameObject iconWoodBlockLevel1;
    public GameObject iconWoodBlockLevel2;

    [Header("Three Slot UI")]
    [Tooltip("Assign the Image component used by each of the three inventory slots.")]
    [SerializeField] private Image[] slotIconImages = new Image[3];

    [Header("Ikony przedmiotow (dla zapisu/wczytania)")]
    [Tooltip("Przypisz ikone dla kazdego typu przedmiotu. Bez tego wczytany przedmiot bedzie w eq, ale slot pozostanie pusty.")]
    [SerializeField] private ItemIconEntry[] itemIcons;

    [Header("Diagnostyka")]
    [Tooltip("Wlacz, zeby wypisywac w konsoli szczegoly rysowania slotow.")]
    [SerializeField] private bool logujDiagnostyke = true;

    public bool keyInInv = false;

    [Header("Wood Block Selection")]
    [SerializeField] private ItemType selectedWoodBlock;
    [SerializeField] private bool hasSelectedWoodBlock;
    public ItemType SelectedWoodBlock => selectedWoodBlock;

    // Kept public because save/load and gameplay puzzles use this list.
    public List<ItemType> items = new List<ItemType>();

    [Header("Slots")]
    [Min(1)] public int maxSlots = 3;
    [SerializeField] private InventorySlot[] slots = new InventorySlot[3];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("InventoryManager: Instance ustawiony na obiekcie '" + gameObject.name + "'.");
        }
        else
        {
            Debug.LogWarning("InventoryManager: duplikat na '" + gameObject.name + "' - niszcze obiekt.");
            Destroy(gameObject);
        }
    }

    private readonly Dictionary<ItemType, Sprite> itemIconCache = new Dictionary<ItemType, Sprite>();
    private readonly Dictionary<Image, Sprite> emptySlotSprites = new Dictionary<Image, Sprite>();
    private readonly Dictionary<Image, Color> emptySlotColors = new Dictionary<Image, Color>();

    private void Start()
    {
        if (logujDiagnostyke)
            Debug.Log("InventoryManager.Start(): przedmiotow na liscie = " + items.Count +
                      ", slotIconImages = " + OpisSlotUi());

        SynchronizeSlotsWithItems();
    }

    private string OpisSlotUi()
    {
        if (slotIconImages == null)
            return "NULL (tablica nieprzypisana!)";

        int przypisane = 0;
        foreach (Image img in slotIconImages)
        {
            if (img != null) przypisane++;
        }

        return slotIconImages.Length + " pol, przypisanych Image: " + przypisane;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        if (inventoryWindow != null)
            inventoryWindow.SetActive(isInventoryOpen);
    }

    public void ForceCloseInventory()
    {
        if (inventoryWindow != null)
            inventoryWindow.SetActive(false);

        isInventoryOpen = false;
    }

    public bool TryAddItem(ItemType itemType)
    {
        return TryAddItem(itemType, null);
    }

    public bool TryAddItem(ItemType itemType, Sprite itemIcon)
    {
        if (itemIcon != null)
            itemIconCache[itemType] = itemIcon;

        SynchronizeSlotsWithItems();

        if (items.Contains(itemType))
        {
            int existingSlotIndex = FindSlotContaining(itemType);
            if (existingSlotIndex >= 0 && itemIcon != null)
            {
                slots[existingSlotIndex].icon = itemIcon;
                RefreshSlotIcons();
            }

            return true;
        }

        int freeSlotIndex = FindFirstFreeSlot();
        if (freeSlotIndex < 0 || items.Count >= maxSlots)
        {
            Debug.Log("Brak miejsca w ekwipunku!");
            return false;
        }

        items.Add(itemType);
        slots[freeSlotIndex].occupied = true;
        slots[freeSlotIndex].itemType = itemType;
        slots[freeSlotIndex].icon = ResolveItemIcon(itemType, itemIcon);

        if (IsWoodBlock(itemType))
        {
            selectedWoodBlock = itemType;
            hasSelectedWoodBlock = true;
        }

        keyInInv = items.Contains(ItemType.Czerwona);
        RefreshSlotIcons();
        Debug.Log("Ekwipunek: dodano " + itemType + " do slotu " + (freeSlotIndex + 1) + ".");
        return true;
    }

    // Pelne odtworzenie ekwipunku z zapisu.
    // Czysci liste i sloty, wpisuje dane z pliku i przebudowuje UI od zera.
    public void RestoreItems(List<ItemType> savedItems)
    {
        items.Clear();
        EnsureSlotStorage();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].occupied = false;
            slots[i].itemType = default;
            slots[i].icon = null;
        }

        if (savedItems != null)
        {
            foreach (ItemType item in savedItems)
            {
                if (!items.Contains(item) && items.Count < maxSlots)
                    items.Add(item);
            }
        }

        hasSelectedWoodBlock = TryGetAnyWoodBlock(out selectedWoodBlock);
        SynchronizeSlotsWithItems();

        Debug.Log("InventoryManager: przywrocono " + items.Count + " przedmiotow. UI: " + OpisSlotUi());
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
        SynchronizeSlotsWithItems();

        if (!items.Remove(itemType))
            return false;

        int slotIndex = FindSlotContaining(itemType);
        if (slotIndex >= 0)
        {
            slots[slotIndex].occupied = false;
            slots[slotIndex].itemType = default;
            slots[slotIndex].icon = null;
        }

        if (hasSelectedWoodBlock && selectedWoodBlock == itemType)
            hasSelectedWoodBlock = TryGetAnyWoodBlock(out selectedWoodBlock);

        keyInInv = items.Contains(ItemType.Czerwona);
        RefreshSlotIcons();
        return true;
    }

    public bool AddItem(ItemType itemType)
    {
        return TryAddItem(itemType);
    }

    private void SynchronizeSlotsWithItems()
    {
        EnsureSlotStorage();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].occupied && !items.Contains(slots[i].itemType))
            {
                slots[i].occupied = false;
                slots[i].itemType = default;
                slots[i].icon = null;
            }
        }

        foreach (ItemType item in items)
        {
            if (FindSlotContaining(item) >= 0)
                continue;

            int freeSlotIndex = FindFirstFreeSlot();
            if (freeSlotIndex < 0)
                break;

            slots[freeSlotIndex].occupied = true;
            slots[freeSlotIndex].itemType = item;
            slots[freeSlotIndex].icon = ResolveItemIcon(item, null);
        }

        keyInInv = items.Contains(ItemType.Czerwona);
        RefreshSlotIcons();
    }

    private void EnsureSlotStorage()
    {
        int slotCount = Mathf.Max(1, maxSlots);
        if (slots != null && slots.Length == slotCount)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    slots[i] = new InventorySlot();
            }

            return;
        }

        slots = new InventorySlot[slotCount];
        for (int i = 0; i < slots.Length; i++)
            slots[i] = new InventorySlot();
    }

    private int FindFirstFreeSlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].occupied)
                return i;
        }

        return -1;
    }

    private int FindSlotContaining(ItemType itemType)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].occupied && slots[i].itemType == itemType)
                return i;
        }

        return -1;
    }

    private void RefreshSlotIcons()
    {
        bool usesSlotUi = HasSlotUi();

        if (!usesSlotUi)
        {
            if (logujDiagnostyke)
                Debug.LogError("InventoryManager.RefreshSlotIcons(): PRZERWANE - brak przypisanych Image w 'Slot Icon Images'. " +
                               "Sloty nie beda rysowane! Stan: " + OpisSlotUi());
            return;
        }

        for (int i = 0; i < slotIconImages.Length; i++)
        {
            Image slotIcon = slotIconImages[i];
            if (slotIcon == null)
                continue;

            CacheEmptySlotVisual(slotIcon);

            bool hasItem = i < slots.Length && slots[i].occupied;
            Sprite icon = hasItem ? ResolveItemIcon(slots[i].itemType, slots[i].icon) : null;

            // Never disable the assigned Image: it can be the slot's own background.
            // Empty slots restore the artwork that was present before the game started.
            slotIcon.enabled = true;
            slotIcon.sprite = hasItem && icon != null ? icon : emptySlotSprites[slotIcon];
            slotIcon.color = hasItem && icon != null ? Color.white : emptySlotColors[slotIcon];

            if (logujDiagnostyke && hasItem)
            {
                Debug.Log("Slot " + (i + 1) + ": " + slots[i].itemType +
                          " | ikona: " + (icon != null ? icon.name : "BRAK (null)") +
                          " | Image aktywny w hierarchii: " + slotIcon.gameObject.activeInHierarchy +
                          " | alpha: " + slotIcon.color.a);
            }

            if (hasItem && icon == null)
            {
                Debug.LogWarning("InventoryManager: brak ikony dla " + slots[i].itemType +
                                 ". Uzupelnij tablice 'Item Icons' w Inspectorze.");
            }
        }
    }

    private void CacheEmptySlotVisual(Image slotIcon)
    {
        if (!emptySlotSprites.ContainsKey(slotIcon))
            emptySlotSprites.Add(slotIcon, slotIcon.sprite);

        if (!emptySlotColors.ContainsKey(slotIcon))
            emptySlotColors.Add(slotIcon, slotIcon.color);
    }

    private bool HasSlotUi()
    {
        if (slotIconImages == null)
            return false;

        foreach (Image slotIcon in slotIconImages)
        {
            if (slotIcon != null)
                return true;
        }

        return false;
    }

    private void RefreshLegacyIcons()
    {
        SetLegacyIconActive(ItemType.Czerwona, items.Contains(ItemType.Czerwona));
        SetLegacyIconActive(ItemType.Zielona, items.Contains(ItemType.Zielona));
        SetLegacyIconActive(ItemType.Niebieska, items.Contains(ItemType.Niebieska));
        SetLegacyIconActive(ItemType.Wahadlo, items.Contains(ItemType.Wahadlo));
        SetLegacyIconActive(ItemType.Lalka, items.Contains(ItemType.Lalka));
        SetLegacyIconActive(ItemType.WoodBlockLevel1, items.Contains(ItemType.WoodBlockLevel1));
        SetLegacyIconActive(ItemType.WoodBlockLevel2, items.Contains(ItemType.WoodBlockLevel2));
    }

    // Kolejnosc szukania ikony: przekazany sprite -> cache sesji -> tablica itemIcons -> legacy.
    private Sprite ResolveItemIcon(ItemType itemType, Sprite preferredIcon)
    {
        if (preferredIcon != null)
            return preferredIcon;

        if (itemIconCache.TryGetValue(itemType, out Sprite cachedIcon) && cachedIcon != null)
            return cachedIcon;

        if (itemIcons != null)
        {
            foreach (ItemIconEntry entry in itemIcons)
            {
                if (entry != null && entry.itemType == itemType && entry.icon != null)
                    return entry.icon;
            }
        }

        return GetItemIcon(itemType);
    }

    private Sprite GetItemIcon(ItemType itemType)
    {
        GameObject iconObject = GetLegacyIconObject(itemType);
        Image iconImage = iconObject != null ? iconObject.GetComponentInChildren<Image>(true) : null;
        return iconImage != null ? iconImage.sprite : null;
    }

    private void SetAllLegacyIconsActive(bool active)
    {
        SetLegacyIconActive(ItemType.Czerwona, active);
        SetLegacyIconActive(ItemType.Zielona, active);
        SetLegacyIconActive(ItemType.Niebieska, active);
        SetLegacyIconActive(ItemType.Wahadlo, active);
        SetLegacyIconActive(ItemType.Lalka, active);
        SetLegacyIconActive(ItemType.WoodBlockLevel1, active);
        SetLegacyIconActive(ItemType.WoodBlockLevel2, active);
    }

    private void SetLegacyIconActive(ItemType itemType, bool active)
    {
        GameObject iconObject = GetLegacyIconObject(itemType);
        if (iconObject != null)
            iconObject.SetActive(active);
    }

    private GameObject GetLegacyIconObject(ItemType itemType)
    {
        return itemType switch
        {
            ItemType.Czerwona => iconKey,
            ItemType.Zielona => iconHammer,
            ItemType.Niebieska => iconGlass,
            ItemType.Wahadlo => iconPendulum,
            ItemType.Lalka => iconDoll,
            ItemType.WoodBlockLevel1 => iconWoodBlockLevel1,
            ItemType.WoodBlockLevel2 => iconWoodBlockLevel2,
            _ => null
        };
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
}

public enum ItemType
{
    Czerwona,
    Zielona,
    Niebieska,
    Wahadlo,
    Lalka,
    WoodBlockLevel1,
    WoodBlockLevel2,
    SmallBox
}