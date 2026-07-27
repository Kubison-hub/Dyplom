using TMPro;
using UnityEngine;

/// <summary>
/// Optional presentation data for an Interactable hover label.
/// Add it only to objects that should receive the detective-style callout.
/// </summary>
[RequireComponent(typeof(Interactable))]
public class InteractionHoverLabel : MonoBehaviour
{
    [Header("Content")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string interactionHint = "Zbadaj";
    [SerializeField] private TMP_FontAsset labelFont;
    [SerializeField, Min(0.01f)] private float labelFontSize = 0.18f;

    [Header("Placement")]
    [SerializeField] private Transform labelAnchor;
    [SerializeField] private Vector2 screenOffset = new Vector2(170f, 62f);
    [SerializeField, Min(0f)] private float screenMargin = 48f;
    [SerializeField, Min(0.05f)] private float elbowLength = 0.45f;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            Interactable interactable = GetComponent<Interactable>();
            return interactable != null ? interactable.objectDescription : gameObject.name;
        }
    }

    public string InteractionHint => interactionHint;
    public Transform LabelAnchor => labelAnchor != null ? labelAnchor : transform;
    public TMP_FontAsset LabelFont => labelFont;
    public float LabelFontSize => labelFontSize;
    public Vector2 ScreenOffset => screenOffset;
    public float ScreenMargin => screenMargin;
    public float ElbowLength => elbowLength;

    private void Reset()
    {
        labelAnchor = transform;
    }
}
