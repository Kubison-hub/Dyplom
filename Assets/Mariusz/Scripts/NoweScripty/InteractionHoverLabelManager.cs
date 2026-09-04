using TMPro;
using UnityEngine;

/// <summary>
/// Draws one world-space callout for the Interactable currently hovered by the
/// existing interaction tooltip system.
/// </summary>
public class InteractionHoverLabelManager : MonoBehaviour
{
    private static InteractionHoverLabelManager instance;

    [SerializeField] private string labelLayerName = "WorldText";
    [SerializeField] private Color labelColor = new Color(0.96f, 0.97f, 0.94f, 1f);
    [SerializeField] private Color lineColor = new Color(0.92f, 0.94f, 0.9f, 0.9f);
    [SerializeField, Min(0.005f)] private float lineWidth = 0.018f;
    [SerializeField, Min(0.01f)] private float pointSize = 0.08f;
    [SerializeField, Min(0.01f)] private float labelFontSize = 0.18f;

    private InteractionHoverLabel hoveredLabel;
    private GameObject visualRoot;
    private TextMeshPro labelText;
    private LineRenderer connectorLine;
    private Transform anchorPoint;
    private Material lineMaterial;

    public static void SetHovered(Interactable interactable, bool isHovered)
    {
        if (interactable == null)
            return;

        InteractionHoverLabel label = interactable.GetComponent<InteractionHoverLabel>();
        if (label == null)
            return;

        InteractionHoverLabelManager manager = GetOrCreate();
        if (isHovered && IsEagleVisionActive() && !IsConversationActive() && !IsWorldInputBlocked())
            manager.Show(label);
        else
            manager.HideIfOwnedBy(label);
    }

    private static InteractionHoverLabelManager GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<InteractionHoverLabelManager>();
        if (instance != null)
            return instance;

        GameObject managerObject = new GameObject("InteractionHoverLabelManager");
        instance = managerObject.AddComponent<InteractionHoverLabelManager>();
        return instance;
    }

    private void LateUpdate()
    {
        if (IsConversationActive() || IsWorldInputBlocked() || !IsEagleVisionActive() ||
            hoveredLabel == null || !hoveredLabel.isActiveAndEnabled)
        {
            Hide();
            return;
        }

        UpdateVisual();
    }

    private void Show(InteractionHoverLabel label)
    {
        hoveredLabel = label;
        EnsureVisual();
        visualRoot.SetActive(true);
        UpdateVisual();
    }

    private void HideIfOwnedBy(InteractionHoverLabel label)
    {
        if (hoveredLabel == label)
            Hide();
    }

    private void Hide()
    {
        hoveredLabel = null;
        if (visualRoot != null)
            visualRoot.SetActive(false);
    }

    private void EnsureVisual()
    {
        if (visualRoot != null)
            return;

        visualRoot = new GameObject("InteractionHoverLabel");
        visualRoot.transform.SetParent(transform, false);
        ApplyLayer(visualRoot);

        GameObject textObject = new GameObject("Text", typeof(TextMeshPro));
        textObject.transform.SetParent(visualRoot.transform, false);
        ApplyLayer(textObject);
        labelText = textObject.GetComponent<TextMeshPro>();
        labelText.fontSize = labelFontSize;
        labelText.color = labelColor;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.enableWordWrapping = false;
        labelText.richText = true;

        GameObject lineObject = new GameObject("Connector", typeof(LineRenderer));
        lineObject.transform.SetParent(visualRoot.transform, false);
        ApplyLayer(lineObject);
        connectorLine = lineObject.GetComponent<LineRenderer>();
        connectorLine.useWorldSpace = true;
        connectorLine.positionCount = 3;
        connectorLine.startWidth = lineWidth;
        connectorLine.endWidth = lineWidth;
        connectorLine.startColor = lineColor;
        connectorLine.endColor = lineColor;
        connectorLine.material = GetLineMaterial();

        GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObject.name = "AnchorPoint";
        pointObject.transform.SetParent(visualRoot.transform, false);
        ApplyLayer(pointObject);
        Collider pointCollider = pointObject.GetComponent<Collider>();
        if (pointCollider != null)
            pointCollider.enabled = false;

        Renderer pointRenderer = pointObject.GetComponent<Renderer>();
        if (pointRenderer != null)
        {
            Material pointMaterial = new Material(Shader.Find("Sprites/Default"));
            pointMaterial.color = lineColor;
            pointRenderer.material = pointMaterial;
        }

        anchorPoint = pointObject.transform;
    }

    private void UpdateVisual()
    {
        if (hoveredLabel == null || labelText == null || connectorLine == null || anchorPoint == null)
            return;

        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
            return;

        Transform anchor = hoveredLabel.LabelAnchor;
        Vector3 anchorPosition = anchor.position;
        Vector3 anchorScreenPosition = cameraToUse.WorldToScreenPoint(anchorPosition);
        if (anchorScreenPosition.z <= 0f)
        {
            visualRoot.SetActive(false);
            return;
        }

        float margin = hoveredLabel.ScreenMargin;
        Vector2 desiredScreenPosition = new Vector2(
            anchorScreenPosition.x + hoveredLabel.ScreenOffset.x,
            anchorScreenPosition.y + hoveredLabel.ScreenOffset.y);
        Vector2 labelScreenPosition = new Vector2(
            Mathf.Clamp(desiredScreenPosition.x, margin, Screen.width - margin),
            Mathf.Clamp(desiredScreenPosition.y, margin, Screen.height - margin));
        Vector3 labelPosition = cameraToUse.ScreenToWorldPoint(
            new Vector3(labelScreenPosition.x, labelScreenPosition.y, anchorScreenPosition.z));
        Vector3 elbowPosition = anchorPosition +
                                (labelPosition - anchorPosition).normalized * hoveredLabel.ElbowLength;

        labelText.transform.position = labelPosition;
        labelText.transform.rotation = Quaternion.LookRotation(
            labelText.transform.position - cameraToUse.transform.position,
            cameraToUse.transform.up);
        labelText.transform.localScale = Vector3.one;
        labelText.font = hoveredLabel.LabelFont != null ? hoveredLabel.LabelFont : TMP_Settings.defaultFontAsset;
        labelText.fontSize = hoveredLabel.LabelFontSize;
        labelText.text = $"<u>{hoveredLabel.DisplayName}</u>\n<size=70%>{hoveredLabel.InteractionHint}</size>";

        anchorPoint.position = anchorPosition;
        anchorPoint.localScale = Vector3.one * pointSize;
        connectorLine.SetPosition(0, anchorPosition);
        connectorLine.SetPosition(1, elbowPosition);
        connectorLine.SetPosition(2, labelPosition);
        visualRoot.SetActive(true);
    }

    private Material GetLineMaterial()
    {
        if (lineMaterial != null)
            return lineMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        lineMaterial = new Material(shader);
        return lineMaterial;
    }

    private void ApplyLayer(GameObject target)
    {
        int layer = LayerMask.NameToLayer(labelLayerName);
        if (layer >= 0)
            target.layer = layer;
    }

    private static bool IsEagleVisionActive()
    {
        return EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
    }

    private static bool IsConversationActive()
    {
        return DialogueEditor.ConversationManager.Instance != null &&
               DialogueEditor.ConversationManager.Instance.IsConversationActive;
    }

    private static bool IsWorldInputBlocked()
    {
        return NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen ||
               TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
               TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput;
    }
}
