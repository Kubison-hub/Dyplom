using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public abstract class Lvl3LoupeWallPuzzleBase : Lvl3InteractionDialogueBase
{
    [Header("Loupe Pattern")]
    [SerializeField] private Collider wallCollider;
    [Tooltip("Optional dedicated collider used only by the loupe. It stays active when a vision reveal disables the real wall collider.")]
    [SerializeField] private Collider loupeCollider;
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.005f)] private float traceRadius = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float requiredProgress = 0.9f;

    [Header("Pattern Appearance")]
    [SerializeField] private string undiscoveredLayerName = "Clues";
    [SerializeField] private string discoveredLayerName = "WorldText";
    [SerializeField] private Color hiddenSegmentColor = new Color(0.08f, 0.22f, 0.12f, 0.35f);
    [SerializeField] private Color revealedSegmentColor = new Color(0.48f, 0.78f, 0.55f, 1f);
    [SerializeField, Min(0.01f)] private float revealDuration = 0.2f;
    [SerializeField] private bool deactivateSegmentsOnSolved = true;

    [Header("Interactables To Enable On Solved")]
    [Tooltip("Assign GameObjects with an Interactable component. Their GameObjects stay unchanged; only isInteractableActive becomes true.")]
    [SerializeField] private GameObject[] activateOnSolved;

    private readonly List<LineRenderer> segments = new List<LineRenderer>();
    private readonly HashSet<LineRenderer> revealedSegments = new HashSet<LineRenderer>();
    private Interactable interactable;
    private int undiscoveredLayer = -1;
    private int discoveredLayer = -1;
    private bool solved;

    protected abstract InteractionType PuzzleInteractionType { get; }
    protected virtual bool RotateWatsonOnSolved => false;
    protected virtual bool UseDedicatedLoupeCollider => false;
    protected virtual bool IsLoupePuzzleEnabled => true;
    protected virtual bool CanTraceLoupePattern => true;
    protected virtual bool ShouldShowLoupePattern => true;
    protected virtual bool ShowPatternOnlyInEagleVision => false;
    protected virtual float WatsonApproachDistanceOnSolved => 0f;
    protected virtual float WatsonApproachSpeedMultiplierOnSolved => 1f;
    protected virtual float WatsonRotationSpeedMultiplierOnSolved => 1f;
    protected virtual void OnPuzzleSolved() { }

    protected void SetupPuzzle()
    {
        SetupInteractable(PuzzleInteractionType);
    }

    protected virtual void Start()
    {
        SetupPuzzle();

        interactable = GetComponent<Interactable>();
        if (wallCollider == null)
            wallCollider = GetComponent<Collider>();

        if (loupeCollider == null)
        {
            loupeCollider = UseDedicatedLoupeCollider
                ? CreateDedicatedLoupeCollider()
                : wallCollider;
        }

        if (segmentsParent == null)
            segmentsParent = transform.Find("MaskCanvas");

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        undiscoveredLayer = LayerMask.NameToLayer(undiscoveredLayerName);
        discoveredLayer = LayerMask.NameToLayer(discoveredLayerName);

        CacheSegments();
        UpdatePatternVisibility();
    }

    private void Update()
    {
        UpdatePatternVisibility();

        if (solved || !IsLoupePuzzleEnabled || !CanTraceLoupePattern || magnifier == null || loupeCollider == null || segments.Count == 0)
            return;

        if (!magnifier.TryGetActiveLoupeHit(out RaycastHit hit) || !IsWallHit(hit.collider))
            return;

        foreach (LineRenderer segment in segments)
        {
            if (revealedSegments.Contains(segment) || !IsPointNearSegment(hit.point, segment))
                continue;

            revealedSegments.Add(segment);
            StartCoroutine(RevealSegment(segment));
        }

        if (Progress >= requiredProgress)
            CompletePuzzle();
    }

    public void PerformInteraction(PlayerController player)
    {
        // The pattern is intentionally solved with the loupe rather than a normal click.
    }

    private float Progress => segments.Count == 0 ? 0f : (float)revealedSegments.Count / segments.Count;

    private void UpdatePatternVisibility()
    {
        if (segmentsParent == null)
            return;

        bool eagleVisionActive = EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
        bool shouldBeVisible = !solved &&
                               IsLoupePuzzleEnabled &&
                               ShouldShowLoupePattern &&
                               (!ShowPatternOnlyInEagleVision || eagleVisionActive);
        if (segmentsParent.gameObject.activeSelf != shouldBeVisible)
            segmentsParent.gameObject.SetActive(shouldBeVisible);
    }

    private void CacheSegments()
    {
        if (segmentsParent == null)
        {
            Debug.LogWarning($"{name}: Assign MaskCanvas to Segments Parent.", this);
            return;
        }

        segments.AddRange(segmentsParent.GetComponentsInChildren<LineRenderer>(true));
        segments.RemoveAll(segment => segment == null || segment.positionCount < 2);

        foreach (LineRenderer segment in segments)
        {
            SetSegmentColor(segment, hiddenSegmentColor);
            SetSegmentLayer(segment, undiscoveredLayer);
        }
    }

    private bool IsWallHit(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        Transform hitTransform = hitCollider.transform;
        Transform loupeTransform = loupeCollider.transform;

        return hitCollider == loupeCollider ||
               hitTransform.IsChildOf(loupeTransform) ||
               loupeTransform.IsChildOf(hitTransform);
    }

    private bool IsPointNearSegment(Vector3 worldPoint, LineRenderer segment)
    {
        for (int index = 0; index < segment.positionCount - 1; index++)
        {
            Vector3 start = GetWorldPoint(segment, index);
            Vector3 end = GetWorldPoint(segment, index + 1);

            if (DistanceToLineSegment(worldPoint, start, end) <= traceRadius)
                return true;
        }

        return segment.loop && DistanceToLineSegment(
            worldPoint,
            GetWorldPoint(segment, segment.positionCount - 1),
            GetWorldPoint(segment, 0)) <= traceRadius;
    }

    private static Vector3 GetWorldPoint(LineRenderer segment, int index)
    {
        Vector3 point = segment.GetPosition(index);
        return segment.useWorldSpace ? point : segment.transform.TransformPoint(point);
    }

    private static float DistanceToLineSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < 0.000001f)
            return Vector3.Distance(point, start);

        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * t);
    }

    private IEnumerator RevealSegment(LineRenderer segment)
    {
        Color startColor = segment.startColor;
        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetSegmentColor(segment, Color.Lerp(startColor, revealedSegmentColor, elapsed / revealDuration));
            yield return null;
        }

        SetSegmentColor(segment, revealedSegmentColor);
        SetSegmentLayer(segment, discoveredLayer);
    }

    private void CompletePuzzle()
    {
        if (solved)
            return;

        solved = true;
        interactable?.MarkCompleted();
        OnPuzzleSolved();
        PlayCompletionDialogue();

        if (RotateWatsonOnSolved)
        {
            WatsonCompanionController.Instance?.FocusInteraction(
                transform,
                WatsonApproachDistanceOnSolved,
                WatsonApproachSpeedMultiplierOnSolved,
                WatsonRotationSpeedMultiplierOnSolved);
        }

        foreach (GameObject target in activateOnSolved)
        {
            if (target == null)
                continue;

            Interactable targetInteractable = target.GetComponent<Interactable>();
            if (targetInteractable == null)
            {
                Debug.LogWarning($"{name}: '{target.name}' needs an Interactable component to be enabled on puzzle completion.", target);
                continue;
            }

            targetInteractable.isInteractableActive = true;
        }

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.allowQuestionFXWhenInactive = false;
            interactable.SetQuestionFXEagleVisionState(false);

            if (interactable.interactiveShader != null)
                interactable.interactiveShader.SetActive(false);
        }

        if (deactivateSegmentsOnSolved)
            segmentsParent?.gameObject.SetActive(false);

        if (wallCollider != null)
            wallCollider.enabled = false;

        if (loupeCollider != null && loupeCollider != wallCollider)
            loupeCollider.enabled = false;
    }

    private Collider CreateDedicatedLoupeCollider()
    {
        Bounds bounds = wallCollider != null
            ? wallCollider.bounds
            : GetSegmentsBounds();

        if (bounds.size.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning($"{name}: Cannot create a loupe collider because the wall has no valid bounds.", this);
            return wallCollider;
        }

        GameObject colliderObject = new GameObject("LoupeRaycastCollider");
        colliderObject.transform.SetParent(transform, true);
        colliderObject.transform.position = bounds.center;
        colliderObject.transform.rotation = Quaternion.identity;
        colliderObject.layer = gameObject.layer;

        BoxCollider dedicatedCollider = colliderObject.AddComponent<BoxCollider>();
        dedicatedCollider.size = bounds.size + Vector3.one * 0.02f;
        return dedicatedCollider;
    }

    private Bounds GetSegmentsBounds()
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;

        foreach (LineRenderer segment in segments)
        {
            if (segment == null)
                continue;

            if (!hasBounds)
            {
                bounds = segment.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(segment.bounds);
            }
        }

        return bounds;
    }

    private static void SetSegmentColor(LineRenderer segment, Color color)
    {
        segment.startColor = color;
        segment.endColor = color;
    }

    private static void SetSegmentLayer(LineRenderer segment, int layer)
    {
        if (layer >= 0)
            segment.gameObject.layer = layer;
    }
}
