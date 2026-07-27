using Unity.Cinemachine;
using UnityEngine;

public class Interaction : MonoBehaviour
{
    [SerializeField] private Int_EdithExamBody edith;
    private Interactable interactable;
    
    public bool performed = false;
    public CinemachineCamera dialogCam;

    public Transform cardPosition;

    private void Start()
    {
        interactable = GetComponent<Interactable>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
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
        this.gameObject.SetActive(false);
    }


    public void ChangeToDialogCamera()
    {
        if (dialogCam != null)
        {
            dialogCam.Priority = 50;
        }
    }

   
    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
