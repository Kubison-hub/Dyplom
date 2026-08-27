using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(NavMeshAgent))]
public class WatsonEscortController : MonoBehaviour
{
    public enum CursorMode
    {
        Normal,
        Move,
        Npc
    }

    public static WatsonEscortController Instance { get; private set; }

    [Header("Formation")]
    [SerializeField, Min(0.1f)] private float sideDistance = 0.75f;
    [SerializeField, Min(0.05f)] private float formationTolerance = 0.2f;
    [SerializeField, Min(1f)] private float watsonRotationSpeed = 420f;
    [SerializeField, Range(0f, 1f)] private float finalFaceAmount = 0.85f;
    [SerializeField, Min(0f)] private float navMeshEdgeClearance = 0.2f;
    [SerializeField, Min(1f)] private float maxCatchUpSpeedMultiplier = 1.8f;
    [SerializeField, Range(0.1f, 1f)] private float minNpcEscortSpeedMultiplier = 0.55f;

    [Header("Cursor Raycasts")]
    [SerializeField] private LayerMask npcLayerMask = ~0;
    [SerializeField] private LayerMask groundLayerMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Placement Gesture")]
    [SerializeField, Min(0.01f)] private float placementNavMeshSampleRadius = 1f;
    [SerializeField] private Vector3 placementPreviewOffset = Vector3.zero;

    [Header("Escort Range")]
    [SerializeField, Min(0.1f)] private float maxEscortRange = 8f;
    [Tooltip("Optional filter. When set, only materials using this shader receive the preview visibility change.")]
    [SerializeField] private Shader placementPreviewShader;
    [SerializeField] private string placementPreviewVisibilityProperty = "_Visibility";
    [SerializeField, Min(0f)] private float placementPreviewBaseVisibility = 1.2f;
    [SerializeField] private Color escortRingColor = new Color(0.36f, 0.9f, 0.55f, 0.8f);
    [Tooltip("The preview remains fully visible until this percentage of the escort range, then fades to zero at the edge.")]
    [SerializeField, Range(0f, 0.99f)] private float rangePreviewFadeStart = 0.75f;

    [Header("Influence Points")]
    [Tooltip("Used only when no influence point affects the chosen NPC position.")]
    [SerializeField] private bool fallBackToMovementDirection = true;

    private PlayerController watson;
    private NavMeshAgent watsonAgent;
    private WatsonEscortNPC escortedNpc;
    private Coroutine escortRoutine;
    private GameObject placementPreview;
    private bool isPlacingDestination;
    private Vector3 placementDestination;
    private Quaternion placementRotation = Quaternion.identity;
    private float defaultWatsonSpeed;
    private Vector3 escortRangeOrigin;
    private bool hasEscortRangeOrigin;
    private bool isPlacementWithinRange;
    private MaterialPropertyBlock placementPreviewPropertyBlock;
    private float sherlockActiveEscortElapsed;

    public bool IsEscorting => escortedNpc != null;

    private float ActiveEscortRange => escortedNpc != null && escortedNpc.EscortRangeOverride > 0f
        ? escortedNpc.EscortRangeOverride
        : maxEscortRange;

    public bool IsEscortingNpc(WatsonEscortNPC npc)
    {
        return npc != null && escortedNpc == npc;
    }

    private void Awake()
    {
        Instance = this;
        watson = GetComponent<PlayerController>();
        watsonAgent = GetComponent<NavMeshAgent>();
        placementPreviewPropertyBlock = new MaterialPropertyBlock();
        if (watsonAgent != null)
            defaultWatsonSpeed = watsonAgent.speed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        UpdateSherlockEscortTimeout();

        bool watsonGripActive = MagnifierGlassController.IsWatsonGripActive;
        if (!watsonGripActive)
        {
            if (isPlacingDestination)
                CancelPlacementPreview();
            return;
        }

        if (!isPlacingDestination && escortedNpc != null && escortRoutine == null && escortedNpc.IsReadyForEscort)
            BeginPlacementPreview();

        if (!isPlacingDestination || Mouse.current == null)
            return;

        UpdatePlacementPreview();
    }

