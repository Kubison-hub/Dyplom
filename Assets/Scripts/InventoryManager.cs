using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

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