using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerTopText : MonoBehaviour
{
    public static PlayerTopText Instance;

    [Header("Screen Space UI")]
    [SerializeField] private Canvas topTextCanvas;
    [SerializeField, Min(0)] private int sortingOrder = 100;
    [SerializeField] private Camera projectionCamera;

    [Header("Character Anchors")]
    [SerializeField] private Transform sherlockAnchor;
    [SerializeField] private Vector3 sherlockWorldOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private Transform watsonAnchor;
    [SerializeField] private Vector3 watsonWorldOffset = new Vector3(0f, 2.1f, 0f);

    [Header("Text Fields")]
    public TMP_Text sherlockTopText;
    public TMP_Text watsonTopText;

    [Header("Typography")]
    [SerializeField, Min(1f)] private float unifiedFontSize = 24f;

    public float textTime = 3f;
    private Coroutine topTextCoroutine;

    private void Awake()
    {
        Instance = this;
        ConfigureCanvas();
        ConfigureTextTypography(sherlockTopText);
        ConfigureTextTypography(watsonTopText);
    }

    private void LateUpdate()
    {
        UpdateTextPosition(sherlockTopText, sherlockAnchor, sherlockWorldOffset);
        UpdateTextPosition(watsonTopText, watsonAnchor, watsonWorldOffset);
    }

    public void ShowTopText(string sText = "", string wText = "")
    {
        if (topTextCoroutine != null)
            StopCoroutine(topTextCoroutine);

        topTextCoroutine = StartCoroutine(ShowTopTextCor(sText, wText));
    }

    public void ShowTopTextPersistent(string sText = "", string wText = "")
    {
        if (topTextCoroutine != null)
        {
            StopCoroutine(topTextCoroutine);
            topTextCoroutine = null;
        }

        if (sherlockTopText != null)
            sherlockTopText.text = sText;

        if (watsonTopText != null)
            watsonTopText.text = wText;
    }

    public void ClearTopTextIfMatches(string sText = "", string wText = "")
    {
        if (sherlockTopText != null && sherlockTopText.text == sText)
            sherlockTopText.text = "";

        if (watsonTopText != null && watsonTopText.text == wText)
            watsonTopText.text = "";
    }

    public IEnumerator ShowTopTextCor(string sText, string wText)
    {
        if (sherlockTopText != null)
            sherlockTopText.text = sText;

        yield return new WaitForSeconds(textTime);

        if (sherlockTopText != null)
            sherlockTopText.text = "";

        if (watsonTopText != null)
        {
            watsonTopText.text = wText;
            yield return new WaitForSeconds(textTime);
            watsonTopText.text = "";
        }

        topTextCoroutine = null;
    }

    private void ConfigureCanvas()
    {
        if (topTextCanvas == null)
            return;

        topTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        topTextCanvas.overrideSorting = true;
        topTextCanvas.sortingOrder = sortingOrder;
    }

    private void ConfigureTextTypography(TMP_Text text)
    {
        if (text == null)
            return;

        text.enableAutoSizing = false;
        text.fontSize = unifiedFontSize;
    }

    private void UpdateTextPosition(TMP_Text text, Transform anchor, Vector3 worldOffset)
    {
        if (topTextCanvas == null || text == null || anchor == null)
            return;

        Camera cameraToUse = projectionCamera != null ? projectionCamera : Camera.main;
        if (cameraToUse == null)
            return;

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(anchor.position + worldOffset);
        bool isInFrontOfCamera = screenPosition.z > 0f;
        text.enabled = isInFrontOfCamera;

        if (!isInFrontOfCamera)
            return;

        RectTransform canvasRect = topTextCanvas.transform as RectTransform;
        Camera canvasCamera = topTextCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : topTextCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            text.rectTransform.anchoredPosition = localPosition;
        }
    }
}
