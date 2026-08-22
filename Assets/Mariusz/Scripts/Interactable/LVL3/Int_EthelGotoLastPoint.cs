using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public class Int_EthelGotoLastPoint : MonoBehaviour
{
    [Header("Ethel")]
    [SerializeField] private Transform ethel;
    [SerializeField] private NavMeshAgent ethelAgent;
    [SerializeField] private Animator ethelAnimator;
    [SerializeField] private Transform ethelLastPoint;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float runningSpeed = 3.5f;

    [Header("Animation")]
    [SerializeField] private string isWalkingParameter = "IsWalking";
    [SerializeField] private string isRunningParameter = "IsRunning";

    private Interactable interactable;
    private bool isRunningSequence;

    private void Reset() => SetupInteractable();
    private void OnValidate() => SetupInteractable();

    private void Awake()
    {
        SetupInteractable();

        if (ethel == null)
            ethel = transform;

        if (ethelAgent == null && ethel != null)
            ethelAgent = ethel.GetComponent<NavMeshAgent>();

        if (ethelAnimator == null && ethel != null)
            ethelAnimator = ethel.GetComponentInChildren<Animator>(true);
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isRunningSequence)
            return;

        isRunningSequence = true;
        if (interactable != null)
            interactable.isInteractableActive = false;

        if (player != null)
            player.currentInteractable = null;

        StartCoroutine(MoveEthelToLastPoint());
    }

    private IEnumerator MoveEthelToLastPoint()
    {
        if (ethel == null || ethelAgent == null || ethelLastPoint == null)
        {
            Debug.LogWarning($"{name}: assign Ethel, Ethel Agent and Ethel Last Point.", this);
            yield break;
        }

        if (!ethelAgent.isOnNavMesh ||
            !NavMesh.SamplePosition(ethelLastPoint.position, out NavMeshHit destination, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: Ethel or her Last Point is outside the NavMesh.", this);
            yield break;
        }

        ethelAgent.speed = runningSpeed;
        ethelAgent.isStopped = false;
        ethelAgent.updateRotation = true;
        SetRunningAnimation(true);
        ethelAgent.SetDestination(destination.position);

        while (ethelAgent.pathPending)
            yield return null;

        if (ethelAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"{name}: Ethel cannot reach '{ethelLastPoint.name}'.", this);
            SetRunningAnimation(false);
            yield break;
        }

        while (ethelAgent.remainingDistance > Mathf.Max(arrivalDistance, ethelAgent.stoppingDistance))
            yield return null;

        ethelAgent.ResetPath();
        ethelAgent.isStopped = true;
        ethel.transform.rotation = ethelLastPoint.rotation;
        SetRunningAnimation(false);
    }

    private void SetRunningAnimation(bool isRunning)
    {
        if (ethelAnimator == null)
            return;

        if (!string.IsNullOrWhiteSpace(isWalkingParameter))
            ethelAnimator.SetBool(isWalkingParameter, false);

        if (!string.IsNullOrWhiteSpace(isRunningParameter))
            ethelAnimator.SetBool(isRunningParameter, isRunning);
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_EthelGotoLastPoint);
    }
}
