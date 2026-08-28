using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class Int_Lvl_3_Leadder : Lvl3InteractionDialogueBase
{
    [Header("Exit State")]
    [FormerlySerializedAs("canExit")]
    [SerializeField] private bool canGo;

    [Header("Upper Floor Markers")]
    [SerializeField] private Transform sherlockIntPoint;
    [SerializeField] private Transform watsonIntPoint;
    [SerializeField] private Transform ethelIntPoint;
    [SerializeField] private Transform ethel;
    [SerializeField, Min(0.05f)] private float navMeshSampleRadius = 1f;
    [SerializeField, Min(0.05f)] private float rotationLockDuration = 0.75f;

    [Header("Basement Exposure")]
    [SerializeField] private BasementExposureController basementExposureController;

    [Header("After Upper Floor Arrival")]
    [SerializeField] private Int_SetupLastPoints setupLastPoints;
    [Tooltip("Objects activated after Sherlock, Watson and Ethel reach the upper floor.")]
    [SerializeField] private GameObject[] activateAfterTeleport;
    [Tooltip("Objects disabled when Sherlock, Watson and Ethel leave the basement.")]
    [SerializeField] private GameObject[] deactivateAfterTeleport;
    [Tooltip("Activated after the group teleports upstairs. Use Int_lv4_EthelRun on this GameObject.")]
    [SerializeField] private GameObject ethelRunAfterTeleport;

    [Header("Locked Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] cannotGoDialogue;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => cannotGoDialogue;
    private bool teleportQueued;

    public bool CanGo
    {
        get => canGo;
        set => canGo = value;
    }

    // Compatibility with the existing Ethel exit sequence.
    public bool canExit
    {
        get => canGo;
        set => canGo = value;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (!canGo)
        {
            PlayDialogue(player, cannotGoDialogue);
            return;
        }

        if (!teleportQueued)
            StartCoroutine(TeleportGroupAfterInteraction(player));
    }

    private IEnumerator TeleportGroupAfterInteraction(PlayerController interactingPlayer)
    {
        teleportQueued = true;

        // Lets PlayerController finish its arrival rotation before we move it.
        yield return new WaitForEndOfFrame();

        interactingPlayer.CancelInteractionForTeleport();
        SetObjectsActive(activateAfterTeleport);
        SetObjectsInactive(deactivateAfterTeleport);

        if (basementExposureController == null)
            basementExposureController = FindFirstObjectByType<BasementExposureController>();

        if (basementExposureController != null)
            yield return basementExposureController.RestoreBeforeLeavingBasement();

        TeleportPlayer(PlayerCharacter.Sherlock, sherlockIntPoint);
        TeleportPlayer(PlayerCharacter.Watson, watsonIntPoint);
        TeleportTransform(ethel, ethelIntPoint);
        interactingPlayer.ResumeAfterTeleport();
        Transform activeMarker = GetMarkerFor(interactingPlayer);
        if (activeMarker != null)
            interactingPlayer.LockRotationAfterTeleport(activeMarker.rotation, rotationLockDuration);

        setupLastPoints?.PerformInteraction(interactingPlayer);
        ethelRunAfterTeleport?.SetActive(true);
        CluesLog.Instance?.ReplaceFindEthelWithSessionConfrontation();

        teleportQueued = false;
    }

    private static void SetObjectsActive(GameObject[] objectsToActivate)
    {
        if (objectsToActivate == null)
            return;

        foreach (GameObject target in objectsToActivate)
        {
            if (target != null)
                target.SetActive(true);
        }
    }

    private static void SetObjectsInactive(GameObject[] objectsToDeactivate)
    {
        if (objectsToDeactivate == null)
            return;

        foreach (GameObject target in objectsToDeactivate)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    private Transform GetMarkerFor(PlayerController player)
    {
        return player != null && player.playerCharacter == PlayerCharacter.Watson
            ? watsonIntPoint
            : sherlockIntPoint;
    }

    private void TeleportPlayer(PlayerCharacter character, Transform marker)
    {
        if (marker == null)
        {
            Debug.LogWarning($"{name}: missing marker for {character}.", this);
            return;
        }

        foreach (PlayerController candidate in FindObjectsOfType<PlayerController>())
        {
            if (candidate != null && candidate.playerCharacter == character)
            {
                TeleportTransform(candidate.transform, marker);
                return;
            }
        }

        Debug.LogWarning($"{name}: could not find {character}.", this);
    }

    private void TeleportTransform(Transform actor, Transform marker)
    {
        if (actor == null || marker == null)
            return;

        Vector3 targetPosition = marker.position;
        if (NavMesh.SamplePosition(marker.position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            targetPosition = hit.position;
        else
            Debug.LogWarning($"{name}: marker {marker.name} is outside NavMesh.", marker);

        NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
        {
            bool wasStopped = agent.isStopped;
            bool usedAgentRotation = agent.updateRotation;
            agent.isStopped = true;
            agent.updateRotation = false;
            agent.velocity = Vector3.zero;
            agent.Warp(targetPosition);
            agent.ResetPath();
            agent.nextPosition = targetPosition;
            actor.SetPositionAndRotation(targetPosition, marker.rotation);
            agent.updateRotation = usedAgentRotation;
            agent.isStopped = wasStopped;
        }
        else
        {
            actor.position = targetPosition;
        }

        actor.rotation = marker.rotation;
    }
}
