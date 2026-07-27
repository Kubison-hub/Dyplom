using DialogueEditor;
using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class Int2_WatsonDialogViolet_2 : MonoBehaviour
{
    public static Int2_WatsonDialogViolet_2 Instance;

    public GameObject VioletGO;
    public GameObject SelmaGO;

    public Int_StairsUp stairsUp;

    public bool performed = false;
    public bool dialogPerformed = false;

    public Transform moveDestination;
    private Vector3 destination;

    public GameObject[] nextInteractions;

    public CinemachineCamera dialogCam;
    private CinemachineSplineDolly splineDolly;
    
    private Interactable interactable;
    private NavMeshAgent navMeshAgent;
    [HideInInspector] public SmartNPC npc;


    void Start()
    {

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        VioletGO = GameObject.Find("VIOLET_NPC");
        SelmaGO = GameObject.Find("SELMA_NPC");


        if (SelmaGO != null)
        {
            npc = SelmaGO.GetComponentInChildren<SmartNPC>();
        }

        if (npc != null)
        {
            navMeshAgent = npc.GetComponent<NavMeshAgent>();
        }

        interactable = GetComponent<Interactable>();

        dialogCam = GetComponentInChildren<CinemachineCamera>();
        if (dialogCam != null)
        {
            splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();
        }

        if (VioletGO != null)
        {
            transform.parent = VioletGO.transform;
            transform.position = VioletGO.transform.position;
            transform.rotation = VioletGO.transform.localRotation;
        }
        else
        {
            Debug.LogWarning("VIOLET_NPC was not found for Watson dialog step 2.");
        }


        if (moveDestination != null)
        {
            destination = moveDestination.position;
        }
    }



    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        interactable.questionVFX.Stop();
        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        StartConversation();

    }


    private void StartConversation()
    {
        //ChangeCamera();

        //npc.SprawdzIZacznijRozmowe();

        MoveToPosition();

    }

    public void ChangeCamera()
    {
        if (dialogCam != null)
        {
            if (splineDolly != null)
            {
                splineDolly.CameraPosition = .6f;
            }

            dialogCam.Priority = 50;
        }
    }

    public void MoveToPosition()
    {
        if (npc == null)
        {
            Debug.LogWarning("Cannot move Selma because SmartNPC is missing.");
            return;
        }

        npc.GoToPoint(destination, () => ActivateStairs());
    }

    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }

        this.gameObject.SetActive(false);
    }

    private void ActivateStairs()
    {
        if (stairsUp != null)
        {
            stairsUp.canGoUpStairs = true;
        }
        else
        {
            Debug.LogWarning("Cannot activate stairs because Int_StairsUp reference is missing.");
        }
    }

}
