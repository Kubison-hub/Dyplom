using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public class Int_SherlockWatsonGoToLastPoint : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private PlayerController sherlock;
    [SerializeField] private PlayerController watson;
    [SerializeField] private Transform sherlockLastPoint;
    [SerializeField] private Transform watsonLastPoint;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;

    private Interactable interactable;
    private bool isSequenceRunning;

    private void Reset() => SetupInteractable();
    private void OnValidate() => SetupInteractable();

    private void Awake()
    {
        SetupInteractable();
        ResolvePlayers();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isSequenceRunning)
            return;

        isSequenceRunning = true;
        if (interactable != null)
            interactable.isInteractableActive = false;

        if (player != null)
            player.currentInteractable = null;

        StartCoroutine(MoveCharactersToLastPoints());
    }

    private IEnumerator MoveCharactersToLastPoints()
    {
        ResolvePlayers();
        LockGameplayInput();

        bool sherlockDone = false;
        bool watsonDone = false;
        StartCoroutine(MoveActor(sherlock, sherlockLastPoint, () => sherlockDone = true));
        StartCoroutine(MoveActor(watson, watsonLastPoint, () => watsonDone = true));

        while (!sherlockDone || !watsonDone)
            yield return null;
    }

    private IEnumerator MoveActor(PlayerController actor, Transform destination, Action completed)
    {
        if (actor == null || destination == null)
        {
            Debug.LogWarning($"{name}: missing character or Last Point.", this);
            completed?.Invoke();
            yield break;
        }

        NavMeshAgent agent = actor.navMeshAgent != null
            ? actor.navMeshAgent
            : actor.GetComponent<NavMeshAgent>();
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(destination.position, out NavMeshHit target, navMeshSampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: {actor.name} or '{destination.name}' is outside the NavMesh.", this);
            completed?.Invoke();
            yield break;
        }

        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(target.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.remainingDistance > Mathf.Max(arrivalDistance, agent.stoppingDistance))
                yield return null;

            agent.ResetPath();
            agent.isStopped = true;
            actor.transform.rotation = destination.rotation;
        }
        else
        {
            Debug.LogWarning($"{name}: {actor.name} cannot reach '{destination.name}'.", this);
        }

        completed?.Invoke();
    }

    private void LockGameplayInput()
    {
        if (SwitchCharacter.Instance != null)
            SwitchCharacter.Instance.canSwitch = false;

        sherlock?.SetTutorialInputLocked(true);
        watson?.SetTutorialInputLocked(true);
    }

    private void ResolvePlayers()
    {
        if (sherlock != null && watson != null)
            return;

        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate == null)
                continue;

            if (sherlock == null && candidate.playerCharacter == PlayerCharacter.Sherlock)
                sherlock = candidate;
            else if (watson == null && candidate.playerCharacter == PlayerCharacter.Watson)
                watson = candidate;
        }
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_SherlockWatsonGoToLastPoint);
    }
}
