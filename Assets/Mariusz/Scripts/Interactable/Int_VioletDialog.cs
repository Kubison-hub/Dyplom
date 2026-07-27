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

    public static Int_VioletDialog Instance;
    public bool dialogPerformed = false;
    public GameObject[] nextInteractions;

    public bool isVioletInRoom = true;

    public Transform moveDestination;
    private Vector3 destination;
    private void Start()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        interactable = GetComponent<Interactable>();

        if (dialogCam != null)
        {
            splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();
        }

        if (cardPosition == null)
        {
            Debug.Log("cardPosition is null");
        }

        VioletGO = GameObject.Find("VIOLET_NPC");
        if (VioletGO != null)
        {
            transform.parent = VioletGO.transform;
            transform.position = VioletGO.transform.position;
            transform.rotation = VioletGO.transform.localRotation;
        }
        else
        {
            Debug.LogWarning("VIOLET_NPC was not found for Violet dialog interaction.");
        }

        if (smartNPC == null)
            smartNPC = VioletGO.GetComponent<SmartNPC>();
    }

    

    public void PerformInteraction(PlayerController player)
    {
        if (ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            performed = true;
            interactable.isInteractableActive = false;
            interactable.AddClue(0, cardPosition);
            smartNPC.SprawdzIZacznijRozmowe();
           
            destination = moveDestination.position;
            
            player.currentInteractable = null;

        }
        else
        {
            Debug.LogWarning("Cannot start Violet dialog while another conversation is active.");
            player.currentInteractable = null;
        }




        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            if (splineDolly != null)
            {
                splineDolly.CameraPosition = .8f;
            }

            dialogCam.Priority = 50;
        }
    }

    public void MoveToPosition()
    {
        if (smartNPC == null)
        {
            Debug.LogWarning("Cannot move Violet because SmartNPC is missing.");
            return;
        }

        smartNPC.GoToPoint(destination, () => ActiveNextInteractions(true));
    }

    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }

        this.gameObject.SetActive(false);
    }

    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }

    

}
