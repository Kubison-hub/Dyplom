using DialogueEditor;
using Unity.Cinemachine;
using UnityEngine;

public class Int_SelmaDialog : MonoBehaviour
{
    public SmartNPC smartNPC;
    public Interactable interactable;

    public bool performed = false;
    public CinemachineCamera dialogCam;
    //public CinemachineSplineDolly splineDolly;
    public Transform cardPosition;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        //splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

    }

    private void Update()
    {
       
    }

    public void PerformInteraction(PlayerController player)
    {
        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            interactable.isInteractableActive = false;
            interactable.AddClue(0, cardPosition);
            interactable.AddClue(1, cardPosition);
            smartNPC.SprawdzIZacznijRozmowe();
            
            
            player.currentInteractable = null;

        }
        else
        {
            Debug.LogWarning("Cannot start Selma dialog while another conversation is active.");
            player.currentInteractable = null;
        }

       
        

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            //splineDolly.CameraPosition = .8f;
            dialogCam.Priority = 50;
        }
    }


    


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
