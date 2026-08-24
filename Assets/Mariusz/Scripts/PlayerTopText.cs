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
    [SerializeField] private Transform ethelAnchor;
    [SerializeField] private Vector3 ethelWorldOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private Transform selmaAnchor;
    [SerializeField] private Vector3 selmaWorldOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private Transform violetAnchor;
    [SerializeField] private Vector3 violetWorldOffset = new Vector3(0f, 2.1f, 0f);

    [Header("Text Fields")]
    public TMP_Text sherlockTopText;
    public TMP_Text watsonTopText;
    public TMP_Text ethelTopText;
    public TMP_Text selmaTopText;
    public TMP_Text violetTopText;

    [Header("Typography")]
    [SerializeField, Min(1f)] private float unifiedFontSize = 24f;

    public float textTime = 3f;
    private Coroutine topTextCoroutine;
    private Coroutine ethelTopTextCoroutine;

    private void Awake()
    {
        Instance = this;
        ConfigureCanvas();
        ConfigureTextTypography(sherlockTopText);
        ConfigureTextTypography(watsonTopText);
        ConfigureTextTypography(ethelTopText);
        ConfigureTextTypography(selmaTopText);
        ConfigureTextTypography(violetTopText);
    }

    private void LateUpdate()
    {
        UpdateTextPosition(sherlockTopText, sherlockAnchor, sherlockWorldOffset);
        UpdateTextPosition(watsonTopText, watsonAnchor, watsonWorldOffset);
        UpdateTextPosition(ethelTopText, ethelAnchor, ethelWorldOffset);
        UpdateTextPosition(selmaTopText, selmaAnchor, selmaWorldOffset);
        UpdateTextPosition(violetTopText, violetAnchor, violetWorldOffset);
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

    public void ShowWatsonTopText(string wText)
    {
        if (topTextCoroutine != null)
            StopCoroutine(topTextCoroutine);

        if (sherlockTopText != null)
            sherlockTopText.text = "";

        if (watsonTopText != null)
            watsonTopText.text = wText;

        topTextCoroutine = StartCoroutine(ClearWatsonTopTextAfterDelay(wText));
    }

    public void ShowEthelTopText(string text, float duration = -1f)
    {
        if (ethelTopTextCoroutine != null)
            StopCoroutine(ethelTopTextCoroutine);

        if (ethelTopText != null)
            ethelTopText.text = text;

        ethelTopTextCoroutine = StartCoroutine(ClearEthelTopTextAfterDelay(text, duration));
    }

    public void ShowSelmaTopTextPersistent(string text)
    {
        if (selmaTopText != null)
            selmaTopText.text = text;
    }

    public void ShowVioletTopTextPersistent(string text)
    {
        if (violetTopText != null)
            violetTopText.text = text;
    }

    public void ClearTopTextIfMatches(string sText = "", string wText = "")
    {
        if (sherlockTopText != null && sherlockTopText.text == sText)
            sherlockTopText.text = "";

        if (watsonTopText != null && watsonTopText.text == wText)
            watsonTopText.text = "";
    }

    public void ClearSelmaTopTextIfMatches(string text)
    {
        if (selmaTopText != null && selmaTopText.text == text)
            selmaTopText.text = "";
    }

    public void ClearVioletTopTextIfMatches(string text)
    {
        if (violetTopText != null && violetTopText.text == text)
            violetTopText.text = "";
    }

    public void ClearAllTopText()
    {
        if (topTextCoroutine != null)
        {
            StopCoroutine(topTextCoroutine);
            topTextCoroutine = null;
        }

        if (ethelTopTextCoroutine != null)
        {
            StopCoroutine(ethelTopTextCoroutine);
            ethelTopTextCoroutine = null;
        }

        if (sherlockTopText != null)
            sherlockTopText.text = "";

        if (watsonTopText != null)
            watsonTopText.text = "";

        if (ethelTopText != null)
            ethelTopText.text = "";

        if (selmaTopText != null)
            selmaTopText.text = "";

        if (violetTopText != null)
            violetTopText.text = "";
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

    private IEnumerator ClearWatsonTopTextAfterDelay(string wText)
    {
        yield return new WaitForSeconds(textTime);

        if (watsonTopText != null && watsonTopText.text == wText)
            watsonTopText.text = "";

        topTextCoroutine = null;
    }

    private IEnumerator ClearEthelTopTextAfterDelay(string text, float duration)
    {
        yield return new WaitForSeconds(duration > 0f ? duration : textTime);

        if (ethelTopText != null && ethelTopText.text == text)
            ethelTopText.text = "";

        ethelTopTextCoroutine = null;
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
