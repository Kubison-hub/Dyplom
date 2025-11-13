using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Wiêcej ni¿ jedna instancja Ekwipunku!");
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public List<Item> items = new List<Item>();
    public int maxSlots = 3;

    public event Action OnItemAdded;

    public void AddItem(Item item)
    {
        if (items.Count >= maxSlots)
        {
            Debug.Log("Brak miejsca w ekwipunku!");
            return;
        }

        items.Add(item);
        Debug.Log($"Dodano przedmiot: {item.name}");
        OnItemAdded?.Invoke(); // <--- powiadom UI
    }
}
