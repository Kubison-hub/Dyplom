using DialogueEditor;
using Unity.Cinemachine;
using UnityEngine;

public class Int_VioletDialog : MonoBehaviour
{
    public SmartNPC smartNPC;
    public Interactable interactable;
    public GameObject VioletGO;
    public bool performed = false;
    public CinemachineCamera dialogCam;
    public CinemachineSplineDolly splineDolly;
    public Transform cardPosition;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }
        VioletGO = GameObject.Find("VIOLET_NPC");
        transform.parent = VioletGO.transform;
        transform.position = VioletGO.transform.position;
        transform.rotation = VioletGO.transform.localRotation;
    }

    

    public void PerformInteraction(PlayerController player)
    {
        interactable.isInteractableActive = false;


        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            interactable.AddClue(0, cardPosition);
            smartNPC.SprawdzIZacznijRozmowe();


            player.currentInteractable = null;

        }
        else
        {
            Debug.LogError("ERROR");
        }




        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            splineDolly.CameraPosition = .8f;
            dialogCam.Priority = 50;
        }
    }





    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }


}
