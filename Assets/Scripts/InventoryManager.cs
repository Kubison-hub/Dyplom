using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("G³ówne Okno Ekwipunku")]
    public GameObject inventoryWindow; // <--- PRZYPISZ TUTAJ SWÓJ PANEL EKWIPUNKU
    public bool isInventoryOpen = false; // <--- Zmienna œledz¹ca stan (Otwarty/Zamkniêty)

    [Header("Ikony w UI (Canvas)")]
    public GameObject iconKey;
    public GameObject iconHammer;
    public GameObject iconGlass;

    public bool keyInInv = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Na starcie ukrywamy okno ekwipunku
        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(false);
            isInventoryOpen = false;
        }
    }

    private void Update()
    {
        // Opcjonalnie: Otwieranie/Zamykanie na klawisz (np. TAB lub E)
        // Jeœli masz to w innym skrypcie (np. PlayerController), mo¿esz to usun¹æ.
        if (Input.GetKeyDown(KeyCode.Tab)) // <--- Zmieñ klawisz wedle uznania
        {
            ToggleInventory();
        }
    }

    // Funkcja do normalnego otwierania/zamykania (na przycisk)
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;

        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(isInventoryOpen);
        }
    }

    // --- NOWA FUNKCJA DLA PAUZY (O któr¹ prosi³eœ) ---
    public void ForceCloseInventory()
    {
        // 1. Wy³¹czamy okno w hierarchii
        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(false);
        }

        // 2. Resetujemy zmienn¹ stanu
        isInventoryOpen = false;

        Debug.Log("InventoryManager: Wymuszono zamkniêcie ekwipunku przez pauzê.");
    }

    public void AddItem(ItemType itemType)
    {
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

// Tutaj jest definicja ItemType - JEDYNA w ca³ym projekcie
public enum ItemType
{
    Key,
    Hammer,
    MagnifyingGlass
}