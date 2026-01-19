using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // <--- WA¯NE: To pozwala u¿ywaæ Listy

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("G³ówne Okno (dla Pauzy)")]
    public GameObject inventoryWindow; // Przypisz tu panel ca³ego ekwipunku
    public bool isInventoryOpen = false;

    [Header("Ikony w UI (Canvas)")]
    public GameObject iconKey;
    public GameObject iconHammer;
    public GameObject iconGlass;

    // Zmienne pomocnicze
    public bool keyInInv = false;

    // --- NOWOŒÆ: Lista przedmiotów (Dla Systemu Zapisu) ---
    // SaveLoadManager bierze tê listê i zapisuje do pliku.
    public List<ItemType> items = new List<ItemType>();
    // ------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Na starcie ukrywamy okno, jeœli jest przypisane
        if (inventoryWindow != null) inventoryWindow.SetActive(false);
    }

    private void Update()
    {
        // Otwieranie na TAB (mo¿esz zmieniæ lub usun¹æ, jeœli masz to w PlayerController)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    // --- Obs³uga otwierania/zamykania ---
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

    // --- G³ówna funkcja dodawania przedmiotów ---
    public void AddItem(ItemType itemType)
    {
        // 1. DODAJEMY DO LISTY (To jest kluczowe dla zapisu gry!)
        items.Add(itemType);

        // 2. W³¹czamy odpowiedni¹ ikonkê (Twoja logika wizualna)
        switch (itemType)
        {
            case ItemType.Key:
                if (iconKey) iconKey.SetActive(true);
                keyInInv = true;
                Debug.Log("Ekwipunek: Dodano Klucz");
                break;

            case ItemType.Hammer:
                if (iconHammer) iconHammer.SetActive(true);
                Debug.Log("Ekwipunek: Dodano M³otek");
                break;

            case ItemType.MagnifyingGlass:
                if (iconGlass) iconGlass.SetActive(true);
                Debug.Log("Ekwipunek: Dodano Lupê");
                break;
        }
    }
}

// Enum pozostaje bez zmian
public enum ItemType
{
    Key,
    Hammer,
    MagnifyingGlass
}