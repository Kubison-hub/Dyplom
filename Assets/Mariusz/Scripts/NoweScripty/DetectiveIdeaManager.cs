using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class DetectiveIdeaManager : MonoBehaviour
{
    public static DetectiveIdeaManager Instance { get; private set; }

    // Subscribers can consume an empty click while Vision Eye is active.
    public event Func<bool> OnEmptyVisionClick;

    [Header("Puzzle")]
    public DetectiveSequencePuzzle activePuzzle;

    [Header("Raycast")]
    public LayerMask ideaLayerMask = ~0;
    public float raycastDistance = 100f;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("Lines")]
    public Material lineMaterial;
    public Color previewLineColor = new Color(0.62f, 0.82f, 0.64f, 0.95f);
    public Color acceptedLineColor = new Color(0.42f, 0.68f, 0.48f, 1f);
    public Color rejectedLineColor = new Color(0.16f, 0.30f, 0.20f, 0.8f);
    public float lineWidth = 0.035f;
    [Range(0.1f, 1f)] public float rejectedLineWidthMultiplier = 0.5f;
    public float pointerDistance = 8f;
    public string lineLayerName = "WorldText";

    [Header("Dragged Line Description")]
    public bool showDraggedLineDescription = true;
    public Color lineDescriptionColor = new Color(0.68f, 0.88f, 0.7f, 1f);
    [Min(0.01f)] public float lineDescriptionFontSize = 0.16f;
    public Vector3 lineDescriptionScreenOffset = new Vector3(0.2f, 0.1f, 0f);

    [Header("Visibility")]
    public bool showDiscoveredPointsWhileActive = true;
    [Min(0f)] public float worldTextRevealDelay = 0f;
    [Min(0.1f)] public float magnifierDiscoveryDuration = 1.5f;

    [Header("Vision Eye")]
    [Min(0f)] public float solvedVisionHoldDuration = 5f;

    [Header("Player Camera Look At")]
    [SerializeField] private bool useSequenceLookAt = true;
    [SerializeField, Range(0f, 1f)] private float sequenceLookAtLineInfluence = 0.85f;
    [SerializeField, Min(0.01f)] private float sequenceLookAtSmoothTime = 0.25f;
    [SerializeField, Min(0f)] private float sequenceLookAtMaxDistanceFromPlayer = 3f;

    [Header("Sherlock Head Rig During Line Drag")]
    [Tooltip("Rig with Sherlock's Multi-Aim Constraint for the head.")]
    [SerializeField] private Rig headRig;
    [Tooltip("Assign the Transform used as the source target by the Multi-Aim Constraint.")]
    [SerializeField] private Transform headRigLookTarget;
    [SerializeField] private bool headRigOnlyWhenSherlockIsActive = true;
    [SerializeField, Min(0.01f)] private float headRigBlendDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float headRigTargetSmoothTime = 0.08f;

    private readonly List<DetectiveIdeaPoint> visiblePoints = new List<DetectiveIdeaPoint>();
    private readonly List<LineRenderer> acceptedLines = new List<LineRenderer>();
    private readonly List<LineRenderer> activeSequenceLines = new List<LineRenderer>();
    private readonly List<LineRenderer> rejectedLines = new List<LineRenderer>();

    private DetectiveIdeaPoint hoveredPoint;
    private DetectiveIdeaPoint dragSource;
    private LineRenderer previewLine;
    private TextMeshPro previewLineDescription;
    private bool puzzleCompleted;
    private bool wasDetectiveVisionActive;
    private float detectiveVisionActivatedAt;
    private Transform sequenceLookAtTarget;
    private CameraController sequenceLookAtCameraController;
    private Vector3 sequenceLookAtVelocity;
    private bool resetSequenceLookAtOnNextUse = true;
    private bool holdSolvedSequenceLookAt;
    private Coroutine solvedSequenceLookAtCoroutine;
    private DetectiveIdeaPoint magnifierDiscoveryCandidate;
    private float magnifierDiscoveryStartedAt;
    private int sequenceProgressIndex;
    private bool connectionsEnabled = true;
    private bool puzzleCompletionEnabled = true;
    private Vector3 headRigTargetVelocity;
    private bool resetHeadRigTargetOnNextDrag = true;

    private void Awake()
    {
        lineLayerName = "WorldText";
        previewLineColor = new Color(0.62f, 0.82f, 0.64f, 0.95f);
        acceptedLineColor = new Color(0.42f, 0.68f, 0.48f, 1f);

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        RestoreSequenceLookAt();

        if (headRig != null)
            headRig.weight = 0f;

        if (sequenceLookAtTarget != null)
            Destroy(sequenceLookAtTarget.gameObject);
    }

    private void LateUpdate()
    {
        UpdateHeadRigForDraggedLine();
    }

    private void SetRejectedLinesVisible(bool visible)
    {
        for (int i = rejectedLines.Count - 1; i >= 0; i--)
        {
            LineRenderer line = rejectedLines[i];
            if (line == null)
            {
                rejectedLines.RemoveAt(i);
                continue;
            }

            line.gameObject.SetActive(visible);
        }
    }

    private void Update()
    {
        if (!IsSherlockActive())
        {
            wasDetectiveVisionActive = false;
            ClearHover();
            CancelDrag();
            HideVisiblePoints();
            SetAcceptedLinesVisible(false);
            SetRejectedLinesVisible(false);
            return;
        }

        if (puzzleCompleted)
        {
            if (holdSolvedSequenceLookAt)
                UpdateSequenceLookAt(GetPointerWorldPosition());

            return;
        }

        bool detectiveVisionActive = IsDetectiveVisionActive();
        if (!detectiveVisionActive)
        {
            wasDetectiveVisionActive = false;
            ClearHover();
            CancelDrag();
            HideVisiblePoints();
            SetAcceptedLinesVisible(false);
            SetRejectedLinesVisible(false);
            return;
        }

        if (!wasDetectiveVisionActive)
        {
            wasDetectiveVisionActive = true;
            detectiveVisionActivatedAt = Time.unscaledTime;
            HideVisiblePoints();
            SetAcceptedLinesVisible(false);
            SetRejectedLinesVisible(false);
        }

        if (!IsWorldTextRevealReady())
        {
            ClearHover();
            SetAcceptedLinesVisible(false);
            SetRejectedLinesVisible(false);
            HideVisiblePoints();
            return;
        }

        SetAcceptedLinesVisible(true);
        SetRejectedLinesVisible(true);
        SetDiscoveredPointsVisible(true);
        UpdateHover();
        UpdatePreviewLine();
        KeepHoveredTitleVisible();
    }

    public bool TryHandlePointerPress()
    {
        if (!IsSherlockActive())
            return false;

        if (puzzleCompleted)
            return false;

        if (!connectionsEnabled)
            return false;

        if (!IsDetectiveVisionActive())
            return false;

        if (!IsWorldTextRevealReady())
            return false;

        DetectiveIdeaPoint point = RaycastIdeaPoint();
        if (point == null)
        {
            if (dragSource != null)
            {
                CancelDrag();
                return true;
            }

            return TryConsumeEmptyVisionClick();
        }

        if (!point.IsDiscovered)
            return false;

        if (dragSource != null)
        {
            FinishConnection(point);
            return true;
        }

        BeginDrag(point);
        return true;
    }

    public bool IsDraggingIdea()
    {
        return dragSource != null;
    }

    private bool TryConsumeEmptyVisionClick()
    {
        if (OnEmptyVisionClick == null)
            return false;

        foreach (Delegate listener in OnEmptyVisionClick.GetInvocationList())
        {
            if (((Func<bool>)listener).Invoke())
                return true;
        }

        return false;
    }

    public void BeginPuzzleSession(
        DetectiveSequencePuzzle puzzle,
        bool enableConnections = false,
        bool enablePuzzleCompletion = true)
    {
        CancelDrag();
        ClearHover();
        DestroySessionLines();
        HideVisiblePoints();
        visiblePoints.Clear();

        if (solvedSequenceLookAtCoroutine != null)
            StopCoroutine(solvedSequenceLookAtCoroutine);

        solvedSequenceLookAtCoroutine = null;
        holdSolvedSequenceLookAt = false;
        RestoreSequenceLookAt();

        activePuzzle = puzzle;
        puzzleCompleted = false;
        sequenceProgressIndex = 0;
        connectionsEnabled = enableConnections;
        puzzleCompletionEnabled = enablePuzzleCompletion;
        wasDetectiveVisionActive = false;
        detectiveVisionActivatedAt = Time.unscaledTime;
    }

    public void SetConnectionsEnabled(bool enabled)
    {
        connectionsEnabled = enabled;

        if (!enabled)
            CancelDrag();
    }

    public void SetPuzzleCompletionEnabled(bool enabled)
    {
        puzzleCompletionEnabled = enabled;

        if (puzzleCompletionEnabled &&
            activePuzzle != null &&
            !activePuzzle.IsSolved &&
            sequenceProgressIndex >= activePuzzle.correctSequence.Count)
        {
            activePuzzle.SolveFromOrderedSequence();
            CompletePuzzle();
        }
    }

    public bool IsPointerOverDiscoveredIdeaPoint()
    {
        if (!IsSherlockActive())
            return false;

        if (!IsDetectiveVisionActive())
            return false;

        DetectiveIdeaPoint point = RaycastIdeaPoint();
        return point != null && point.IsDiscovered;
    }

    private bool IsDetectiveVisionActive()
    {
        return EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
    }

    private bool IsSherlockActive()
    {
        return SwitchCharacter.Instance == null ||
               SwitchCharacter.Instance.activePlayerIndex == 0;
    }

    public void DiscoverPoint(DetectiveIdeaPoint point)
    {
        if (point == null || puzzleCompleted)
            return;

        point.MarkDiscovered();
        RememberVisiblePoint(point);

        bool shouldBeVisible = IsDetectiveVisionActive() &&
                               IsWorldTextRevealReady() &&
                               showDiscoveredPointsWhileActive;
        point.SetVisible(shouldBeVisible);
    }

    private bool IsMagnifierActive()
    {
        return EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.IsMagnifierHeld();
    }

    private bool IsWorldTextRevealReady()
    {
        return wasDetectiveVisionActive &&
               Time.unscaledTime - detectiveVisionActivatedAt >= worldTextRevealDelay;
    }

    private void BeginDrag(DetectiveIdeaPoint point)
    {
        dragSource = point;
        dragSource.MarkDiscovered();
        dragSource.SetVisible(true);
        dragSource.SetHovered(true);
        RememberVisiblePoint(dragSource);
        dragSource.ShowIdeaText();

        previewLine = CreateLine(previewLineColor);
        previewLine.positionCount = 2;
        previewLine.SetPosition(0, dragSource.AnchorPosition);
        previewLine.SetPosition(1, dragSource.AnchorPosition);
        CreatePreviewLineDescription(dragSource);
    }

    private void FinishConnection(DetectiveIdeaPoint target)
    {
        if (target != null)
        {
            target.MarkDiscovered();
            target.SetVisible(true);
            RememberVisiblePoint(target);
        }

        DetectiveSequencePuzzle.ConnectionResult result = DetectiveSequencePuzzle.ConnectionResult.Wrong;
        bool isExpectedSequenceStep = IsExpectedSequenceStep(dragSource, target);
        if (activePuzzle != null && target != null && target != dragSource)
        {
            result = activePuzzle.TryConnect(dragSource, target);
        }

        bool advancesSequence = isExpectedSequenceStep &&
                                (result == DetectiveSequencePuzzle.ConnectionResult.Correct ||
                                 result == DetectiveSequencePuzzle.ConnectionResult.AlreadySolved);

        if (advancesSequence)
        {
            previewLine.startColor = acceptedLineColor;
            previewLine.endColor = acceptedLineColor;
            previewLine.SetPosition(0, dragSource.AnchorPosition);
            previewLine.SetPosition(1, target.AnchorPosition);
            acceptedLines.Add(previewLine);

            activeSequenceLines.Add(previewLine);

            previewLine = null;
            sequenceProgressIndex++;

            if (activePuzzle != null &&
                puzzleCompletionEnabled &&
                sequenceProgressIndex >= activePuzzle.correctSequence.Count)
            {
                activePuzzle.SolveFromOrderedSequence();
                CompletePuzzle();
                return;
            }

            ContinueDragFrom(target);
            return;
        }

        // Every non-sequential connection remains visible as a dark clue trail.
        KeepRejectedLine(target);
        ClearActiveSequenceLines();
        sequenceProgressIndex = 0;

        if (dragSource != null && dragSource != hoveredPoint)
            dragSource.SetHovered(false);

        dragSource = null;
        DestroyPreviewLineDescription();
        RestoreSequenceLookAt();
    }

    private bool IsExpectedSequenceStep(DetectiveIdeaPoint from, DetectiveIdeaPoint to)
    {
        if (activePuzzle == null || from == null || to == null ||
            sequenceProgressIndex < 0 || sequenceProgressIndex >= activePuzzle.correctSequence.Count)
            return false;

        DetectiveSequencePuzzle.IdeaConnectionStep expectedStep = activePuzzle.correctSequence[sequenceProgressIndex];
        return expectedStep != null && expectedStep.from == from && expectedStep.to == to;
    }

    private void ContinueDragFrom(DetectiveIdeaPoint point)
    {
        if (dragSource != null && dragSource != hoveredPoint)
            dragSource.SetHovered(false);

        dragSource = point;
        dragSource.SetVisible(true);
        dragSource.SetHovered(true);
        RememberVisiblePoint(dragSource);

        previewLine = CreateLine(previewLineColor);
        previewLine.positionCount = 2;
        previewLine.SetPosition(0, dragSource.AnchorPosition);
        previewLine.SetPosition(1, GetPointerWorldPosition());
        CreatePreviewLineDescription(dragSource);
    }

    private void CancelDrag()
    {
        if (dragSource != null && dragSource != hoveredPoint)
            dragSource.SetHovered(false);

        dragSource = null;
        DestroyPreviewLine();
        DestroyPreviewLineDescription();
        ClearActiveSequenceLines();
        sequenceProgressIndex = 0;
        RestoreSequenceLookAt();
    }

    private void UpdateHover()
    {
        DetectiveIdeaPoint newHover = RaycastIdeaPoint();
        bool hoverChanged = newHover != hoveredPoint;

        if (hoverChanged)
        {
            ClearHover();
            hoveredPoint = newHover;
        }

        if (hoveredPoint == null)
            return;

        if (hoveredPoint.IsDiscovered)
        {
            hoveredPoint.SetVisible(true);
            hoveredPoint.SetHovered(true);
            if (hoverChanged)
                hoveredPoint.ShowIdeaTitle();
            RememberVisiblePoint(hoveredPoint);
            return;
        }

        if (!hoveredPoint.CanDiscoverWithMagnifier || !IsMagnifierActive())
        {
            ClearHover();
            return;
        }

        UpdateMagnifierDiscovery(hoveredPoint);
    }

    private void ClearHover()
    {
        CancelMagnifierDiscovery(hoveredPoint);

        if (hoveredPoint != null && hoveredPoint != dragSource)
            hoveredPoint.SetHovered(false);

        if (hoveredPoint != null)
            hoveredPoint.ClearIdeaTitle();

        if (hoveredPoint != null && (!hoveredPoint.IsDiscovered || !showDiscoveredPointsWhileActive))
            hoveredPoint.SetVisible(false);

        hoveredPoint = null;
    }

    private void UpdateMagnifierDiscovery(DetectiveIdeaPoint point)
    {
        if (magnifierDiscoveryCandidate != point)
        {
            CancelMagnifierDiscovery(null);
            magnifierDiscoveryCandidate = point;
            magnifierDiscoveryStartedAt = Time.unscaledTime;
        }

        float progress = Mathf.Clamp01(
            (Time.unscaledTime - magnifierDiscoveryStartedAt) / magnifierDiscoveryDuration);
        point.SetDiscoveryPreview(progress);

        if (progress < 1f)
            return;

        magnifierDiscoveryCandidate = null;
        DiscoverPoint(point);
        point.SetHovered(true);
        point.ShowIdeaText();
        RememberVisiblePoint(point);
    }

    private void CancelMagnifierDiscovery(DetectiveIdeaPoint point)
    {
        if (magnifierDiscoveryCandidate == null ||
            point != null && magnifierDiscoveryCandidate != point)
            return;

        magnifierDiscoveryCandidate.CancelDiscoveryPreview();
        magnifierDiscoveryCandidate = null;
    }

    private void KeepHoveredTitleVisible()
    {
        if (hoveredPoint == null || PlayerTopText.Instance == null || PlayerTopText.Instance.sherlockTopText == null)
            return;

        if (string.IsNullOrEmpty(PlayerTopText.Instance.sherlockTopText.text))
            hoveredPoint.ShowIdeaTitle();
    }

    private void UpdatePreviewLine()
    {
        if (dragSource == null || previewLine == null)
            return;

        Vector3 pointerPosition = GetPointerWorldPosition();
        previewLine.SetPosition(0, dragSource.AnchorPosition);
        previewLine.SetPosition(1, pointerPosition);
        UpdatePreviewLineDescription();

        if (useSequenceLookAt)
            UpdateSequenceLookAt(pointerPosition);
        else
            RestoreSequenceLookAt();
    }

    private void UpdateHeadRigForDraggedLine()
    {
        if (headRig == null)
            return;

        bool sherlockIsActive = SwitchCharacter.Instance == null ||
                                SwitchCharacter.Instance.activePlayerIndex == 0;
        bool shouldUseHeadRig = dragSource != null &&
                                 previewLine != null &&
                                 (!headRigOnlyWhenSherlockIsActive || sherlockIsActive);

        float targetWeight = shouldUseHeadRig ? 1f : 0f;
        headRig.weight = Mathf.MoveTowards(
            headRig.weight,
            targetWeight,
            Time.unscaledDeltaTime / headRigBlendDuration);

        if (!shouldUseHeadRig || headRigLookTarget == null)
        {
            if (!shouldUseHeadRig)
                resetHeadRigTargetOnNextDrag = true;

            return;
        }

        Vector3 lineHeadPosition = previewLine.GetPosition(1);
        if (resetHeadRigTargetOnNextDrag)
        {
            headRigLookTarget.position = lineHeadPosition;
            headRigTargetVelocity = Vector3.zero;
            resetHeadRigTargetOnNextDrag = false;
            return;
        }

        headRigLookTarget.position = Vector3.SmoothDamp(
            headRigLookTarget.position,
            lineHeadPosition,
            ref headRigTargetVelocity,
            headRigTargetSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    private void UpdateSequenceLookAt(Vector3 cursorPosition)
    {
        CameraController cameraController = GetActiveCameraController();
        if (cameraController == null)
            return;

        if (sequenceLookAtCameraController != cameraController)
        {
            RestoreSequenceLookAt();
            sequenceLookAtCameraController = cameraController;
        }

        if (sequenceLookAtTarget == null)
        {
            GameObject targetObject = new GameObject("DetectiveIdeaSequenceLookAtTarget");
            sequenceLookAtTarget = targetObject.transform;
        }

        if (resetSequenceLookAtOnNextUse)
        {
            sequenceLookAtTarget.position = cameraController.GetLookAtPosition();
            sequenceLookAtVelocity = Vector3.zero;
            resetSequenceLookAtOnNextUse = false;
        }

        Vector3 followPosition = cameraController.GetFollowPosition();
        Vector3 desiredLookAt = Vector3.Lerp(
            followPosition,
            cursorPosition,
            sequenceLookAtLineInfluence);
        desiredLookAt = followPosition + Vector3.ClampMagnitude(
            desiredLookAt - followPosition,
            sequenceLookAtMaxDistanceFromPlayer);

        sequenceLookAtTarget.position = Vector3.SmoothDamp(
            sequenceLookAtTarget.position,
            desiredLookAt,
            ref sequenceLookAtVelocity,
            sequenceLookAtSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        sequenceLookAtCameraController.OverrideLookAtTarget(sequenceLookAtTarget);
    }

    private CameraController GetActiveCameraController()
    {
        if (SwitchCharacter.Instance == null)
            return null;

        int activeIndex = SwitchCharacter.Instance.activePlayerIndex;
        var cameras = SwitchCharacter.Instance.playersCamera;
        if (cameras == null || activeIndex < 0 || activeIndex >= cameras.Length || cameras[activeIndex] == null)
            return null;

        return cameras[activeIndex].GetComponent<CameraController>();
    }

    private void RestoreSequenceLookAt()
    {
        if (sequenceLookAtCameraController != null)
            sequenceLookAtCameraController.RestoreLookAtTarget();

        sequenceLookAtCameraController = null;
        sequenceLookAtVelocity = Vector3.zero;
        resetSequenceLookAtOnNextUse = true;
    }

    private DetectiveIdeaPoint RaycastIdeaPoint()
    {
        if (Camera.main == null || Mouse.current == null)
            return null;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, ideaLayerMask, triggerInteraction))
            return null;

        return hit.collider.GetComponentInParent<DetectiveIdeaPoint>();
    }

    private Vector3 GetPointerWorldPosition()
    {
        if (Camera.main == null || Mouse.current == null)
            return dragSource != null ? dragSource.AnchorPosition : transform.position;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        return ray.GetPoint(pointerDistance);
    }

    private LineRenderer CreateLine(Color color)
    {
        GameObject lineObject = new GameObject("DetectiveIdeaLine");
        lineObject.transform.SetParent(transform);
        ApplyLineLayer(lineObject);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.material = GetLineMaterial();
        line.startColor = color;
        line.endColor = color;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;

        return line;
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        lineMaterial = new Material(shader);
        return lineMaterial;
    }

    private void ApplyLineLayer(GameObject lineObject)
    {
        if (lineObject == null || string.IsNullOrWhiteSpace(lineLayerName))
            return;

        int layer = LayerMask.NameToLayer(lineLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"{name}: Layer '{lineLayerName}' does not exist.");
            return;
        }

        lineObject.layer = layer;
    }

    private void DestroyPreviewLine()
    {
        if (previewLine != null)
            Destroy(previewLine.gameObject);

        previewLine = null;
    }

    private void RememberVisiblePoint(DetectiveIdeaPoint point)
    {
        if (point != null && !visiblePoints.Contains(point))
            visiblePoints.Add(point);
    }

    private void HideVisiblePoints()
    {
        for (int i = visiblePoints.Count - 1; i >= 0; i--)
        {
            DetectiveIdeaPoint point = visiblePoints[i];
            if (point == null)
            {
                visiblePoints.RemoveAt(i);
                continue;
            }

            if (!showDiscoveredPointsWhileActive || !point.IsDiscovered || !IsDetectiveVisionActive())
                point.SetVisible(false);
        }
    }

    private void SetAcceptedLinesVisible(bool visible)
    {
        for (int i = acceptedLines.Count - 1; i >= 0; i--)
        {
            LineRenderer line = acceptedLines[i];
            if (line == null)
            {
                acceptedLines.RemoveAt(i);
                continue;
            }

            line.gameObject.SetActive(visible);
        }
    }

    private void ClearActiveSequenceLines()
    {
        for (int i = activeSequenceLines.Count - 1; i >= 0; i--)
        {
            LineRenderer line = activeSequenceLines[i];
            if (line != null)
            {
                acceptedLines.Remove(line);
                Destroy(line.gameObject);
            }
        }

        activeSequenceLines.Clear();
    }

    private void SetDiscoveredPointsVisible(bool visible)
    {
        if (!showDiscoveredPointsWhileActive)
            return;

        for (int i = visiblePoints.Count - 1; i >= 0; i--)
        {
            DetectiveIdeaPoint point = visiblePoints[i];
            if (point == null)
            {
                visiblePoints.RemoveAt(i);
                continue;
            }

            if (point.IsDiscovered)
                point.SetVisible(visible);
        }
    }

    private void CompletePuzzle()
    {
        puzzleCompleted = true;
        Debug.Log("SUCCES");

        if (EagleVisionSystem.Instance != null)
            EagleVisionSystem.Instance.HoldVisionFor(solvedVisionHoldDuration);

        HoldSequenceLookAtAfterSolve();
        ClearHover();
        dragSource = null;
        DestroyPreviewLine();
        DestroyPreviewLineDescription();
        ClearActiveSequenceLines();

        foreach (LineRenderer line in acceptedLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        acceptedLines.Clear();
        activeSequenceLines.Clear();

        foreach (LineRenderer line in rejectedLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        rejectedLines.Clear();

        foreach (DetectiveIdeaPoint point in visiblePoints)
        {
            HideAndDisablePoint(point);
        }
        visiblePoints.Clear();

        if (activePuzzle != null)
        {
            foreach (DetectiveSequencePuzzle.IdeaConnectionStep step in activePuzzle.correctSequence)
            {
                if (step == null)
                    continue;

                HideAndDisablePoint(step.from);
                HideAndDisablePoint(step.to);
            }
        }

        hoveredPoint = null;
        dragSource = null;
        previewLine = null;
    }

    private void DestroySessionLines()
    {
        DestroyPreviewLine();
        DestroyPreviewLineDescription();

        foreach (LineRenderer line in acceptedLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        acceptedLines.Clear();
        activeSequenceLines.Clear();

        foreach (LineRenderer line in rejectedLines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        rejectedLines.Clear();
    }

    private void CreatePreviewLineDescription(DetectiveIdeaPoint point)
    {
        DestroyPreviewLineDescription();

        if (!showDraggedLineDescription || point == null || string.IsNullOrWhiteSpace(point.ideaDescription))
            return;

        GameObject descriptionObject = new GameObject("DetectiveIdeaLineDescription");
        descriptionObject.transform.SetParent(transform);
        ApplyLineLayer(descriptionObject);

        previewLineDescription = descriptionObject.AddComponent<TextMeshPro>();
        previewLineDescription.text = point.ideaDescription;
        previewLineDescription.fontSize = lineDescriptionFontSize;
        previewLineDescription.color = lineDescriptionColor;
        previewLineDescription.alignment = TextAlignmentOptions.Right;
        previewLineDescription.enableWordWrapping = false;
        UpdatePreviewLineDescription();
    }

    private void UpdatePreviewLineDescription()
    {
        if (previewLineDescription == null || dragSource == null || Camera.main == null)
            return;

        Transform cameraTransform = Camera.main.transform;
        Vector3 offset = cameraTransform.right * lineDescriptionScreenOffset.x +
                         cameraTransform.up * lineDescriptionScreenOffset.y +
                         cameraTransform.forward * lineDescriptionScreenOffset.z;

        previewLineDescription.transform.position = dragSource.AnchorPosition + offset;
        previewLineDescription.transform.rotation = cameraTransform.rotation;
    }

    private void DestroyPreviewLineDescription()
    {
        if (previewLineDescription != null)
            Destroy(previewLineDescription.gameObject);

        previewLineDescription = null;
    }

    private void HoldSequenceLookAtAfterSolve()
    {
        if (!useSequenceLookAt || solvedVisionHoldDuration <= 0f)
        {
            RestoreSequenceLookAt();
            return;
        }

        holdSolvedSequenceLookAt = true;

        if (solvedSequenceLookAtCoroutine != null)
            StopCoroutine(solvedSequenceLookAtCoroutine);

        solvedSequenceLookAtCoroutine = StartCoroutine(ReleaseSequenceLookAtAfterSolve());
    }

    private System.Collections.IEnumerator ReleaseSequenceLookAtAfterSolve()
    {
        yield return new WaitForSecondsRealtime(solvedVisionHoldDuration);

        holdSolvedSequenceLookAt = false;
        solvedSequenceLookAtCoroutine = null;
        RestoreSequenceLookAt();
    }

    private void KeepRejectedLine(DetectiveIdeaPoint target)
    {
        if (previewLine == null || dragSource == null || target == null)
        {
            DestroyPreviewLine();
            return;
        }

        previewLine.startColor = rejectedLineColor;
        previewLine.endColor = rejectedLineColor;
        previewLine.startWidth = lineWidth * rejectedLineWidthMultiplier;
        previewLine.endWidth = lineWidth * rejectedLineWidthMultiplier;
        previewLine.SetPosition(0, dragSource.AnchorPosition);
        previewLine.SetPosition(1, target.AnchorPosition);
        rejectedLines.Add(previewLine);
        previewLine = null;
    }

    private void HideAndDisablePoint(DetectiveIdeaPoint point)
    {
        if (point == null)
            return;

        point.SetHovered(false);
        point.SetVisible(false);
        point.enabled = false;
    }
}
