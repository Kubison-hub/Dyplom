using System;
using Unity.Cinemachine;
using UnityEngine;

public class EmptyWall : MonoBehaviour
{

    private Interactable interactable;

    public bool performed = false;
    public Transform cardPosition;

    

    public GameObject[] nextInteractions;

    private Collider intCollider;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        intCollider = GetComponent<Collider>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        ActiveNextInteractions(false);



    }

    public void PerformInteraction(PlayerController player)
    {
        interactable.AddClue(0, cardPosition);
        performed = true;
        Debug.Log("Interaction Performed");

        ActiveNextInteractions(true);

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        intCollider.enabled = false;
    }


    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }
    }

    


}
