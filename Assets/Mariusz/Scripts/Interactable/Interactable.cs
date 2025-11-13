using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Te Interactable wywo³uje metody z klas obiektów do interakcji, metody wywo³ywane s¹ z PlayerControllera.
/// Ka¿dy interaktywny obiekt musi mieæ Tag Interactable, skypt Interaktable i odpowiadaj¹cy Enumowi skrypt interakcji.
/// </summary>

public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractionType interactionType;
    

    public void TryToInteract(PlayerController player)
    {
        //Debug.Log("Interactin with " + gameObject.name);

        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();

            if (door != null ) door.Interact(player);
            else Debug.LogError("Door not found");
        }

        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();

            if (piano != null) piano.Interact(player);
            else Debug.LogError("PianoScript not found");

        }

        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();

            if (doorSwitcher != null) doorSwitcher.Interact(player);
            else Debug.LogError("doorSwitcher not found");
        }
    }


    public void PerformInteraction(PlayerController player)
    {
        if (interactionType == InteractionType.Door)
        {
            DoorTrigger door = GetComponent<DoorTrigger>();

            if (door != null) door.PerformInteraction();
            else Debug.LogError("Door not Found");
        }

        if (interactionType == InteractionType.Piano)
        {
            Piano piano = GetComponent<Piano>();

            if (piano != null) piano.PerformInteraction(player);
            else Debug.LogError("PianoScript not found");

        }

        if (interactionType == InteractionType.DoorSwitcher)
        {
            DoorSwitcher doorSwitcher = GetComponent<DoorSwitcher>();

            if (doorSwitcher != null) doorSwitcher.PerformInteraction();
            else Debug.LogError("doorSwitcher not found");
        }
    }

}

public enum InteractionType
{
    None,
    Door,
    Dialog,
    Piano,
    DoorSwitcher
}

