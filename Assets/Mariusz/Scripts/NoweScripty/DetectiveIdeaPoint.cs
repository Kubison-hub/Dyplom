using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.VFX;

[DisallowMultipleComponent]
public class DetectiveIdeaPoint : MonoBehaviour
{
    public enum DiscoveryMode
    {
        Magnifier,
        External,
        MagnifierOrExternal
    }

    public enum DependencyMode
    {
        All,
        Any
    }

    [System.Serializable]
    public class ConnectionDependency
    {
        public DetectiveIdeaPoint first;
        public DetectiveIdeaPoint second;
    }

    [System.Serializable]
    public class IdeaConnection
    {
        public DetectiveIdeaPoint target;

        [Header("Connection")]
        public bool canConnect = true;
        public bool canConnectReverse = true;

        [Header("First response")]
        public string firstTitle = "Sherlock";
        [TextArea] public string firstDescription = "";

        [Header("Repeated response")]
        public string repeatTitle = "Sherlock";
        [TextArea] public string repeatDescription = "";

        [Header("Dependencies")]
        public DependencyMode dependencyMode = DependencyMode.All;
        public List<ConnectionDependency> requiredConnections = new List<ConnectionDependency>();
        public string lockedTitle = "Jeszcze nie.";
        [TextArea] public string lockedDescription = "Brakuje mi informacji, aby polaczyc te fakty.";
    }

    [Header("Idea")]
    public string ideaId;
    public string ideaTitle = "Trop";
    [TextArea] public string ideaDescription = "";

    [Header("Discovery")]
    [Tooltip("Magnifier: discover with F. External: reveal only from another script. MagnifierOrExternal: either source.")]
    public DiscoveryMode discoveryMode = DiscoveryMode.Magnifier;

    [Header("Connections from this point")]
    public List<IdeaConnection> connections = new List<IdeaConnection>();

    [Header("Visual")]
    public Transform ideaAnchor;
    public GameObject ideaVisual;
    public bool autoCreateVisual = true;
    public float visualScale = 0.18f;
    public Material visualMaterial;
    public Color visualColor = new Color(0.56f, 0.82f, 0.62f, 1f);
    public bool setVisualLayer = true;
    public string visualLayerName = "WorldText";
    public bool useVisualAsRaycastTarget = true;
    public float hoverScaleIncrease = 0.2f;

    [Header("Collider")]
    public Collider clickableCollider;
    [SerializeField] private string ideaAnchorChildName = "IdeaAnchor";

    [Header("Question FX")]
    [SerializeField] private VisualEffect questionFX;
    [SerializeField] private string questionFXRateProperty = "Rate";
    [SerializeField, Min(0f)] private float questionFXDisableDelay = 5f;


    public bool IsDiscovered { get; private set; }
    public event Action<DetectiveIdeaPoint> OnDiscovered;
    public bool CanDiscoverWithMagnifier =>
        discoveryMode == DiscoveryMode.Magnifier || discoveryMode == DiscoveryMode.MagnifierOrExternal;
    public bool CanDiscoverExternally =>
        discoveryMode == DiscoveryMode.External || discoveryMode == DiscoveryMode.MagnifierOrExternal;
    private Vector3 baseVisualScale;
    private bool isHovered;
    private Renderer visualRenderer;
    private Material visualInstance;
    private Coroutine questionFXDisableCoroutine;

