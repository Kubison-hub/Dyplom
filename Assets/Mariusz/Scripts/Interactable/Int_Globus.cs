using Unity.Cinemachine;
using UnityEngine;

public class Int_Globus : MonoBehaviour
{

    private Interactable interactable;

    public bool performed = false;
  
    public Transform cardPosition;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (cardPosition == null)
        {
            cardPosition = transform;
        }

    }

    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        Debug.Log(interactable.name + ", interaction Performed");

        interactable.AddClue(0, cardPosition);

        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
