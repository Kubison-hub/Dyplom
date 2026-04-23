using System;
using System.Collections;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Int2_WatsonScan : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;
    public bool canInteract = false;
    public bool isOnDistraction = false;

    public GameObject npcGO;
    public GameObject watsonGO;
    public GameObject sherlockGO;
    public NavMeshAgent sherlockNavMesh;
    public Transform SherlockResetTransform;
    private Vector3 SherlockResetPosition;
    private Quaternion SherlockResetRotation;

    public Collider sherlockDetection;
    private Vector3 firstNpcPosition;
    private Quaternion firstNpcRotation;

    [Space]
    public NavMeshAgent navMeshAgent;
    public float stopDistance = 0.1f;

    private Coroutine movementCoroutine;
    private Coroutine SHmovementCoroutine;


    private Vector3 watsonDistractionPosition;
    public Int_StairsUp stairsUp;

    [SerializeField] private PlayerInput sherlockPlayerInput;
    private InputAction moveAction;


    private void Start()
    {
        interactable = GetComponent<Interactable>();
        navMeshAgent = GetComponentInParent<NavMeshAgent>();

        npcGO = transform.parent.gameObject;
        watsonGO = GameObject.Find("Watson");
        sherlockGO = GameObject.Find("Sherlock");
        sherlockNavMesh = sherlockGO.GetComponent<NavMeshAgent>();

        firstNpcPosition = npcGO.transform.position;
        firstNpcRotation = npcGO.transform.rotation;

        SherlockResetPosition = SherlockResetTransform.position;
        SherlockResetRotation = SherlockResetTransform.localRotation;

        if (stairsUp == null)
            Debug.LogError("stairsUp is null");

        moveAction = sherlockPlayerInput.actions["LeftClick"];
    }

    private void Update()
    {
        if (!isOnDistraction)
            return;

        float distance = Vector3.Distance(watsonGO.transform.position, watsonDistractionPosition);

        if (distance > 0.2f)
        {
            isOnDistraction = false;
            ReturnToFirstPosition();
        }
    }

    public void GoToPoint(Vector3 destination, Action onReachedDestination = null)
    {
        if (navMeshAgent == null)
        {
            Debug.LogError("navMeshAgent == null");
            return;
        }

        if (movementCoroutine != null)
            StopCoroutine(movementCoroutine);

        navMeshAgent.SetDestination(destination);
        movementCoroutine = StartCoroutine(WaitForArrival(onReachedDestination, navMeshAgent));
    }

    private IEnumerator WaitForArrival(Action onReached, NavMeshAgent agent)
    {
        yield return new WaitUntil(() => agent.pathPending == false);

        while (agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
        {
            onReached?.Invoke();
            movementCoroutine = null;
        }
    }

    private IEnumerator WaitForSherlockArrival(Action onReached)
    {
        yield return new WaitUntil(() => !sherlockNavMesh.pathPending);

        while (sherlockNavMesh.remainingDistance > sherlockNavMesh.stoppingDistance)
        {
            yield return null;
        }

        while (sherlockNavMesh.velocity.sqrMagnitude > 0.01f)
        {
            yield return null;
        }

        onReached?.Invoke();
        SHmovementCoroutine = null;
    }

    private void WatsonScanInteraction()
    {
        Rotate();
        
        isOnDistraction = true;
        stairsUp.canGoUpStairs = true;
        watsonDistractionPosition = watsonGO.transform.position;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (canInteract)
        {
            performed = true;

            if (WatsonEagleVisionScanner.Instance.isScanning)
            {
                Debug.Log("Watson is Scanning GoToPoint");

                GoToPoint(WatsonEagleVisionScanner.Instance.movePoint.position, WatsonScanInteraction);

                player.currentInteractable = null;
            }
            else
            {
                Debug.Log("Watson is NOT Scanning");
                player.currentInteractable = null;
            }
        }
        
    }

    public void ReturnToFirstPosition()
    {
        stairsUp.canGoUpStairs = false;
        GoToPoint(firstNpcPosition, OnReturnedToStart);
        
    }

    

    private void OnReturnedToStart()
    {
        StartCoroutine(RotateCor(firstNpcRotation));
    }
    private void Rotate()
    {
        Vector3 direction = watsonGO.transform.position - npcGO.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        StartCoroutine(RotateCor(targetRotation));
    }

    private IEnumerator RotateCor(Quaternion targetRotation)
    {
        while (Quaternion.Angle(npcGO.transform.rotation, targetRotation) > 1f)
        {
            npcGO.transform.rotation = Quaternion.RotateTowards(
                npcGO.transform.rotation,
                targetRotation,
                120 * Time.deltaTime
            );

            yield return null;
        }

        npcGO.transform.rotation = targetRotation;
    }

    public void OnSherlockDetected()
    {
        if (!isOnDistraction)
            return;

        Debug.Log("Sherlock detected!! RETURN TO FIRST POSITION");
        ResetSherlockPosition(SherlockResetPosition, ResetSherlock);
        StopDistraction();
    }

    public void ResetSherlockPosition(Vector3 destination, Action onReachedDestination)
    {
        if (sherlockNavMesh == null)
        {
            Debug.LogError("sherlockNavMeshAgent == null");
            return;
        }

        if (SHmovementCoroutine != null)
            StopCoroutine(SHmovementCoroutine);

        moveAction.Disable();
        sherlockNavMesh.SetDestination(destination);
        SHmovementCoroutine = StartCoroutine(WaitForSherlockArrival(onReachedDestination));
    }

    private void ResetSherlock()
    {
        Debug.Log("ResetSherlock");
        StartCoroutine(RotateSherlock(SherlockResetRotation));
        

    }

    private IEnumerator RotateSherlock(Quaternion targetRotation)
    {
        while (Quaternion.Angle(sherlockGO.transform.rotation, targetRotation) > 1f)
        {
            sherlockGO.transform.rotation = Quaternion.RotateTowards(
                sherlockGO.transform.rotation,
                targetRotation,
                120 * Time.deltaTime
            );

            yield return null;
        }

        sherlockGO.transform.rotation = targetRotation;
        moveAction.Enable();
    }


    public void StopDistraction()
    {
        if (!isOnDistraction)
            return;

        isOnDistraction = false;
        ReturnToFirstPosition();
    }
}