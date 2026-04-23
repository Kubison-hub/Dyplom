using DialogueEditor;
using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;

public class Int2_WatsonDialogViolet : MonoBehaviour
{
    public static Int2_WatsonDialogViolet Instance;

    public GameObject WatsonGO;
    public GameObject SherlockGO;
    public GameObject VioletGO;

    public bool performed = false;
    public bool dialogPerformed = false;

    public Transform moveDestination;
    private Vector3 destination;

    public GameObject[] nextInteractions;

    public CinemachineCamera dialogCam;
    private CinemachineSplineDolly splineDolly;

    public Interactable interactable;
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

        WatsonGO = GameObject.Find("Watson");
        SherlockGO = GameObject.Find("Sherlock");

        VioletGO = GameObject.Find("VIOLET_NPC");
        npc = VioletGO.GetComponentInChildren<SmartNPC>();
        navMeshAgent = npc.GetComponent<NavMeshAgent>();

        interactable = GetComponent<Interactable>();

        dialogCam = GetComponentInChildren<CinemachineCamera>();
        splineDolly = dialogCam.GetComponent<CinemachineSplineDolly>();

        transform.parent = VioletGO.transform;
        transform.position = VioletGO.transform.position;
        transform.rotation = VioletGO.transform.localRotation;

        
    }



    public void PerformInteraction(PlayerController player)
    {
        performed = true;
        interactable.questionVFX.Stop();
        interactable.isInteractableActive = false;
        player.currentInteractable = null;

        StartConversation();

        destination = moveDestination.position;
    }


    private void StartConversation()
    {
        //ChangeCamera();

        npc.SprawdzIZacznijRozmowe();

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
        npc.GoToPoint(destination, () => ActiveNextInteractions(true));
    }

    private void ActiveNextInteractions(bool active)
    {
        foreach (var nextInteraction in nextInteractions)
        {
            nextInteraction.SetActive(active);
        }

        this.gameObject.SetActive(false);
    }

}
