using UnityEngine;
using System.Collections.Generic; // Potrzebne do Listy

public class HighlightManager : MonoBehaviour
{
    public KeyCode highlightKey = KeyCode.V;

    // Dwie oddzielne tablice na obiekty do podœwietlenia
    private PickupableObject[] pickupObjects;
    private InteractableHighlight[] interactableObjects;

    private void Start()
    {
        // 1. ZnajdŸ wszystkie obiekty 'Pickupable' (po typie skryptu)
        pickupObjects = FindObjectsOfType<PickupableObject>();
        Debug.Log($"[HighlightManager] Znaleziono {pickupObjects.Length} obiektów 'PickupableObject'.");


        // 2. ZnajdŸ wszystkie obiekty 'Interactable' (po tagu i skrypcie)
        List<InteractableHighlight> interactableList = new List<InteractableHighlight>();
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("interactable");
        Debug.Log($"[HighlightManager] Znaleziono {taggedObjects.Length} obiektów z tagiem 'interactable'.");

        foreach (GameObject go in taggedObjects)
        {
            // Wa¿ne: SprawdŸ, czy nie jest to ju¿ Pickupable, aby unikn¹æ duplikatów
            if (go.GetComponent<PickupableObject>() != null)
            {
                Debug.Log($"[HighlightManager] Pomijam {go.name} (jest ju¿ 'Pickupable').");
                continue;
            }

            // Pobierz nowy skrypt InteractableHighlight
            InteractableHighlight highlightScript = go.GetComponent<InteractableHighlight>();
            if (highlightScript != null)
            {
                interactableList.Add(highlightScript);
            }
            else
            {
                Debug.LogWarning($"[HighlightManager] Obiekt {go.name} ma tag 'interactable', ale brakuje mu skryptu 'InteractableHighlight'!");
            }
        }

        interactableObjects = interactableList.ToArray(); // Konwertuj listê na tablicê
        Debug.Log($"[HighlightManager] Finalnie dodano {interactableObjects.Length} obiektów 'InteractableHighlight'.");
    }

    private void Update()
    {
        // SprawdŸ klawisz tylko raz
        bool highlightOn = Input.GetKey(highlightKey);

        // Pêtla dla PickupableObjects
        foreach (var obj in pickupObjects)
        {
            if (obj != null)
                obj.Highlight(highlightOn);
        }

        // Pêtla dla InteractableObjects
        foreach (var obj in interactableObjects)
        {
            if (obj != null)
                obj.Highlight(highlightOn); // Wywo³aj tê sam¹ metodê
        }
    }
}