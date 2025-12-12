using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DoorSwitcher : MonoBehaviour
{
    
    public float interactionPointOffset = 1f;
    [SerializeField] private Transform modelTransform;

    public Transform secretWall;
    public Animator wallAnimator;
    public bool canOpen;

    private Interactable interactable;
    private PlayerInput playerInput;

    public Room hidenRoom;

    public bool isOpen;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        wallAnimator = secretWall.GetComponent<Animator>();
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
        if (canOpen)
        {
            if (!isOpen)
            {
                wallAnimator.SetTrigger("Open");
                PlayerTopText.Instance.ShotTopText("Brawo Watsonie!, ciekawe jak¹ tajemnicê skrywa to tajne przejœcie", "W rzeczy samej Sherlock");
                isOpen = true;
                hidenRoom.isActive = true;
                hidenRoom.discovered = true;
               
            }
            else
            {
                wallAnimator.SetTrigger("Close");
                PlayerTopText.Instance.ShotTopText("Naraazie zamkniemy", "Doskonale");
                isOpen = false;
                
            }
            
            
        }

        else
        {
            wallAnimator.SetTrigger("TryOpen");
            PlayerTopText.Instance.ShotTopText("Hmm, wygl¹da na to, ¿e komoda specjalnie blokuje dojœcie do œciany", "Dok³adnie, wydaje siê zbyt ciê¿ka aby przepchaæ j¹ rêcznie.");
        }
            
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