using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv1_HidenWallMask : MonoBehaviour
{
    [Header("Mask Detection")]
    [SerializeField] private bool startPuzzleActive = true;
    [SerializeField] private Collider maskCollider;
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField, Min(0.005f)] private float traceRadius = 0.08f;
    [SerializeField, Range(0.1f, 1f)] private float requiredProgress = 0.9f;

    [Header("Eagle Vision Visibility")]
    [SerializeField] private bool visibleOnlyInEagleVision = true;

    [Header("Vision Layers")]
    [SerializeField] private string undiscoveredLayerName = "Clues";
    [SerializeField] private string discoveredLayerName = "WorldText";

    [Header("Mask Appearance")]
    [SerializeField] private bool setInitialColorOnStart = true;
    [SerializeField] private Color hiddenSegmentColor = new Color(0.08f, 0.22f, 0.12f, 0.35f);
    [SerializeField] private Color revealedSegmentColor = new Color(0.48f, 0.78f, 0.55f, 1f);
    [SerializeField, Min(0.01f)] private float revealDuration = 0.2f;

    [Header("Reveal Glow")]
    [SerializeField] private bool createGlow = true;
    [SerializeField] private Material glowMaterial;
    [SerializeField, Min(1f)] private float glowWidthMultiplier = 2.2f;
    [SerializeField] private Color glowColor = new Color(0.35f, 0.8f, 0.45f, 0.22f);

    [Header("Completion")]
    [SerializeField, TextArea] private string firstDiscoveryText =
        "Ciekawy wzor. Lupa wydobywa go spod warstwy kurzu.";
    [SerializeField, TextArea] private string completedText =
        "Maska. A wiec gospodarz lubil tajemnice bardziej niz swieze powietrze.";
    [SerializeField] private Transform cardPosition;
    [SerializeField] private DetectiveIdeaPoint ideaPoint;
    [SerializeField] private GameObject[] activateOnSolved;
    [SerializeField] private bool deactivateSegmentsOnSolved = true;

    private readonly List<LineRenderer> segments = new List<LineRenderer>();
    private readonly HashSet<LineRenderer> discoveredSegments = new HashSet<LineRenderer>();
    private readonly Dictionary<LineRenderer, LineRenderer> glowSegments = new Dictionary<LineRenderer, LineRenderer>();
    private Interactable interactable;
    private bool hasShownFirstDiscoveryText;
    private bool solved;
    private bool puzzleActive;
    private bool maskVisualsVisible;
    private int undiscoveredLayer = -1;
    private int discoveredLayer = -1;

    public float Progress => segments.Count == 0 ? 0f : (float)discoveredSegments.Count / segments.Count;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactable.SetInteractionType(InteractionType.Int_lv1_HidenWallMask);
        

        if (maskCollider == null)
            maskCollider = GetComponent<Collider>();

        if (segmentsParent == null)
            segmentsParent = transform.Find("MaskCanvas");

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (ideaPoint != null)
            ideaPoint.discoveryMode = DetectiveIdeaPoint.DiscoveryMode.External;

        CacheVisionLayers();
        CacheSegments();
        puzzleActive = startPuzzleActive;
        UpdateMaskVisibility();
    }

    private void Update()
    {
        UpdateMaskVisibility();

        if (!puzzleActive || solved || magnifier == null || maskCollider == null || segments.Count == 0)
            return;

        if (!magnifier.TryGetActiveLoupeHit(out RaycastHit hit) || !IsMaskHit(hit.collider))
            return;

        bool foundNewSegment = false;
        foreach (LineRenderer segment in segments)
        {
            if (discoveredSegments.Contains(segment) || !IsPointNearSegment(hit.point, segment))
                continue;

            DiscoverSegment(segment);
            foundNewSegment = true;
        }

        if (foundNewSegment && !hasShownFirstDiscoveryText)
        {
            hasShownFirstDiscoveryText = true;
            ShowTopText(firstDiscoveryText);
        }

        if (Progress >= requiredProgress)
            CompleteMask();
    }

    public void PerformInteraction(PlayerController player)
    {
        // This interaction is solved only through the loupe, not a regular click.
    }

    public void BeginPuzzle()
    {
        if (solved)
            return;

        puzzleActive = true;
        UpdateMaskVisibility();
    }

    public void ConfigureTraceDifficulty(float newTraceRadius, float newRequiredProgress)
    {
        traceRadius = Mathf.Max(0.005f, newTraceRadius);
        requiredProgress = Mathf.Clamp(newRequiredProgress, 0.1f, 1f);
    }

    private void CacheSegments()
    {
        if (segmentsParent == null)
        {
            Debug.LogWarning("Int_lv1_HidenWallMask: Assign MaskCanvas in Segments Parent.", this);
            return;
        }

        segments.Clear();
        segments.AddRange(segmentsParent.GetComponentsInChildren<LineRenderer>(true));

        foreach (LineRenderer segment in segments)
        {
            if (segment == null || segment.positionCount < 2)
                continue;

            if (setInitialColorOnStart)
                SetSegmentColor(segment, hiddenSegmentColor);

            SetSegmentLayer(segment, undiscoveredLayer);
        }

        segments.RemoveAll(segment => segment == null || segment.positionCount < 2);
    }

    private void UpdateMaskVisibility()
    {
        if (segmentsParent == null)
            return;

        bool shouldBeVisible = puzzleActive &&
                               (!visibleOnlyInEagleVision ||
                                EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive);

        if (maskVisualsVisible == shouldBeVisible && segmentsParent.gameObject.activeSelf == shouldBeVisible)
            return;

        maskVisualsVisible = shouldBeVisible;
        segmentsParent.gameObject.SetActive(shouldBeVisible);
    }

    private bool IsMaskHit(Collider hitCollider)
    {
        if (hitCollider == null || maskCollider == null)
            return false;

        Transform hitTransform = hitCollider.transform;
        Transform maskTransform = maskCollider.transform;

        return hitCollider == maskCollider ||
               hitTransform.IsChildOf(maskTransform) ||
               maskTransform.IsChildOf(hitTransform);
    }

    private bool IsPointNearSegment(Vector3 worldPoint, LineRenderer segment)
    {
        int pointCount = segment.positionCount;
        for (int index = 0; index < pointCount - 1; index++)
        {
            Vector3 firstPoint = GetWorldPoint(segment, index);
            Vector3 secondPoint = GetWorldPoint(segment, index + 1);

            if (DistanceToLineSegment(worldPoint, firstPoint, secondPoint) <= traceRadius)
                return true;
        }

        return segment.loop &&
               DistanceToLineSegment(worldPoint, GetWorldPoint(segment, pointCount - 1), GetWorldPoint(segment, 0)) <= traceRadius;
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

        float positionOnSegment = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        return Vector3.Distance(point, start + segment * positionOnSegment);
    }

    private void DiscoverSegment(LineRenderer segment)
    {
        discoveredSegments.Add(segment);
        SetSegmentLayer(segment, discoveredLayer);
        StartCoroutine(AnimateSegmentReveal(segment));
    }

    private IEnumerator AnimateSegmentReveal(LineRenderer segment)
    {
        LineRenderer glow = createGlow ? GetOrCreateGlow(segment) : null;
        float elapsed = 0f;

        while (elapsed < revealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, elapsed / revealDuration);

            SetSegmentColor(segment, Color.Lerp(hiddenSegmentColor, revealedSegmentColor, progress));
            if (glow != null)
                SetSegmentColor(glow, Color.Lerp(Color.clear, glowColor, progress));

            yield return null;
        }

        SetSegmentColor(segment, revealedSegmentColor);
        if (glow != null)
            SetSegmentColor(glow, glowColor);
    }

    private LineRenderer GetOrCreateGlow(LineRenderer source)
    {
        if (glowSegments.TryGetValue(source, out LineRenderer glow) && glow != null)
            return glow;

        GameObject glowObject = new GameObject(source.name + "_Glow");
        glowObject.transform.SetParent(source.transform, false);

        glow = glowObject.AddComponent<LineRenderer>();
        glow.useWorldSpace = source.useWorldSpace;
        glow.alignment = source.alignment;
        glow.textureMode = source.textureMode;
        glow.numCapVertices = source.numCapVertices;
        glow.numCornerVertices = source.numCornerVertices;
        glow.loop = source.loop;
        glow.widthCurve = source.widthCurve;
        glow.widthMultiplier = source.widthMultiplier * glowWidthMultiplier;
        glow.sharedMaterial = glowMaterial != null ? glowMaterial : source.sharedMaterial;
        glow.sortingLayerID = source.sortingLayerID;
        glow.sortingOrder = source.sortingOrder - 1;
        glow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        glow.receiveShadows = false;
        glow.positionCount = source.positionCount;

        Vector3[] points = new Vector3[source.positionCount];
        source.GetPositions(points);
        glow.SetPositions(points);
        SetSegmentColor(glow, Color.clear);
        SetSegmentLayer(glow, discoveredLayer);

        glowSegments[source] = glow;
        return glow;
    }

    private void CacheVisionLayers()
    {
        undiscoveredLayer = LayerMask.NameToLayer(undiscoveredLayerName);
        discoveredLayer = LayerMask.NameToLayer(discoveredLayerName);

        if (undiscoveredLayer < 0)
            Debug.LogWarning("Int_lv1_HidenWallMask: Layer '" + undiscoveredLayerName + "' was not found.", this);

        if (discoveredLayer < 0)
            Debug.LogWarning("Int_lv1_HidenWallMask: Layer '" + discoveredLayerName + "' was not found.", this);
    }

    private static void SetSegmentLayer(LineRenderer segment, int layer)
    {
        if (segment != null && layer >= 0)
            segment.gameObject.layer = layer;
    }

    private static void SetSegmentColor(LineRenderer segment, Color color)
    {
        segment.startColor = color;
        segment.endColor = color;
    }

    private void CompleteMask()
    {
        if (solved)
            return;

        solved = true;
        ShowTopText(completedText);

        if (interactable.clues != null && interactable.clues.Length > 0)
            interactable.AddClue(0, cardPosition);

        ideaPoint?.RevealFromExternalSource();

        // The pattern has been fully examined, so it should no longer advertise itself in Eagle Vision.
        interactable.isInteractableActive = false;
        interactable.allowQuestionFXWhenInactive = false;
        interactable.SetQuestionFXEagleVisionState(false);

        foreach (GameObject target in activateOnSolved)
        {
            if (target != null)
                target.SetActive(true);
        }

        if (deactivateSegmentsOnSolved)
        {
            foreach (LineRenderer segment in segments)
            {
                if (segment != null)
                    segment.gameObject.SetActive(false);
            }
        }

        if (maskCollider != null)
            maskCollider.enabled = false;
    }

    private static void ShowTopText(string text)
    {
        if (!string.IsNullOrWhiteSpace(text) && PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(text, "");
    }
}
