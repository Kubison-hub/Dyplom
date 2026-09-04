using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Fade Gradient")]
[RequireComponent(typeof(Graphic))]
public class UIGradientFade : BaseMeshEffect
{
    [Header("Ustawienia koloru")]
    [Tooltip("Kolor po lewej stronie")]
    public Color leftColor = Color.black;

    [Tooltip("Kolor po prawej stronie")]
    public Color rightColor = new Color(0f, 0f, 0f, 0f);

    [SerializeField, HideInInspector] private RectTransform gradientBounds;

    public RectTransform GradientBounds => gradientBounds;

    public void SetGradientBounds(RectTransform bounds)
    {
        gradientBounds = bounds;
        if (graphic != null)
            graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        // Szukamy skrajnych punktów szerokoœci obiektu
        float leftX = vh.currentVertCount > 0 ? float.MaxValue : 0;
        float rightX = vh.currentVertCount > 0 ? float.MinValue : 0;

        UIVertex vertex = new UIVertex();

        // Pierwsza pêtla: ustalamy szerokoœæ wierzcho³ków obrazka
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.x < leftX) leftX = vertex.position.x;
            if (vertex.position.x > rightX) rightX = vertex.position.x;
        }

        float width = rightX - leftX;
        bool useSharedBounds = gradientBounds != null;

        if (!useSharedBounds && width <= 0)
            return;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);

            float t;
            if (useSharedBounds)
            {
                Vector3 boundsPosition = gradientBounds.InverseTransformPoint(transform.TransformPoint(vertex.position));
                t = Mathf.InverseLerp(gradientBounds.rect.xMin, gradientBounds.rect.xMax, boundsPosition.x);
            }
            else
            {
                t = (vertex.position.x - leftX) / width;
            }

            vertex.color = Color.Lerp(leftColor, rightColor, t);
            vh.SetUIVertex(vertex, i);
        }
    }
}
/// <summary>
/// Applies the same UIGradientFade effect to every TMP label below a quest-log
/// content object. It also catches labels instantiated later by CluesLog.
/// </summary>
[AddComponentMenu("UI/Effects/Fade Gradient Group")]
[ExecuteAlways]
public class UIGradientFadeGroup : MonoBehaviour
{
    [Header("Quest Log Content")]
    [Tooltip("Leave empty when this component is placed directly on QuestLogContent.")]
    [SerializeField] private Transform questLogContent;

    [Header("Gradient Colors")]
    [SerializeField] private Color leftColor = Color.black;
    [SerializeField] private Color rightColor = new Color(0f, 0f, 0f, 0f);
    [Tooltip("Optional rect that defines where the shared gradient starts and ends.")]
    [SerializeField] private RectTransform gradientBounds;

    private void OnEnable()
    {
        ApplyGradientToRows();
    }

    private void OnValidate()
    {
        ApplyGradientToRows();
    }

    private void OnTransformChildrenChanged()
    {
        ApplyGradientToRows();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
            return;

        Transform content = questLogContent != null ? questLogContent : transform;
        RectTransform bounds = gradientBounds != null ? gradientBounds : content as RectTransform;
        TMPro.TextMeshProUGUI[] labels = content.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        foreach (TMPro.TextMeshProUGUI label in labels)
        {
            UIGradientFade gradient = label.GetComponent<UIGradientFade>();
            if (gradient == null || gradient.GradientBounds != bounds)
            {
                ApplyGradientToRows();
                return;
            }
        }
    }

    public void ApplyGradientToRows()
    {
        Transform content = questLogContent != null ? questLogContent : transform;
        RectTransform bounds = gradientBounds != null ? gradientBounds : content as RectTransform;
        TMPro.TextMeshProUGUI[] labels = content.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);

        foreach (TMPro.TextMeshProUGUI label in labels)
        {
            UIGradientFade gradient = label.GetComponent<UIGradientFade>();
            if (gradient == null)
                gradient = label.gameObject.AddComponent<UIGradientFade>();

            gradient.leftColor = leftColor;
            gradient.rightColor = rightColor;
            gradient.SetGradientBounds(bounds);
            label.SetVerticesDirty();
        }
    }
}
