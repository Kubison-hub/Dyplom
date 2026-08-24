using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class Int_LastCutsceneTrigger : MonoBehaviour
{
    [Header("Actors")]
    [SerializeField] private PlayerController sherlock;
    [SerializeField] private PlayerController watson;
    [SerializeField] private Transform sherlockLastPoint;
    [SerializeField] private Transform watsonLastPoint;
    [SerializeField, Min(0.05f)] private float arrivalDistance = 0.15f;

    [Header("Camera")]
    [SerializeField] private CameraZoomState cameraPreset = CameraZoomState.Medium;
    [SerializeField] private float cameraHorizontalAxis = -203f;
    [SerializeField, Min(0.1f)] private float cameraHorizontalOrbitSpeed = 6f;

    [Header("Completion")]
    [Tooltip("Leave false for the final scene: player input remains locked after both actors arrive.")]
    [SerializeField] private bool restoreInputOnComplete;

    private bool sequenceStarted;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController triggeringPlayer = other.GetComponentInParent<PlayerController>();
        if (sequenceStarted || triggeringPlayer == null)
            return;

        StartCoroutine(RunFinalSequence(triggeringPlayer));
    }

    private IEnumerator RunFinalSequence(PlayerController triggeringPlayer)
    {
        sequenceStarted = true;
        ResolvePlayers();

        sherlock?.SetTutorialInputLocked(true);
        watson?.SetTutorialInputLocked(true);

        bool sherlockArrived = sherlock == null || sherlockLastPoint == null;
        bool watsonArrived = watson == null || watsonLastPoint == null;
        if (!sherlockArrived)
            StartCoroutine(MoveActorTo(sherlock, sherlockLastPoint, () => sherlockArrived = true));
        if (!watsonArrived)
            StartCoroutine(MoveActorTo(watson, watsonLastPoint, () => watsonArrived = true));

        CameraController triggeringCamera = triggeringPlayer.GetComponent<CameraController>();
        if (triggeringCamera == null)
            triggeringCamera = triggeringPlayer.GetComponentInChildren<CameraController>(true);

        if (triggeringCamera != null)
        {
            triggeringCamera.SetZoomState(cameraPreset);
            triggeringCamera.OrbitHorizontalAxisTo(cameraHorizontalAxis, cameraHorizontalOrbitSpeed);
        }

        while (!sherlockArrived || !watsonArrived)
            yield return null;

        Debug.Log("KONIEC");

        if (restoreInputOnComplete)
        {
            sherlock?.SetTutorialInputLocked(false);
            watson?.SetTutorialInputLocked(false);
        }
    }

    private IEnumerator MoveActorTo(PlayerController actor, Transform marker, Action onArrived)
    {
        NavMeshAgent agent = actor != null ? actor.GetComponent<NavMeshAgent>() : null;
        if (agent == null || !agent.isOnNavMesh ||
            !NavMesh.SamplePosition(marker.position, out NavMeshHit destination, 1f, NavMesh.AllAreas))
        {
            Debug.LogWarning($"{name}: {actor?.name ?? "Actor"} cannot reach final marker '{marker.name}'.", this);
            onArrived?.Invoke();
            yield break;
        }

        agent.ResetPath();
        agent.isStopped = false;
        agent.updateRotation = true;
        agent.SetDestination(destination.position);

        while (agent.pathPending)
            yield return null;

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            while (agent.remainingDistance > Mathf.Max(arrivalDistance, agent.stoppingDistance))
                yield return null;
        }
        else
        {
            Debug.LogWarning($"{name}: {actor.name} has no complete path to '{marker.name}'.", this);
        }

        agent.ResetPath();
        agent.isStopped = true;
        actor.transform.rotation = marker.rotation;
        onArrived?.Invoke();
    }

    private void ResolvePlayers()
    {
        if (sherlock == null || watson == null)
        {
            foreach (PlayerController player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (player.playerCharacter == PlayerCharacter.Sherlock)
                    sherlock ??= player;
                else if (player.playerCharacter == PlayerCharacter.Watson)
                    watson ??= player;
            }
        }

    }
}
