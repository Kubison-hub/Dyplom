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

        if (wallAnimator == null && secretWall != null)
            wallAnimator = secretWall.GetComponent<Animator>();
    }

    public void Interact(PlayerController player)
    {
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
            targetPosition += transform.right * 1f; // Przesunięcie w bok
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
            if (wallAnimator == null)
            {
                Debug.LogError($"{name}: DoorSwitcher has no Wall Animator assigned.", this);
                return;
            }

            if (!isOpen)
            {
                wallAnimator.SetTrigger("Open");
                PlayerTopText.Instance.ShowTopText("Brawo Watsonie!, ciekawe jaką tajemnicę skrywa to tajne przejście", "W rzeczy samej Sherlock");
                isOpen = true;
                hidenRoom.isActive = true;
                hidenRoom.discovered = true;
               
            }
            else
            {
                wallAnimator.SetTrigger("Close");
                PlayerTopText.Instance.ShowTopText("Naraazie zamkniemy", "Doskonale");
                isOpen = false;
                
            }
            
            
        }

        else
        {
            if (wallAnimator != null)
                wallAnimator.SetTrigger("TryOpen");
            PlayerTopText.Instance.ShowTopText("Hmm, wygląda na to, że komoda specjalnie blokuje dojście do ściany", "Dokładnie, wydaje się zbyt ciężka aby przepchać ją ręcznie.");
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
