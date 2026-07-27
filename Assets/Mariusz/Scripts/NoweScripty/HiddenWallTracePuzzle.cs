using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class HiddenWallTracePuzzle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Int_HidenWall hiddenWall;
    [SerializeField] private SplineContainer runeSpline;
    [SerializeField] private Collider traceSurface;
    [SerializeField] private MagnifierGlassController magnifier;
    [SerializeField] private GameObject guideVisual;
    [SerializeField] private LineRenderer guideLine;
    [SerializeField] private LineRenderer progressLine;

    [Header("Tracing")]
    [SerializeField, Range(16, 160)] private int sampleCount = 72;
    [SerializeField, Range(0f, 0.25f)] private float startTolerance = 0.08f;
    [SerializeField, Range(0, 4)] private int brushSegments = 1;
    [SerializeField, Range(0.5f, 1f)] private float completionThreshold = 0.96f;

    [Header("Visual")]
    [SerializeField] private string lineLayerName = "WorldText";

    private bool[] tracedSamples;
    private bool started;
    private bool completed;
    private int contiguousProgress;

    private void Awake()
    {
        if (hiddenWall == null)
            hiddenWall = GetComponent<Int_HidenWall>();

        if (traceSurface == null)
            traceSurface = GetComponent<Collider>();

        if (magnifier == null)
            magnifier = FindFirstObjectByType<MagnifierGlassController>();

        if (progressLine != null)
        {
            progressLine.useWorldSpace = true;
            progressLine.positionCount = 0;
            ApplyLayer(progressLine.gameObject);
        }

        if (guideLine != null)
        {
            guideLine.useWorldSpace = true;
            ApplyLayer(guideLine.gameObject);
            BuildGuideLine();
        }

        SetGuideVisible(false);
    }

    private void Update()
    {
        if (completed || runeSpline == null || magnifier == null)
            return;

        bool loupeActive = magnifier.IsLoupeActive;
        bool isTracing = magnifier.TryGetActiveLoupeHit(out RaycastHit hit) && IsTraceSurface(hit.collider);
        SetGuideVisible(loupeActive);

        if (!isTracing)
            return;

        int sampleIndex = GetClosestSample(hit.point);
        if (!started)
        {
            if (sampleIndex > Mathf.CeilToInt((sampleCount - 1) * startTolerance))
                return;

            started = true;
        }

        MarkSample(sampleIndex);
        UpdateContiguousProgress();
        UpdateProgressLine();

        if ((float)contiguousProgress / (sampleCount - 1) >= completionThreshold)
            CompletePuzzle();
    }

    public void ResetPuzzle()
    {
        completed = false;
        started = false;
        contiguousProgress = 0;
        tracedSamples = null;

        if (progressLine != null)
            progressLine.positionCount = 0;
    }

    private bool IsTraceSurface(Collider hitCollider)
    {
        if (hitCollider == null)
            return false;

        if (traceSurface == null)
            return hitCollider.transform.IsChildOf(transform) || hitCollider.transform == transform;

        return hitCollider == traceSurface || hitCollider.transform.IsChildOf(traceSurface.transform);
    }

    private int GetClosestSample(Vector3 hitPoint)
    {
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            float distance = (GetWorldSplinePosition(t) - hitPoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void MarkSample(int centerIndex)
    {
        if (tracedSamples == null || tracedSamples.Length != sampleCount)
            tracedSamples = new bool[sampleCount];

        int first = Mathf.Max(0, centerIndex - brushSegments);
        int last = Mathf.Min(sampleCount - 1, centerIndex + brushSegments);
        for (int i = first; i <= last; i++)
            tracedSamples[i] = true;
    }

    private void UpdateContiguousProgress()
    {
        contiguousProgress = 0;
        while (contiguousProgress < sampleCount - 1 && tracedSamples[contiguousProgress + 1])
            contiguousProgress++;
    }

    private void UpdateProgressLine()
    {
        if (progressLine == null || !started)
            return;

        progressLine.positionCount = contiguousProgress + 1;
        for (int i = 0; i <= contiguousProgress; i++)
        {
            float t = (float)i / (sampleCount - 1);
            progressLine.SetPosition(i, GetWorldSplinePosition(t));
        }
    }

    private void BuildGuideLine()
    {
        if (guideLine == null || runeSpline == null)
            return;

        guideLine.positionCount = sampleCount;
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            guideLine.SetPosition(i, GetWorldSplinePosition(t));
        }
    }

    private Vector3 GetWorldSplinePosition(float t)
    {
        SplineUtility.Evaluate(runeSpline.Spline, t, out float3 localPosition, out _, out _);
        return runeSpline.transform.TransformPoint(localPosition);
    }

    private void CompletePuzzle()
    {
        completed = true;
        SetGuideVisible(true);
        hiddenWall?.ResolveTracePuzzle();
    }

    private void SetGuideVisible(bool visible)
    {
        if (guideVisual != null)
            guideVisual.SetActive(visible || completed);

        if (guideLine != null)
            guideLine.gameObject.SetActive(visible || completed);
    }

    private void ApplyLayer(GameObject target)
    {
        int layer = LayerMask.NameToLayer(lineLayerName);
        if (layer >= 0)
            target.layer = layer;
    }
}
