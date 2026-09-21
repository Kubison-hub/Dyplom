using UnityEngine;
using UnityEngine.AI;

public enum WatsonApproachSide
{
    Left,
    Right
}

public enum CompanionApproachMode
{
    TowardActiveCharacterOnNavMesh,
    InteractionPointSideOffset,
    SpecificTransform
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class WatsonCompanionController : MonoBehaviour
{
    private static readonly int IsThinkingHash = Animator.StringToHash("IsThinking");

    public static WatsonCompanionController Instance { get; private set; }

    [Header("Interaction Focus")]
    [SerializeField, Min(1f)] private float interactionLookRotationSpeed = 220f;
    [SerializeField, Min(0f)] private float interactionFocusDuration = 4f;

    [Header("Idle Routine")]
    [SerializeField] private bool enableIdleRoutineWhenReady = false;
    [SerializeField] private Interactable[] idleInteractionTargets;
    [SerializeField, Min(0f)] private float interactionPointOffset = 2f;
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1f;
    [SerializeField, Range(0f, 1f)] private float lookAtSherlockChance = 0.35f;
    [SerializeField, Range(0f, 1f)] private float visitInteractableChance = 0.4f;
    [SerializeField, Min(0.5f)] private float routineDecisionInterval = 5f;
    [SerializeField, Min(0.1f)] private float lookAtSherlockDuration = 2f;
    [SerializeField, Min(0.1f)] private float minWaitDuration = 2f;
    [SerializeField, Min(0.1f)] private float maxWaitDuration = 4f;
    [SerializeField, Min(1f)] private float sherlockLookRotationSpeed = 90f;

    private NavMeshAgent navMeshAgent;
    private PlayerController playerController;
    private Animator animator;
    private Transform focusTarget;
    private bool isMovingToFocusPosition;
    private bool defaultAgentUpdateRotation;
    private float defaultAgentSpeed;
    private float defaultAgentAngularSpeed;
    private float focusLookRotationMultiplier = 1f;
    private float focusApproachSpeedMultiplier = 1f;
    private float focusApproachRotationSpeedMultiplier = 1f;
    private float focusEndsAt;
    private float focusRotationStartsAt;
    private float focusApproachStartsAt;
    private float focusApproachDelay;
    private float focusApproachDistance;
    private CompanionApproachMode focusApproachMode;
    private WatsonApproachSide focusApproachSide;
    private Transform focusInteractionPoint;
    private Transform focusSpecificApproachPoint;
    private bool isWaitingForReactionRotation;
    private bool isWaitingForFocusApproach;
    private float nextRoutineDecisionTime;
    private float routineStateEndsAt;
    private IdleRoutineState idleRoutineState;
    private Interactable lastVisitedTarget;
    private bool hasIsThinkingParameter;

    public bool IsResolvingInteractionApproach =>
        isWaitingForReactionRotation || isWaitingForFocusApproach || isMovingToFocusPosition;

    private enum IdleRoutineState
    {
        None,
        WalkingToInteraction,
        LookingAtSherlock,
        Waiting
    }

    private void Awake()
    {
        Instance = this;
        navMeshAgent = GetComponent<NavMeshAgent>();
        playerController = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
        defaultAgentUpdateRotation = navMeshAgent.updateRotation;
        defaultAgentSpeed = navMeshAgent.speed;
        defaultAgentAngularSpeed = navMeshAgent.angularSpeed;
        nextRoutineDecisionTime = Time.time + routineDecisionInterval;
        hasIsThinkingParameter = HasAnimatorParameter(IsThinkingHash);

        if (!enableIdleRoutineWhenReady && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsWatsonControlledByPlayer())
        {
            // A companion reaction belongs only to the inactive Watson. Do not leave
            // its target or delayed rotation alive after the player takes control.
            if (focusTarget != null || isWaitingForReactionRotation ||
                isWaitingForFocusApproach || isMovingToFocusPosition)
            {
                ClearInteractionFocus();
            }

            SetThinking(false);
            RestoreAgentRotation();
            return;
        }

        if (focusTarget != null)
        {
            if (isWaitingForReactionRotation)
            {
                if (Time.time < focusRotationStartsAt)
                    return;

                navMeshAgent.updateRotation = false;
                RotateTowards(
                    focusTarget,
                    interactionLookRotationSpeed * focusLookRotationMultiplier);
                if (!IsFacingFocusTarget())
                    return;

                isWaitingForReactionRotation = false;
                isWaitingForFocusApproach = true;
                focusApproachStartsAt = Time.time + focusApproachDelay;
                return;
            }

            if (isWaitingForFocusApproach)
            {
                navMeshAgent.updateRotation = false;
                RotateTowards(
                    focusTarget,
                    interactionLookRotationSpeed * focusLookRotationMultiplier);
                if (Time.time < focusApproachStartsAt)
                    return;

                isWaitingForFocusApproach = false;
                RestoreAgentRotation();
                isMovingToFocusPosition = TryMoveTowardSherlock(
                    focusTarget,
                    focusApproachDistance,
                    focusApproachSpeedMultiplier,
                    focusApproachRotationSpeedMultiplier,
                    focusApproachMode,
                    focusApproachSide,
                    focusInteractionPoint,
                    focusSpecificApproachPoint);
                focusEndsAt = isMovingToFocusPosition
                    ? float.PositiveInfinity
                    : Time.time + interactionFocusDuration;
            }

            if (isMovingToFocusPosition)
            {
                RestoreAgentRotation();

                if (IsAgentTravelling())
                    return;

                navMeshAgent.ResetPath();
                isMovingToFocusPosition = false;
                focusEndsAt = Time.time + interactionFocusDuration;
            }

            if (Time.time < focusEndsAt)
            {
                navMeshAgent.updateRotation = false;
                RotateTowards(
                    focusTarget,
                    interactionLookRotationSpeed * focusLookRotationMultiplier);
                return;
            }

            ClearInteractionFocus();
        }

        UpdateIdleRoutine();
    }

    private void LateUpdate()
    {
        if (!IsWatsonControlledByPlayer() && isMovingToFocusPosition)
            ApplyFocusMovementSettings();
    }

    public void FocusInteraction(
        Transform interactionTarget,
        float approachDistance = 0f,
        float approachSpeedMultiplier = 1f,
        float rotationSpeedMultiplier = 1f,
        CompanionApproachMode approachMode = CompanionApproachMode.TowardActiveCharacterOnNavMesh,
        WatsonApproachSide approachSide = WatsonApproachSide.Right,
        Transform interactionPoint = null,
        float rotationReactionDelay = 0f,
        float approachReactionDelay = 0f,
        Transform specificApproachPoint = null)
    {
        if (interactionTarget == null)
            return;

        WatsonEscortController.Instance?.ForceFarewell();

        focusTarget = interactionTarget;
        StopIdleRoutine();
        focusLookRotationMultiplier = Mathf.Max(0.01f, rotationSpeedMultiplier);
        focusApproachSpeedMultiplier = Mathf.Max(0.01f, approachSpeedMultiplier);
        focusApproachRotationSpeedMultiplier = Mathf.Max(0.01f, rotationSpeedMultiplier);
        focusApproachDistance = approachDistance;
        focusApproachMode = approachMode;
        focusApproachSide = approachSide;
        focusInteractionPoint = interactionPoint;
        focusSpecificApproachPoint = specificApproachPoint;
        focusRotationStartsAt = Time.time + Mathf.Max(0f, rotationReactionDelay);
        focusApproachDelay = Mathf.Max(0f, approachReactionDelay);
        isWaitingForReactionRotation = true;
        isWaitingForFocusApproach = false;
        isMovingToFocusPosition = false;
        focusEndsAt = float.PositiveInfinity;
    }

    public void ClearInteractionFocus()
    {
        if (isMovingToFocusPosition && navMeshAgent != null && navMeshAgent.isOnNavMesh)
            navMeshAgent.ResetPath();

        focusTarget = null;
        isMovingToFocusPosition = false;
        isWaitingForReactionRotation = false;
        isWaitingForFocusApproach = false;
        focusInteractionPoint = null;
        focusSpecificApproachPoint = null;
        focusLookRotationMultiplier = 1f;
        focusApproachSpeedMultiplier = 1f;
        focusApproachRotationSpeedMultiplier = 1f;
        RestoreAgentRotation();
        nextRoutineDecisionTime = Time.time + routineDecisionInterval;
    }

    private bool IsFacingFocusTarget()
    {
        if (focusTarget == null)
            return true;

        Vector3 direction = focusTarget.position - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude <= 0.001f || Vector3.Angle(transform.forward, direction) <= 1f;
    }

    private bool TryMoveTowardSherlock(
        Transform interactionTarget,
        float approachDistance,
        float approachSpeedMultiplier,
        float rotationSpeedMultiplier,
        CompanionApproachMode approachMode,
        WatsonApproachSide approachSide,
        Transform interactionPoint,
        Transform specificApproachPoint)
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh ||
            (approachDistance <= 0f && approachMode != CompanionApproachMode.SpecificTransform))
            return false;

