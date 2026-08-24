using DialogueEditor;
using System.Collections;
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

    [Header("Face Player Before Dialogue")]
    [SerializeField] private bool facePlayerBeforeDialogue = true;
    [SerializeField, Min(1f)] private float facePlayerTurnSpeed = 360f;
    [SerializeField, Min(0.1f)] private float facePlayerTolerance = 1f;

    private bool dialogueStarting;

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
        if (!dialogueStarting && ConversationManager.Instance != null && !ConversationManager.Instance.IsConversationActive)
        {
            interactable.isInteractableActive = false;
            interactable.AddClue(0, cardPosition);
            interactable.AddClue(1, cardPosition);
            player.currentInteractable = null;
            StartCoroutine(BeginDialogue(player));

        }
        else
        {
            Debug.LogWarning("Cannot start Selma dialog while another conversation is active.");
            player.currentInteractable = null;
        }

       
        

        //intCollider.enabled = false;
        //this.gameObject.SetActive(false);
    }

    private IEnumerator BeginDialogue(PlayerController player)
    {
        dialogueStarting = true;

        if (facePlayerBeforeDialogue && smartNPC != null)
        {
            yield return NpcDialogueFacingUtility.FacePlayer(
                smartNPC.transform,
                player != null ? player.transform : null,
                facePlayerTurnSpeed,
                facePlayerTolerance);
        }

        smartNPC?.SprawdzIZacznijRozmowe();
        dialogueStarting = false;
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
