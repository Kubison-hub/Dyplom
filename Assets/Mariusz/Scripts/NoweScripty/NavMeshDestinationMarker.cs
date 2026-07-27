using UnityEngine;
using UnityEngine.AI;

public class NavMeshDestinationMarker : MonoBehaviour
{
    private static NavMeshDestinationMarker instance;
    private const string HiddenLayerName = "Hidden";

    [Header("Appearance")]
    [SerializeField, Min(0.1f)] private float radius = 0.38f;
    [SerializeField, Range(8, 96)] private int segments = 40;
    [SerializeField, Min(0.005f)] private float lineWidth = 0.045f;
    [SerializeField] private Color ringColor = new Color(0.36f, 0.9f, 0.55f, 0.8f);
    [SerializeField] private float groundOffset = 0.025f;

    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float visibleDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.75f;

    [Header("Pulse")]
    [SerializeField, Range(0f, 0.5f)] private float scalePulseAmount = 0.08f;
    [SerializeField, Min(0f)] private float scalePulseSpeed = 1.6f;
    [SerializeField, Range(0f, 1f)] private float minimumAlpha = 0.35f;
    [SerializeField, Min(0f)] private float alphaPulseSpeed = 1.2f;

    private LineRenderer lineRenderer;
    private NavMeshAgent trackedAgent;
    private Vector3 baseScale;
    private float shownAtTime;

    public static NavMeshDestinationMarker GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject markerObject = new GameObject("NavMeshDestinationMarker");
        instance = markerObject.AddComponent<NavMeshDestinationMarker>();
        return instance;
    }

    public static void HideCurrent()
    {
        if (instance != null)
            instance.Hide();
    }

    public void ShowAt(Vector3 destination, NavMeshAgent agent)
    {
        EnsureVisual();
        trackedAgent = agent;
        transform.position = destination + Vector3.up * groundOffset;
        shownAtTime = Time.unscaledTime;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        trackedAgent = null;
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        SetHiddenLayer();
        EnsureVisual();
        baseScale = transform.localScale;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float scalePulse = 1f + Mathf.Sin(time * scalePulseSpeed) * scalePulseAmount;
        transform.localScale = baseScale * scalePulse;

        float alphaWave = (Mathf.Sin(time * alphaPulseSpeed) + 1f) * 0.5f;
        Color pulseColor = ringColor;
        float lifetimeAlpha = GetLifetimeAlpha(time);
        pulseColor.a = Mathf.Lerp(minimumAlpha, ringColor.a, alphaWave) * lifetimeAlpha;
        lineRenderer.startColor = pulseColor;
        lineRenderer.endColor = pulseColor;

        if (lifetimeAlpha <= 0f)
            Hide();
    }

    private void EnsureVisual()
    {
        if (lineRenderer != null)
            return;

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.numCornerVertices = 3;
        lineRenderer.numCapVertices = 3;
        lineRenderer.positionCount = segments;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.material = CreateRingMaterial();

        for (int index = 0; index < segments; index++)
        {
            float angle = index / (float)segments * Mathf.PI * 2f;
            lineRenderer.SetPosition(index, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void SetHiddenLayer()
    {
        int hiddenLayer = LayerMask.NameToLayer(HiddenLayerName);
        if (hiddenLayer < 0)
        {
            Debug.LogWarning($"{name}: Layer '{HiddenLayerName}' was not found.", this);
            return;
        }

        SetLayerRecursively(transform, hiddenLayer);
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }

    private static Material CreateRingMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        return shader != null ? new Material(shader) : new Material(Shader.Find("Standard"));
    }

    private bool HasReachedTrackedDestination()
    {
        if (trackedAgent == null || !trackedAgent.isOnNavMesh || trackedAgent.pathPending)
            return false;

        if (trackedAgent.remainingDistance > trackedAgent.stoppingDistance + 0.06f)
            return false;

        return !trackedAgent.hasPath || trackedAgent.velocity.sqrMagnitude < 0.01f;
    }

    private float GetLifetimeAlpha(float currentTime)
    {
        float elapsed = currentTime - shownAtTime;
        if (elapsed <= visibleDuration)
            return 1f;

        return 1f - Mathf.Clamp01((elapsed - visibleDuration) / fadeOutDuration);
    }
}