    private void UpdateSherlockEscortTimeout()
    {
        if (escortedNpc == null || escortRoutine != null || !escortedNpc.IsReadyForEscort)
            return;

        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.activePlayerIndex != 0)
            return;

        float timeout = escortedNpc.SherlockActiveFarewellDelay;
        if (timeout <= 0f)
            return;

        sherlockActiveEscortElapsed += Time.deltaTime;
        if (sherlockActiveEscortElapsed >= timeout)
            DismissEscort(true, true);
    }

    public bool TryStartEscort(WatsonEscortNPC npc, PlayerController player)
    {
        if (npc == null || player != watson || !npc.CanBeEscortedBy(player) ||
            watsonAgent == null || !watsonAgent.isOnNavMesh)
            return false;

        if (escortedNpc == npc && isPlacingDestination)
            return true;

        if (escortedNpc != null && escortedNpc != npc)
            DismissEscort(false);

        if (escortRoutine != null)
            StopCoroutine(escortRoutine);

        escortedNpc = npc;
        sherlockActiveEscortElapsed = 0f;
        escortRoutine = StartCoroutine(ApproachNpcAndBeginEscort());
        return true;
    }

    public void ForceFarewell()
    {
        if (escortedNpc != null)
            DismissEscort(true);
    }

    public bool TryHandleGroundClick(PlayerController player, Vector3 groundPoint)
    {
        if (player != watson || escortedNpc == null)
            return false;

        if (!MagnifierGlassController.IsWatsonGripActive)
        {
            DismissEscort(true);
            return false;
        }

        if (escortRoutine != null || !NavMesh.SamplePosition(groundPoint, out NavMeshHit hit, placementNavMeshSampleRadius, NavMesh.AllAreas))
            return true;

        Vector3 direction = hit.position - transform.position;
        direction.y = 0f;
        Quaternion rotation = direction.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(direction.normalized, Vector3.up)
            : transform.rotation;
        escortRoutine = StartCoroutine(EscortTo(hit.position, rotation));
        return true;
    }

    public bool TryBeginPlacementFromPointer(PlayerController player)
    {
        if (player != watson || escortedNpc == null || escortRoutine != null ||
            !MagnifierGlassController.IsWatsonGripActive)
            return false;

        if (!isPlacingDestination)
        {
            BeginPlacementPreview();
            return true;
        }

        // The preview has already been positioned and rotated by the mouse.
        // A single ground click now confirms that exact state.
        CommitPlacementPreview();
        return true;
    }

    public CursorMode GetCursorMode(Vector2 screenPosition)
    {
        if (!IsEscorting || Camera.main == null)
            return CursorMode.Normal;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit npcHit, 100f, npcLayerMask, triggerInteraction) &&
            npcHit.collider.GetComponentInParent<WatsonEscortNPC>() != null)
            return CursorMode.Npc;

        return Physics.Raycast(ray, 100f, groundLayerMask, triggerInteraction)
            ? CursorMode.Move
            : CursorMode.Normal;
    }

    private IEnumerator ApproachNpcAndBeginEscort()
    {
        Vector3 approachPoint = escortedNpc.GetWatsonApproachPoint(transform.position);
        if (!MoveWatsonTo(approachPoint))
        {
            escortedNpc = null;
            escortRoutine = null;
            yield break;
        }

        while (!HasWatsonReachedDestination())
            yield return null;

        watsonAgent.ResetPath();
        UpdateEscortRangeOrigin();
        if (MagnifierGlassController.IsWatsonGripActive)
            BeginPlacementPreview();

        escortedNpc.PlayApproachDialogue();
        yield return RotateWatsonTowards(escortedNpc.transform.position);
        yield return escortedNpc.RotateTowards(transform.position);
        yield return escortedNpc.RotateTowards(transform.position);
        escortRoutine = null;
    }

    private IEnumerator EscortTo(Vector3 npcDestination, Quaternion formationRotation)
    {
        if (escortedNpc == null)
        {
            escortRoutine = null;
            yield break;
        }

        if (!IsWithinEscortRange(npcDestination))
        {
            escortRoutine = null;
            yield break;
        }

        Vector3 direction = formationRotation * Vector3.forward;
        direction.y = 0f;
        direction.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        if (!TryGetSafeNpcDestination(npcDestination, out Vector3 safeNpcDestination))
        {
            escortRoutine = null;
            yield break;
        }

        formationRotation = GetInfluencedRotation(
            safeNpcDestination,
            formationRotation * Vector3.forward,
            out float influenceAmount);
        Vector3 influencedDirection = formationRotation * Vector3.forward;
        influencedDirection.y = 0f;
        if (influencedDirection.sqrMagnitude > 0.001f)
            right = Vector3.Cross(Vector3.up, influencedDirection.normalized).normalized;

        if (!TryGetPreferredWatsonDestination(
                safeNpcDestination,
                right,
                out Vector3 watsonDestinationForPair,
                out float watsonPathDistance))
        {
            escortRoutine = null;
            yield break;
        }

        float finalInfluenceAmount = Mathf.Clamp01(influenceAmount * finalFaceAmount);
        Vector3 npcToWatson = watsonDestinationForPair - safeNpcDestination;
        npcToWatson.y = 0f;
        if (npcToWatson.sqrMagnitude < 0.0001f)
            npcToWatson = -(formationRotation * Vector3.forward);
        npcToWatson.Normalize();

        Quaternion npcFacesWatson = Quaternion.LookRotation(npcToWatson, Vector3.up);
        Quaternion watsonFacesNpc = Quaternion.LookRotation(-npcToWatson, Vector3.up);
        Quaternion npcFinalRotation = Quaternion.Slerp(npcFacesWatson, formationRotation, finalInfluenceAmount);
        Quaternion watsonFinalRotation = Quaternion.Slerp(watsonFacesNpc, formationRotation, finalInfluenceAmount);

        defaultWatsonSpeed = watsonAgent.speed;
        float npcSpeed = escortedNpc.MovementSpeed > 0f ? escortedNpc.MovementSpeed : defaultWatsonSpeed;
        watson.SetTemporaryMovementSpeed(npcSpeed);

        if (!escortedNpc.MoveTo(safeNpcDestination))
        {
            RestoreWatsonSpeed();
            escortRoutine = null;
            yield break;
        }

        float npcPathDistance = GetPathDistance(escortedNpc.Agent, safeNpcDestination);
        bool watsonIsMoving = watsonPathDistance > npcPathDistance + formationTolerance;
        bool watsonHasCaughtUp = !watsonIsMoving;
        if (watsonIsMoving && !MoveWatsonTo(watsonDestinationForPair))
        {
            RestoreWatsonSpeed();
            escortedNpc.StopMoving();
            escortRoutine = null;
            yield break;
        }

        while (escortedNpc != null && !escortedNpc.HasReachedDestination())
        {
            float npcRemaining = Mathf.Max(0.01f, escortedNpc.Agent != null
                ? escortedNpc.Agent.remainingDistance
                : Vector3.Distance(escortedNpc.transform.position, safeNpcDestination));

            if (!watsonIsMoving && npcRemaining <= watsonPathDistance + formationTolerance)
            {
                watsonIsMoving = MoveWatsonTo(watsonDestinationForPair);
            }

            if (watsonIsMoving)
            {
                float watsonRemaining = Mathf.Max(0f, watsonAgent.remainingDistance);
                if (!watsonHasCaughtUp && watsonRemaining <= npcRemaining + formationTolerance)
                    watsonHasCaughtUp = true;

                float speedMultiplier = watsonHasCaughtUp
                    ? 1f
                    : Mathf.Clamp(watsonRemaining / npcRemaining, 1f, maxCatchUpSpeedMultiplier);
                float npcSpeedMultiplier = watsonHasCaughtUp
                    ? 1f
                    : Mathf.Clamp(npcRemaining / Mathf.Max(0.01f, watsonRemaining), minNpcEscortSpeedMultiplier, 1f);

                watson.SetTemporaryMovementSpeed(npcSpeed * speedMultiplier);
                escortedNpc.SetEscortMovementSpeed(npcSpeed * npcSpeedMultiplier);
            }

            yield return null;
        }

        if (escortedNpc == null)
        {
            RestoreWatsonSpeed();
            escortRoutine = null;
            yield break;
        }

        if (!watsonIsMoving && !MoveWatsonTo(watsonDestinationForPair))
        {
            RestoreWatsonSpeed();
            escortedNpc.StopMoving();
            escortRoutine = null;
            yield break;
        }

        watsonIsMoving = true;

        while (watsonIsMoving && !HasWatsonReachedDestination())
            yield return null;

        if (escortedNpc != null)
        {
            watsonAgent.ResetPath();
            escortedNpc.StopMoving();
            RestoreWatsonSpeed();
            escortedNpc.RestoreMovementSpeed();
            yield return FacePair(watsonFinalRotation, npcFinalRotation);
        }

        escortRoutine = null;
    }

    private void DismissEscort(bool playFarewell, bool useSherlockActiveFarewellDialogue = false)
    {
        if (escortRoutine != null)
            StopCoroutine(escortRoutine);

        escortRoutine = null;
        watsonAgent?.ResetPath();
        RestoreWatsonSpeed();
        CancelPlacementPreview();

        if (escortedNpc != null)
        {
            escortedNpc.StopMoving();
            escortedNpc.EndEscort();
            if (playFarewell)
            {
                if (useSherlockActiveFarewellDialogue)
                    escortedNpc.PlaySherlockActiveFarewellDialogue();
                else
                    escortedNpc.PlayFarewellDialogue();
            }
        }

        escortedNpc = null;
        hasEscortRangeOrigin = false;
        sherlockActiveEscortElapsed = 0f;
    }

    private bool MoveWatsonTo(Vector3 destination)
    {
        if (watsonAgent == null || !watsonAgent.isOnNavMesh ||
            !NavMeshWallGuard.TryGetClearPath(watsonAgent, destination, out NavMeshPath path))
            return false;

        watsonAgent.isStopped = false;
        watsonAgent.SetPath(path);
        return true;
    }

    private bool HasWatsonReachedDestination()
    {
        return watsonAgent != null && !watsonAgent.pathPending &&
               watsonAgent.remainingDistance <= watsonAgent.stoppingDistance + formationTolerance &&
               (!watsonAgent.hasPath || watsonAgent.velocity.sqrMagnitude < 0.01f);
    }

    private bool TryGetWatsonPathDistance(Vector3 destination, out float distance)
    {
        distance = 0f;
        if (watsonAgent == null || !NavMeshWallGuard.TryGetClearPath(watsonAgent, destination, out NavMeshPath path))
            return false;

        distance = GetPathDistance(path);
        return true;
    }

    private bool TryGetSafeNpcDestination(Vector3 requestedDestination, out Vector3 npcDestination)
    {
        float npcRadius = escortedNpc != null && escortedNpc.Agent != null ? escortedNpc.Agent.radius : 0.2f;
        return TryGetNavMeshEdgeSafePoint(requestedDestination, npcRadius + navMeshEdgeClearance, out npcDestination);
    }

    private bool TryGetPreferredWatsonDestination(
        Vector3 npcDestination,
        Vector3 right,
        out Vector3 watsonDestination,
        out float watsonPathDistance)
    {
        float npcRadius = escortedNpc != null && escortedNpc.Agent != null ? escortedNpc.Agent.radius : 0.2f;
        float clearance = watsonAgent.radius + npcRadius + navMeshEdgeClearance;
        Vector3 leftCandidate = default;
        Vector3 rightCandidate = default;
        float leftPathDistance = 0f;
        float rightPathDistance = 0f;
        bool hasLeftCandidate = TryGetNavMeshEdgeSafePoint(
            npcDestination - right * sideDistance,
            clearance,
            out leftCandidate) &&
            TryGetWatsonPathDistance(leftCandidate, out leftPathDistance);

        bool hasRightCandidate = TryGetNavMeshEdgeSafePoint(
            npcDestination + right * sideDistance,
            clearance,
            out rightCandidate) &&
            TryGetWatsonPathDistance(rightCandidate, out rightPathDistance);

        watsonDestination = default;
        watsonPathDistance = 0f;
        if (!hasLeftCandidate && !hasRightCandidate)
            return false;

        if (!hasRightCandidate || hasLeftCandidate && leftPathDistance <= rightPathDistance)
        {
            watsonDestination = leftCandidate;
            watsonPathDistance = leftPathDistance;
        }
        else
        {
            watsonDestination = rightCandidate;
            watsonPathDistance = rightPathDistance;
        }

        return true;
    }

    private bool TryGetNavMeshEdgeSafePoint(Vector3 requestedDestination, float requiredClearance, out Vector3 safeDestination)
    {
        safeDestination = requestedDestination;
        if (!NavMesh.SamplePosition(requestedDestination, out NavMeshHit sampledHit, placementNavMeshSampleRadius, NavMesh.AllAreas))
            return false;

        safeDestination = sampledHit.position;
        if (NavMesh.FindClosestEdge(safeDestination, out NavMeshHit edgeHit, NavMesh.AllAreas))
        {
            Vector3 awayFromEdge = safeDestination - edgeHit.position;
            awayFromEdge.y = 0f;
            if (awayFromEdge.sqrMagnitude < 0.001f)
            {
                awayFromEdge = -edgeHit.normal;
                awayFromEdge.y = 0f;
            }

            float edgeDistance = Vector3.Distance(
                new Vector3(safeDestination.x, 0f, safeDestination.z),
                new Vector3(edgeHit.position.x, 0f, edgeHit.position.z));

            if (awayFromEdge.sqrMagnitude > 0.001f && edgeDistance < requiredClearance)
            {
                Vector3 adjusted = safeDestination + awayFromEdge.normalized * (requiredClearance - edgeDistance);
                if (NavMesh.SamplePosition(adjusted, out NavMeshHit adjustedHit, placementNavMeshSampleRadius, NavMesh.AllAreas))
                    safeDestination = adjustedHit.position;
            }
        }

        return true;
    }

    private static float GetPathDistance(NavMeshAgent agent, Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh)
            return Vector3.Distance(agent != null ? agent.transform.position : destination, destination);

        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(destination, path))
            return Vector3.Distance(agent.transform.position, destination);

        return GetPathDistance(path);
    }

    private static float GetPathDistance(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return 0f;

        float distance = 0f;
        for (int index = 1; index < path.corners.Length; index++)
            distance += Vector3.Distance(path.corners[index - 1], path.corners[index]);

        return distance;
    }

    private IEnumerator RotateWatsonTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        watsonAgent.updateRotation = false;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                watsonRotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = targetRotation;
        watsonAgent.updateRotation = true;
    }

    private IEnumerator FacePair(Quaternion watsonTarget, Quaternion npcTarget)
    {
        if (escortedNpc == null)
            yield break;

        while (Quaternion.Angle(transform.rotation, watsonTarget) > 0.5f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                watsonTarget,
                watsonRotationSpeed * Time.deltaTime);
            yield return null;
        }

        transform.rotation = watsonTarget;
        yield return escortedNpc.RotateToRotation(npcTarget);
    }

    private bool TryGetPlacementPoint(out Vector3 point)
    {
        point = default;
        if (Camera.main == null || Mouse.current == null)
            return false;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayerMask, triggerInteraction) ||
            !NavMesh.SamplePosition(groundHit.point, out NavMeshHit navMeshHit, placementNavMeshSampleRadius, NavMesh.AllAreas))
            return false;

        point = navMeshHit.position;
        return true;
    }

    private void UpdatePlacementPreview()
    {
        if (TryGetPlacementPoint(out Vector3 point))
            placementDestination = point;

        placementRotation = GetInfluencedRotation(placementDestination, GetPlacementFallbackDirection(), out _);
        isPlacementWithinRange = IsWithinEscortRange(placementDestination);
        float visibilityMultiplier = GetRangeVisibilityMultiplier(placementDestination);

        if (isPlacementWithinRange)
        {
            NavMeshDestinationMarker marker = NavMeshDestinationMarker.GetOrCreate();
            marker.ShowPreview(placementDestination, placementRotation);
            marker.SetPreviewVisibilityMultiplier(visibilityMultiplier);
            marker.SetPreviewColor(escortRingColor);
        }
        else
            NavMeshDestinationMarker.HideCurrent();

        if (placementPreview != null)
        {
            placementPreview.transform.SetPositionAndRotation(
                placementDestination + placementRotation * placementPreviewOffset,
                placementRotation);
            placementPreview.SetActive(isPlacementWithinRange);
            if (isPlacementWithinRange)
                ApplyPlacementPreviewVisibility(visibilityMultiplier);
        }
    }

    private void BeginPlacementPreview()
    {
        if (escortedNpc == null)
            return;

        // Every new escort step begins from Watson's current location.
        UpdateEscortRangeOrigin();

        if (TryGetPlacementPoint(out Vector3 point))
            placementDestination = point;
        else
            placementDestination = transform.position;

        isPlacingDestination = true;
        CreatePlacementPreview();
        UpdatePlacementPreview();
    }

    private void CommitPlacementPreview()
    {
        if (!isPlacingDestination || !isPlacementWithinRange)
            return;

        Vector3 destination = placementDestination;
        Quaternion rotation = placementRotation;
        CancelPlacementPreview();
        escortedNpc?.PlayDestinationDialogue();
        escortRoutine = StartCoroutine(EscortTo(destination, rotation));
    }

    private Quaternion GetInfluencedRotation(Vector3 position, Vector3 fallbackDirection, out float influenceAmount)
    {
        Vector3 combinedDirection = Vector3.zero;
        float totalWeight = 0f;
        WatsonEscortInfluencePoint[] influencePoints = escortedNpc != null
            ? escortedNpc.InfluencePoints
            : null;

        if ((influencePoints == null || influencePoints.Length == 0) &&
            (escortedNpc == null || escortedNpc.UseGlobalInfluencePointsWhenListEmpty))
        {
            influencePoints = FindObjectsByType<WatsonEscortInfluencePoint>(FindObjectsSortMode.None);
        }

        if (influencePoints == null)
            influencePoints = System.Array.Empty<WatsonEscortInfluencePoint>();

        foreach (WatsonEscortInfluencePoint influencePoint in influencePoints)
        {
            if (influencePoint != null && influencePoint.TryGetInfluence(position, out Vector3 direction, out float weight))
            {
                combinedDirection += direction * weight;
                totalWeight += weight;
            }
        }

        Vector3 baseDirection = fallBackToMovementDirection
            ? fallbackDirection
            : escortedNpc != null ? escortedNpc.transform.forward : transform.forward;
        baseDirection.y = 0f;
        if (baseDirection.sqrMagnitude < 0.0001f)
            baseDirection = escortedNpc != null ? escortedNpc.transform.forward : transform.forward;
        baseDirection.Normalize();

        combinedDirection.y = 0f;
        if (combinedDirection.sqrMagnitude < 0.0001f)
        {
            influenceAmount = 0f;
            return Quaternion.LookRotation(baseDirection, Vector3.up);
        }

        // Keep the movement direction at the edge of an influence range, then gradually blend toward it.
        influenceAmount = Mathf.Clamp01(totalWeight);
        Vector3 influenceDirection = combinedDirection.normalized;
        Vector3 blendedDirection = Vector3.Slerp(baseDirection, influenceDirection, influenceAmount);
        return Quaternion.LookRotation(blendedDirection.normalized, Vector3.up);
    }

    private Vector3 GetPlacementFallbackDirection()
    {
        Vector3 direction = escortedNpc != null
            ? placementDestination - escortedNpc.transform.position
            : placementDestination - transform.position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
    }

    private void CreatePlacementPreview()
    {
        DestroyPlacementPreview();
        GameObject prefab = escortedNpc != null ? escortedNpc.PlacementPreviewPrefab : null;
        if (prefab == null)
            return;

        placementPreview = Instantiate(prefab);
        placementPreview.name = $"{prefab.name}_EscortPreview";
        placementPreview.SetActive(true);
        foreach (Collider previewCollider in placementPreview.GetComponentsInChildren<Collider>(true))
            previewCollider.enabled = false;

        // The preview controls its own visibility so a copied interaction fader cannot overwrite it.
        foreach (InteractionShaderFader previewFader in placementPreview.GetComponentsInChildren<InteractionShaderFader>(true))
            previewFader.enabled = false;
    }

    private void CancelPlacementPreview()
    {
        isPlacingDestination = false;
        NavMeshDestinationMarker.HideCurrent();
        DestroyPlacementPreview();
    }

    private void DestroyPlacementPreview()
    {
        if (placementPreview != null)
            Destroy(placementPreview);
        placementPreview = null;
    }

    private bool IsWithinEscortRange(Vector3 destination)
    {
        if (!hasEscortRangeOrigin)
            return true;

        Vector3 flatDestination = destination;
        flatDestination.y = 0f;
        return Vector3.Distance(escortRangeOrigin, flatDestination) <= ActiveEscortRange;
    }

    private void UpdateEscortRangeOrigin()
    {
        escortRangeOrigin = transform.position;
        escortRangeOrigin.y = 0f;
        hasEscortRangeOrigin = true;
    }

    private float GetRangeVisibilityMultiplier(Vector3 destination)
    {
        float escortRange = ActiveEscortRange;
        if (!hasEscortRangeOrigin || escortRange <= 0f)
            return 1f;

        Vector3 flatDestination = destination;
        flatDestination.y = 0f;
        float normalizedDistance = Vector3.Distance(escortRangeOrigin, flatDestination) / escortRange;
        return 1f - Mathf.InverseLerp(rangePreviewFadeStart, 1f, normalizedDistance);
    }

    private void ApplyPlacementPreviewVisibility(float visibilityMultiplier)
    {
        if (placementPreview == null || placementPreviewPropertyBlock == null)
            return;

        int visibilityPropertyId = Shader.PropertyToID(placementPreviewVisibilityProperty);
        float visibility = placementPreviewBaseVisibility * Mathf.Clamp01(visibilityMultiplier);
        foreach (Renderer renderer in placementPreview.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null || !material.HasProperty(visibilityPropertyId) ||
                    (placementPreviewShader != null && material.shader != placementPreviewShader))
                    continue;

                placementPreviewPropertyBlock.Clear();
                placementPreviewPropertyBlock.SetFloat(visibilityPropertyId, visibility);
                renderer.SetPropertyBlock(placementPreviewPropertyBlock, materialIndex);
            }
        }
    }

    private void RestoreWatsonSpeed()
    {
        watson?.ClearTemporaryMovementSpeed();

        if (watsonAgent != null && defaultWatsonSpeed > 0f)
            watsonAgent.speed = defaultWatsonSpeed;
    }
}
