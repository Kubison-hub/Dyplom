using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Interactable))]
public sealed class Int_SherlockWatsonDialog : Int_lv1_NpcDialogBase, IPlayerInteractionAvailability
{
    protected override bool CountsAsSessionWitness => false;

    [Header("Companion Approach")]
    [SerializeField, Min(0.1f)] private float approachDistance = 1.5f;
    [SerializeField, Min(0.1f)] private float approachNavMeshSampleRadius = 1.2f;
    [SerializeField, Min(1f)] private float companionTurnSpeed = 360f;
    [SerializeField, Range(0f, 1f)] private float companionTurnStartProgress = 0.5f;

    private Interactable interactable;
    private Coroutine delayedCompanionTurnCoroutine;
    private Coroutine companionTurnCoroutine;
    private bool companionApproachPending;
    private bool dialogueStartPending;

    protected override InteractionType RequiredInteractionType => InteractionType.Int_SherlockWatsonDialog;
    protected override bool ShouldFacePlayerBeforeDialogue => false;
    protected override bool ShouldFaceInteractingPlayerTowardNpcBeforeDialogue => false;

    public bool CanPlayerInteract(PlayerController player, bool defaultAvailability)
    {
        if (!defaultAvailability || player == null)
            return false;

        return player.playerCharacter == PlayerCharacter.Sherlock || player.CompareTag("PlayerA");
    }

    protected override void Start()
    {
        base.Start();
        interactable = GetComponent<Interactable>();
    }

    public void NotifyInteractionSelected(PlayerController player)
    {
        if (player == null || dialogueStartPending || FindCompanion(player) == null)
            return;

        // A previous world interaction may still be steering Watson toward its
        // reaction target. The face-to-face dialogue owns his movement from here.
        WatsonCompanionController.Instance?.ClearInteractionFocus();
        companionApproachPending = true;
    }

    public bool RequiresCompanionApproach(PlayerController player)
    {
        return companionApproachPending &&
               !dialogueStartPending &&
               player != null &&
               FindCompanion(player) != null;
    }

    public void MovePlayerAfterCompanionReaction(PlayerController player)
    {
        if (player == null)
            return;

        PlayerController companion = FindCompanion(player);
        if (companion == null)
            return;

        float initialDistance = GetHorizontalDistance(player.transform.position, companion.transform.position);

        // The requested distance is a maximum approach distance, not an exact ring
        // around the companion. If the player is already close enough, start the
        // face-to-face interaction without making them step away first.
        if (initialDistance <= approachDistance)
        {
            companionApproachPending = false;
            player.SetWaitingForInteractionReaction(false);
            player.currentInteractionPoint = null;
            player.ClearAutoInteractionApproachPoint();

            if (player.navMeshAgent != null && player.navMeshAgent.isOnNavMesh)
            {
                player.navMeshAgent.ResetPath();
                player.navMeshAgent.isStopped = false;
                player.navMeshAgent.velocity = Vector3.zero;
            }

            StopPendingCompanionTurn();

            // No NavMesh approach is needed. Clear the world-interaction owner so
            // PlayerController cannot start a second RotateAndPerform coroutine,
            // then let this component own facing and dialogue startup directly.
            player.currentInteractable = null;
            PerformInteraction(player);
            return;
        }

        float finalDistance = approachDistance;

        if (!TryGetApproachPoint(player, companion, out Vector3 approachPoint))
        {
            companionApproachPending = false;
            player.CancelUnreachableInteraction();
            return;
        }

        player.currentInteractionPoint = null;
        player.SetAutoInteractionApproachPoint(approachPoint);
        finalDistance = GetHorizontalDistance(approachPoint, companion.transform.position);

        companionApproachPending = false;
        player.SetWaitingForInteractionReaction(false);
        player.MoveToInteractable();

        StopPendingCompanionTurn();

        float turnDistance = Mathf.Lerp(initialDistance, finalDistance, companionTurnStartProgress);
        delayedCompanionTurnCoroutine = StartCoroutine(
            RotateCompanionWhenPlayerReachesDistance(player, companion, turnDistance));
    }

    public new void PerformInteraction(PlayerController player)
    {
        if (dialogueStartPending)
            return;

        StartCoroutine(BeginDialogueAfterCharactersFaceEachOther(player));
    }

