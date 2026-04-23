using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;


public class Int_EdithExamBody : MonoBehaviour
{
    private bool interactionPerforming = false;
    private Interactable interactable;

    public bool performed = false;
    public CinemachineCamera interactionCamera;
    private CinemachineSplineDolly splineDolly; 

    public GameObject watsonGO;
    public GameObject sherlockGO;
    private NavMeshAgent watsonNavMesh;
    private Animator watsonAnimator;
    private Animator sherlockAnimator;

    public Transform watsonPosition;
    private Collider intCollider;


    //ClueCards Positions
    public Transform cp0;
    public Transform cp1;
    public Transform cp2;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        watsonNavMesh = watsonGO.GetComponent<NavMeshAgent>();
        watsonAnimator = watsonGO.GetComponent <Animator>();
        sherlockAnimator = sherlockGO.GetComponent<Animator>();

        splineDolly = interactionCamera.GetComponent<CinemachineSplineDolly>();

        intCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (interactionPerforming) { CheckWatsonArrival(); }
        
    }

    public void PerformInteraction(PlayerController player)
    {
        
        ChangeCamera();
        PerformWatsonAction();
        interactable.interactiveShader = null;
        interactionPerforming = true;
        sherlockAnimator.SetBool("IsThinking", true);
        StartCoroutine(EdithExamination());
        interactable.isInteractableActive = false;
        player.currentInteractable = null;
        intCollider.isTrigger = false;

        interactable.interactionVFX.Stop();
        interactable.questionVFX.Stop();

    }

    private IEnumerator EdithExamination()
    {

        yield return new WaitForSeconds(2);
        AddClue(0, cp0);
        yield return new WaitForSeconds(6);
        
        AddClue(1, cp1);
        yield return new WaitForSeconds(5);
        
        AddClue(2, cp2);
        yield return new WaitForSeconds(5);
        interactionPerforming = false;
        interactionCamera.Priority = 0;
        sherlockAnimator.SetBool("IsThinking", false);
        watsonAnimator.SetBool("AnimEdithExam", false);

        interactable.interactionVFX.Stop();
        interactable.questionVFX.Stop();
        watsonAnimator.SetBool("AnimEdithExam", false);
    }


    public void ChangeCamera()
    {
        if (interactionCamera != null)
        {
            splineDolly.CameraPosition = 0.5f;
            interactionCamera.Priority = 50;

            
        }
    }


    public void AddClue(int number, Transform cardPosition)
    {
        interactable.AddClue(number, cardPosition);
    }

    private void PerformWatsonAction()
    {
        watsonNavMesh.SetDestination(watsonPosition.position);
    }

    private void CheckWatsonArrival()
    {
       
        if (watsonNavMesh.pathPending)
            return;

        if (watsonNavMesh.remainingDistance > watsonNavMesh.stoppingDistance + 0.05f)
            return;

        if (watsonNavMesh.hasPath && watsonNavMesh.velocity.sqrMagnitude > 0.01f)
            return;

        StartCoroutine(WatsonRotateAndPerform());

    }
    private IEnumerator WatsonRotateAndPerform()
    {


        watsonNavMesh.updateRotation = false;

        Quaternion targetRotation = watsonPosition.rotation;

        while (Quaternion.Angle(watsonGO.transform.rotation, targetRotation) > 1f)
        {
            watsonGO.transform.rotation = Quaternion.RotateTowards(
                watsonGO.transform.rotation,
                targetRotation,
                10 * Time.deltaTime
            );

            yield return null;
        }

        watsonGO.transform.rotation = targetRotation;
        watsonNavMesh.updateRotation = true;

        watsonAnimator.SetBool("AnimEdithExam", true);

    }

}
