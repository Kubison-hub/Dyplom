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
        if (VioletGO != null)
        {
            npc = VioletGO.GetComponentInChildren<SmartNPC>();
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
            Debug.LogWarning("VIOLET_NPC was not found for Watson dialog.");
        }

        
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

        if (npc == null)
        {
            Debug.LogWarning("Cannot start Watson-Violet dialog because SmartNPC is missing.");
            return;
        }

        npc.SprawdzIZacznijRozmowe();

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
            Debug.LogWarning("Cannot move Violet because SmartNPC is missing.");
            return;
        }

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
