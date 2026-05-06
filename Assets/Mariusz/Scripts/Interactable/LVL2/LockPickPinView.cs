using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LockPickPinView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image background;
    [SerializeField] private RectTransform ringTransform;
    [SerializeField] private Image ringImage;
    [SerializeField] private TMP_Text numberText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.69f, 0.54f, 0.29f, 1f);
    [SerializeField] private Color activeColor = new Color(0.31f, 0.71f, 0.45f, 1f);
    [SerializeField] private Color errorColor = new Color(0.71f, 0.30f, 0.26f, 1f);

    [SerializeField] private Color normalRingColor = new Color(0.84f, 0.70f, 0.42f, 1f);
    [SerializeField] private Color activeRingColor = new Color(0.57f, 0.91f, 0.68f, 1f);
    [SerializeField] private Color errorRingColor = new Color(0.94f, 0.60f, 0.56f, 1f);

    public int Id { get; private set; }

    public void Initialize(int id)
    {
        Id = id;

        if (numberText != null)
            numberText.text = id.ToString();

        SetState(LockPickPinState.Normal);
        SetRingRotation(0f);
    }

    public void SetState(LockPickPinState state)
    {
        switch (state)
        {
            case LockPickPinState.Active:
                SetColor(activeColor, activeRingColor);
                SetRingRotation(0f);
                break;

            case LockPickPinState.Error:
                SetColor(errorColor, errorRingColor);
                break;

            default:
                SetColor(normalColor, normalRingColor);
                break;
        }
    }

    public void SetRingRotation(float angle)
    {
        if (ringTransform == null)
            return;

        ringTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetColor(Color pinColor, Color ringColor)
    {
        if (background != null)
            background.color = pinColor;

        if (ringImage != null)
            ringImage.color = ringColor;
    }

    public void SetRingVisible(bool visible)
    {
        if (ringTransform != null)
            ringTransform.gameObject.SetActive(visible);
    }
}

public enum LockPickPinState
{
    Normal,
    Active,
    Error
}