using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DoorSwitcher : MonoBehaviour
{
    
    public float interactionPointOffset = 1f;
    [SerializeField] private Transform modelTransform;


    [SerializeField] Door doorToSwitch;

    private Interactable interactable;
    private PlayerInput playerInput;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void Interact(PlayerController player)
    {
        MovePlayerToInteractionPoint(player);
        playerInput = player.GetComponent<PlayerInput>();
    }

    private void MovePlayerToInteractionPoint(PlayerController player)
    {
        Vector3 npcForward = transform.forward;
        Vector3 basePoint = transform.position - npcForward * interactionPointOffset;
        basePoint.y = player.transform.position.y;

        Vector3 targetPosition = basePoint;

        if (!CheckPositionEmpty(basePoint, 0.5f, player.gameObject))
        {
            targetPosition += transform.right * 1f; // Przesuniêcie w bok
        }

        player.currentInteractable = interactable;
        player.targetPosition = targetPosition;
        player.isWalking = true;
    }

    public void PerformInteraction()
    {
        Debug.Log("Perform Interaction " + gameObject.name);

        UseSwitcher();
    }

    private void UseSwitcher()
    {
        doorToSwitch.isLocked = false;
        doorToSwitch.PerformInteraction();
    }



    private bool CheckPositionEmpty(Vector3 position, float radius, GameObject ignoreObject = null)
    {
        Collider[] hits = Physics.OverlapSphere(position, radius);

        foreach (var hit in hits)
        {
            if (hit.isTrigger) continue;

            if (ignoreObject != null && hit.gameObject == ignoreObject) continue;

            if (hit.GetComponent<PlayerController>() != null)
            {
                return false;
            }
        }

        return true;
    }
}