    private IEnumerator BeginDialogueAfterCharactersFaceEachOther(PlayerController player)
    {
        dialogueStartPending = true;

        PlayerController companion = FindCompanion(player);
        if (player != null && companion != null)
        {
            // Reaching the interaction point supersedes the delayed mid-approach
            // turn. Waiting for it here can deadlock when the player already
            // stands exactly at the generated approach point.
            StopPendingCompanionTurn();

            yield return FaceEachOther(player, companion);
        }

        if (TutorialTimeline.Instance != null)
            yield return TutorialTimeline.Instance.PrepareFirstSherlockWatsonDialogue();

        dialogueStartPending = false;
        base.PerformInteraction(player);
    }

    private bool TryGetApproachPoint(
        PlayerController player,
        PlayerController companion,
        out Vector3 approachPoint)
    {
        approachPoint = default;
        if (player == null || companion == null || player.navMeshAgent == null)
            return false;

        Vector3 directionFromCompanion = player.transform.position - companion.transform.position;
        directionFromCompanion.y = 0f;
        if (directionFromCompanion.sqrMagnitude <= 0.001f)
            directionFromCompanion = -companion.transform.forward;

        Vector3 desiredPoint = companion.transform.position +
                               directionFromCompanion.normalized * approachDistance;

        if (!NavMesh.SamplePosition(
                desiredPoint,
                out NavMeshHit navMeshHit,
                approachNavMeshSampleRadius,
                NavMesh.AllAreas))
        {
            return false;
        }

        if (!NavMeshWallGuard.TryGetClearPath(player.navMeshAgent, navMeshHit.position, out NavMeshPath path))
            return false;

        approachPoint = navMeshHit.position;
        return true;
    }

    private IEnumerator RotateCompanionWhenPlayerReachesDistance(
        PlayerController player,
        PlayerController companion,
        float turnDistance)
    {
        while (player != null && companion != null &&
               GetHorizontalDistance(player.transform.position, companion.transform.position) > turnDistance)
        {
            if (player.currentInteractable != interactable)
                break;

            yield return null;
        }

        if (player != null && companion != null && player.currentInteractable == interactable)
        {
            companionTurnCoroutine = StartCoroutine(RotateTowards(companion, player.transform));
            yield return companionTurnCoroutine;
            companionTurnCoroutine = null;
        }

        delayedCompanionTurnCoroutine = null;
    }

    private IEnumerator FaceEachOther(PlayerController player, PlayerController companion)
    {
        companionTurnCoroutine = StartCoroutine(RotateTowards(companion, player.transform));
        yield return companionTurnCoroutine;
        companionTurnCoroutine = null;

        yield return RotateTowards(player, companion.transform);
    }

    private IEnumerator RotateTowards(PlayerController character, Transform target)
    {
        if (character == null || target == null)
            yield break;

        NavMeshAgent agent = character.navMeshAgent;
        bool restoreAgentRotation = agent != null && agent.updateRotation;
        if (agent != null)
            agent.updateRotation = false;

        while (character != null && target != null)
        {
            Vector3 direction = target.position - character.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
                break;

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            character.transform.rotation = Quaternion.RotateTowards(
                character.transform.rotation,
                targetRotation,
                companionTurnSpeed * Time.deltaTime);

            if (Quaternion.Angle(character.transform.rotation, targetRotation) <= 0.5f)
                break;

            yield return null;
        }

        if (agent != null)
            agent.updateRotation = restoreAgentRotation;

    }

    private static PlayerController FindCompanion(PlayerController player)
    {
        if (player == null)
            return null;

        PlayerCharacter companionCharacter = player.playerCharacter == PlayerCharacter.Sherlock
            ? PlayerCharacter.Watson
            : PlayerCharacter.Sherlock;

        foreach (PlayerController candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (candidate != null && candidate.playerCharacter == companionCharacter)
                return candidate;
        }

        return null;
    }

    private static float GetHorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }

    private void OnDisable()
    {
        StopPendingCompanionTurn();
        companionApproachPending = false;
        dialogueStartPending = false;
    }

    private void StopPendingCompanionTurn()
    {
        if (delayedCompanionTurnCoroutine != null)
            StopCoroutine(delayedCompanionTurnCoroutine);

        if (companionTurnCoroutine != null)
            StopCoroutine(companionTurnCoroutine);

        delayedCompanionTurnCoroutine = null;
        companionTurnCoroutine = null;
    }
}
