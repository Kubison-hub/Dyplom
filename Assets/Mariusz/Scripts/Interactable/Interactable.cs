using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionType interactionType;

    // --- KLIKNIÊCIE ---
    public void TryToInteract(PlayerController player)
    {
        // Drzwi
        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();
            if (door != null) door.Interact(player); // Drzwi same ustawiaj¹ ruch gracza
            else Debug.LogError("DoorTrigger component not found");
        }

        // Pianino
        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();
            if (piano != null) piano.Interact(player);
            else Debug.LogError("Piano component not found");
        }

        // Prze³¹cznik drzwi
        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();
            if (doorSwitcher != null) doorSwitcher.Interact(player);
            else Debug.LogError("DoorSwitcher component not found");
        }

        // --- PICKUP (Zmienione na wzór DoorTrigger) ---
        if (interactionType == InteractionType.Pickup)
        {
            PickupItem pickup = GetComponent<PickupItem>();

            // Teraz to PickupItem ustawia ruch gracza (tak jak Drzwi)
            if (pickup != null) pickup.Interact(player);
            else Debug.LogError("PickupItem component not found");
        }

        //--- TABLE

        if (interactionType == InteractionType.Table)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.Interact(player);
            else Debug.LogError("Table component not found");
        }

        // --- GRAMOPHONE 
        if (interactionType == InteractionType.Gramophone)
        {
            Gramophone gramophone = GetComponent<Gramophone>();
            if (gramophone != null) gramophone.Interact(player);
            else Debug.LogError("Table component not found");
        }

        //Stairs

        if (interactionType == InteractionType.Stairs)
        {
            PlayerTopText.Instance.ShotTopText("Madam Selma pilnuje schodów na górê, interesuj¹ce", "Na razie nie przejdziemy, rozejrzyjmy siê po domu.");
        }

        //SecretWall
        if (interactionType == InteractionType.SecretWall)
        {
            PlayerTopText.Instance.ShotTopText("Ta œciana odstaje od reszy", "Ewidentnie");
        }
    }

    // --- DOTARCIE DO CELU ---
    public void PerformInteraction(PlayerController player)
    {
        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();
            if (door != null) door.PerformInteraction();
        }

        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();
            if (piano != null) piano.PerformInteraction(player);
        }

        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();
            if (doorSwitcher != null) doorSwitcher.PerformInteraction();
        }

        // --- PICKUP ---
        if (interactionType == InteractionType.Pickup)
        {
            PickupItem item = GetComponent<PickupItem>();
            if (item != null) item.PerformInteraction();
        }

        if (interactionType == InteractionType.Table)
        {
            Table table = GetComponent<Table>();
            if (table != null) table.PerformInteraction();
            
        }

        if (interactionType == InteractionType.Gramophone)
        {
            Gramophone gramophone = GetComponent<Gramophone>();
            if (gramophone != null) gramophone.PerformInteraction();
            
        }
    }
}

public enum InteractionType
{
    None,
    Door,
    Dialog,
    Piano,
    DoorSwitcher,
    Pickup,
    Table,
    Gramophone,
    Stairs,
    SecretWall
}