    private void Awake()
    {
        visualLayerName = "WorldText";
        visualColor = new Color(0.56f, 0.82f, 0.62f, 1f);

        if (string.IsNullOrWhiteSpace(ideaId))
            ideaId = gameObject.name;

        if (ideaAnchor == null)
        {
            Transform childAnchor = transform.Find(ideaAnchorChildName);
            ideaAnchor = childAnchor != null ? childAnchor : transform;
        }

        if (clickableCollider == null)
            clickableCollider = GetComponent<Collider>();

        if (questionFX == null)
        {
            Transform questionFXChild = transform.Find("QuestionFX");
            if (questionFXChild != null)
                questionFX = questionFXChild.GetComponent<VisualEffect>();
        }

        if (ideaVisual == null && autoCreateVisual)
            CreateDefaultVisual();

        CacheBaseVisualScale();
        EnsureVisualRaycastTarget();
        ApplyVisualColor();
        ApplyVisualLayer();
        SetVisible(false);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(ideaId))
            ideaId = gameObject.name;
    }

    public Vector3 AnchorPosition
    {
        get
        {
            if (ideaAnchor != null)
                return ideaAnchor.position;

            return transform.position;
        }
    }

    public void MarkDiscovered()
    {
        if (IsDiscovered)
            return;

        IsDiscovered = true;
        SetDiscoveryPreview(1f);
        FadeOutQuestionFX();
        OnDiscovered?.Invoke(this);
    }

    private void FadeOutQuestionFX()
    {
        if (questionFX == null)
            return;

        if (questionFX.HasInt(questionFXRateProperty))
            questionFX.SetInt(questionFXRateProperty, 0);
        else if (questionFX.HasFloat(questionFXRateProperty))
            questionFX.SetFloat(questionFXRateProperty, 0f);
        else
            Debug.LogWarning($"{name}: QuestionFX has no Rate property named '{questionFXRateProperty}'.");

        if (questionFXDisableCoroutine != null)
            StopCoroutine(questionFXDisableCoroutine);

        questionFXDisableCoroutine = StartCoroutine(DisableQuestionFXAfterDelay());
    }

    private IEnumerator DisableQuestionFXAfterDelay()
    {
        yield return new WaitForSeconds(questionFXDisableDelay);

        if (questionFX != null)
            questionFX.gameObject.SetActive(false);

        questionFXDisableCoroutine = null;
    }

    public void SetDiscoveryPreview(float progress)
    {
        if (ideaVisual == null)
            return;

        progress = Mathf.Clamp01(progress);
        SetVisible(true);
        ideaVisual.transform.localScale = Vector3.Lerp(baseVisualScale * 0.55f, baseVisualScale, progress);

        Renderer renderer = GetVisualRenderer();
        if (renderer == null)
            return;

        Material material = GetVisualMaterial(renderer);
        if (material != null && material.HasProperty("_BaseColor"))
        {
            Color color = visualColor;
            color.a *= progress;
            material.SetColor("_BaseColor", color);
        }
        else if (material != null && material.HasProperty("_Color"))
        {
            Color color = visualColor;
            color.a *= progress;
            material.SetColor("_Color", color);
        }
    }

    public void CancelDiscoveryPreview()
    {
        if (IsDiscovered)
            return;

        SetDiscoveryPreview(0f);
        SetVisible(false);
    }

    public bool RevealFromExternalSource()
    {
        if (!CanDiscoverExternally)
        {
            Debug.LogWarning($"{name}: This idea point is not configured for external discovery.");
            return false;
        }

        if (DetectiveIdeaManager.Instance == null)
        {
            Debug.LogWarning($"{name}: DetectiveIdeaManager is missing, so the point cannot be revealed.");
            return false;
        }

        DetectiveIdeaManager.Instance.DiscoverPoint(this);
        return true;
    }

    public void SetVisible(bool visible)
    {
        if (ideaVisual == null)
            return;

        // Keep the visual collider alive so an undiscovered point can still be found by the magnifier raycast.
        ideaVisual.SetActive(true);

        Renderer[] renderers = ideaVisual.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
            renderer.enabled = visible;
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;

        if (ideaVisual == null)
            return;

        ideaVisual.transform.localScale = isHovered
            ? baseVisualScale + Vector3.one * hoverScaleIncrease
            : baseVisualScale;
    }

    public void ShowIdeaText()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopText(ideaTitle, ideaDescription);
    }

    public void ShowIdeaTitle()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ShowTopTextPersistent(ideaTitle, "");
    }

    public void ClearIdeaTitle()
    {
        if (PlayerTopText.Instance != null)
            PlayerTopText.Instance.ClearTopTextIfMatches(ideaTitle, "");
    }

    public IdeaConnection FindConnectionTo(DetectiveIdeaPoint target)
    {
        for (int i = 0; i < connections.Count; i++)
        {
            IdeaConnection connection = connections[i];
            if (connection != null && connection.target == target)
                return connection;
        }

        return null;
    }

    private void CreateDefaultVisual()
    {
        ideaVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ideaVisual.name = $"{gameObject.name}_IdeaPointVisual";
        ideaVisual.transform.SetParent(transform);
        ideaVisual.transform.position = AnchorPosition;
        ideaVisual.transform.localScale = Vector3.one * visualScale;

        Renderer renderer = ideaVisual.GetComponent<Renderer>();
        if (renderer != null && visualMaterial != null)
            renderer.sharedMaterial = visualMaterial;

        CacheBaseVisualScale();
        EnsureVisualRaycastTarget();
        ApplyVisualColor();
        ApplyVisualLayer();
    }

    private void CacheBaseVisualScale()
    {
        if (ideaVisual != null)
            baseVisualScale = ideaVisual.transform.localScale;
    }

    private void EnsureVisualRaycastTarget()
    {
        if (!useVisualAsRaycastTarget || ideaVisual == null)
            return;

        Collider visualCollider = ideaVisual.GetComponent<Collider>();
        if (visualCollider == null)
            visualCollider = ideaVisual.AddComponent<SphereCollider>();

        visualCollider.isTrigger = true;
    }

    private void ApplyVisualColor()
    {
        if (ideaVisual == null)
            return;

        Renderer renderer = GetVisualRenderer();
        if (renderer == null)
            return;

        if (visualMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Standard");

            visualMaterial = new Material(shader);
        }

        renderer.sharedMaterial = visualMaterial;
        renderer.sharedMaterial.color = visualColor;
    }

    private Renderer GetVisualRenderer()
    {
        if (visualRenderer == null && ideaVisual != null)
            visualRenderer = ideaVisual.GetComponent<Renderer>();

        return visualRenderer;
    }

    private Material GetVisualMaterial(Renderer renderer)
    {
        if (visualInstance == null && renderer != null)
            visualInstance = renderer.material;

        return visualInstance;
    }

    private void ApplyVisualLayer()
    {
        if (!setVisualLayer || ideaVisual == null || string.IsNullOrWhiteSpace(visualLayerName))
            return;

        int layer = LayerMask.NameToLayer(visualLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"{name}: Layer '{visualLayerName}' does not exist.");
            return;
        }

        SetLayerRecursively(ideaVisual.transform, layer);
    }

    private void SetLayerRecursively(Transform target, int layer)
    {
        target.gameObject.layer = layer;

        for (int i = 0; i < target.childCount; i++)
        {
            SetLayerRecursively(target.GetChild(i), layer);
        }
    }
}
