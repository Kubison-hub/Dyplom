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


        npc = SelmaGO.GetComponentInChildren<SmartNPC>();
        navMeshAgent = npc.GetComponent<NavMeshAgent>();

        interactable = GetComponent<Interactable>();

        dialogCam = GetComponentInChildren<CinemachineCamera>();
        splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        transform.parent = VioletGO.transform;
        transform.position = VioletGO.transform.position;
        transform.rotation = VioletGO.transform.localRotation;


        destination = moveDestination.position;
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
            splineDolly.CameraPosition = .6f;
            dialogCam.Priority = 50;
        }
    }

    public void MoveToPosition()
    {
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
        stairsUp.canGoUpStairs = true;
    }

}