        Vector3 desiredPosition;
        if (approachMode == CompanionApproachMode.SpecificTransform)
        {
            if (specificApproachPoint == null)
                return false;

            desiredPosition = specificApproachPoint.position;
        }
        else if (approachMode == CompanionApproachMode.InteractionPointSideOffset)
        {
            Transform anchor = interactionPoint != null ? interactionPoint : interactionTarget;
            if (anchor == null)
                return false;

            Vector3 sideDirection = approachSide == WatsonApproachSide.Left
                ? -anchor.right
                : anchor.right;
            desiredPosition = anchor.position + sideDirection * approachDistance;
        }
        else
        {
            Transform sherlock = GetSherlockTransform();
            if (sherlock == null)
                return false;

            Vector3 directionFromSherlock = transform.position - sherlock.position;
            directionFromSherlock.y = 0f;
            float currentDistance = directionFromSherlock.magnitude;

            // Do not make Watson step away when he is already close enough.
            if (currentDistance <= approachDistance || currentDistance < 0.001f)
                return false;

            desiredPosition = sherlock.position +
                              directionFromSherlock.normalized * approachDistance;
        }
        if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return false;

        focusApproachSpeedMultiplier = Mathf.Max(0.01f, approachSpeedMultiplier);
        focusApproachRotationSpeedMultiplier = Mathf.Max(0.01f, rotationSpeedMultiplier);
        ApplyFocusMovementSettings();
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(hit.position);
        return true;
    }

    private void UpdateIdleRoutine()
    {
        if (!enableIdleRoutineWhenReady)
        {
            SetThinking(false);
            RestoreAgentRotation();
            return;
        }

        switch (idleRoutineState)
        {
            case IdleRoutineState.WalkingToInteraction:
                UpdateWalkToInteraction();
                return;

            case IdleRoutineState.LookingAtSherlock:
                UpdateLookAtSherlock();
                return;

            case IdleRoutineState.Waiting:
                if (Time.time < routineStateEndsAt)
                {
                    RestoreAgentRotation();
                    return;
                }

                idleRoutineState = IdleRoutineState.None;
                nextRoutineDecisionTime = Time.time + routineDecisionInterval;
                SetThinking(false);
                break;
        }

        RestoreAgentRotation();

        if (IsAgentTravelling() || Time.time < nextRoutineDecisionTime)
            return;

        DecideNextIdleAction();
    }

    private void DecideNextIdleAction()
    {
        float roll = Random.value;
        float visitThreshold = Mathf.Clamp01(lookAtSherlockChance + visitInteractableChance);

        if (roll < lookAtSherlockChance && GetSherlockTransform() != null)
        {
            idleRoutineState = IdleRoutineState.LookingAtSherlock;
            routineStateEndsAt = Time.time + lookAtSherlockDuration;
            return;
        }

        if (roll < visitThreshold && StartWalkToRandomInteraction())
            return;

        StartWaiting(false);
    }

    private bool StartWalkToRandomInteraction()
    {
        if (idleInteractionTargets == null || idleInteractionTargets.Length == 0 || !navMeshAgent.isOnNavMesh)
            return false;

        Interactable target = GetRandomIdleTarget();
        if (target == null)
            return false;

        Transform interactionPoint = target.interactabePoint != null
            ? target.interactabePoint
            : target.transform;

        Vector3 directionAwayFromPoint = transform.position - interactionPoint.position;
        directionAwayFromPoint.y = 0f;

        if (directionAwayFromPoint.sqrMagnitude <= 0.001f)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            directionAwayFromPoint = new Vector3(randomDirection.x, 0f, randomDirection.y);
        }
        else
        {
            directionAwayFromPoint.Normalize();
        }

        Vector3 desiredPosition = interactionPoint.position + directionAwayFromPoint * interactionPointOffset;
        if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return false;

        lastVisitedTarget = target;
        SetThinking(false);
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(hit.position);
        idleRoutineState = IdleRoutineState.WalkingToInteraction;
        return true;
    }

    private Interactable GetRandomIdleTarget()
    {
        int validTargetCount = 0;

        for (int i = 0; i < idleInteractionTargets.Length; i++)
        {
            if (idleInteractionTargets[i] != null && idleInteractionTargets[i] != lastVisitedTarget)
                validTargetCount++;
        }

        if (validTargetCount == 0)
        {
            for (int i = 0; i < idleInteractionTargets.Length; i++)
            {
                if (idleInteractionTargets[i] != null)
                    return idleInteractionTargets[i];
            }

            return null;
        }

        int selectedIndex = Random.Range(0, validTargetCount);
        for (int i = 0; i < idleInteractionTargets.Length; i++)
        {
            Interactable candidate = idleInteractionTargets[i];
            if (candidate == null || candidate == lastVisitedTarget)
                continue;

            if (selectedIndex-- == 0)
                return candidate;
        }

        return null;
    }

    private void UpdateWalkToInteraction()
    {
        if (IsAgentTravelling())
            return;

        navMeshAgent.ResetPath();
        navMeshAgent.isStopped = true;
        StartWaiting(true);
    }

    private void UpdateLookAtSherlock()
    {
        Transform sherlock = GetSherlockTransform();
        if (sherlock == null || Time.time >= routineStateEndsAt)
        {
            idleRoutineState = IdleRoutineState.None;
            nextRoutineDecisionTime = Time.time + routineDecisionInterval;
            RestoreAgentRotation();
            return;
        }

        navMeshAgent.updateRotation = false;
        RotateTowards(sherlock, sherlockLookRotationSpeed);
    }

    private void StartWaiting(bool isThinkingAtInteraction)
    {
        idleRoutineState = IdleRoutineState.Waiting;
        routineStateEndsAt = Time.time + Random.Range(
            minWaitDuration,
            Mathf.Max(minWaitDuration, maxWaitDuration));
        SetThinking(isThinkingAtInteraction);
    }

    private void StopIdleRoutine()
    {
        idleRoutineState = IdleRoutineState.None;
        SetThinking(false);
    }

    private bool IsAgentTravelling()
    {
        if (navMeshAgent.pathPending)
            return true;

        if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + 0.05f)
            return true;

        return navMeshAgent.hasPath && navMeshAgent.velocity.sqrMagnitude > 0.01f;
    }

    private Transform GetSherlockTransform()
    {
        return SwitchCharacter.Instance != null
            ? SwitchCharacter.Instance.sherlockTransform
            : null;
    }

    private bool RotateTowards(Transform target, float rotationSpeed)
    {
        if (target == null)
            return true;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);

        return Quaternion.Angle(transform.rotation, targetRotation) <= 0.5f;
    }

    private bool IsWatsonControlledByPlayer()
    {
        return SwitchCharacter.Instance != null &&
               SwitchCharacter.Instance.activePlayerIndex == 1;
    }

    private void RestoreAgentRotation()
    {
        if (navMeshAgent != null)
            navMeshAgent.updateRotation = defaultAgentUpdateRotation;
    }

    private void ApplyFocusMovementSettings()
    {
        if (navMeshAgent == null)
            return;

        float baseSpeed = playerController != null
            ? playerController.normalSpeed
            : defaultAgentSpeed;

        navMeshAgent.speed = baseSpeed * focusApproachSpeedMultiplier;
        navMeshAgent.angularSpeed = defaultAgentAngularSpeed * focusApproachRotationSpeedMultiplier;
    }

    private void SetThinking(bool isThinking)
    {
        if (animator != null && hasIsThinkingParameter)
            animator.SetBool(IsThinkingHash, isThinking);
    }

    private bool HasAnimatorParameter(int parameterHash)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }
}
