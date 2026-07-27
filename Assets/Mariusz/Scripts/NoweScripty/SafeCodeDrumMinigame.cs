using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SafeCodeDrumMinigame : MonoBehaviour
{
    private static SafeCodeDrumMinigame activeInstance;

    public static bool IsPointerOverActiveBoard =>
        activeInstance != null && activeInstance.IsPointerOverBoard();

    [Header("Existing Lockpick UI")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private LookAtCameraUI lookAtCamera;
    [SerializeField] private LockPickPinView acceptButtonPrefab;

    [Header("Drums")]
    [SerializeField] private string availableSymbols = "1234ABCD";
    [SerializeField] private Color drumBackgroundColor = new(0.18f, 0.12f, 0.07f, 0.96f);
    [SerializeField] private Color drumCenterColor = new(0.30f, 0.21f, 0.11f, 1f);
    [SerializeField] private Color symbolColor = new(0.86f, 0.70f, 0.40f, 1f);
    [SerializeField] private Color inactiveSymbolColor = new(0.55f, 0.43f, 0.27f, 0.72f);
    [SerializeField] private Color buttonColor = new(0.42f, 0.29f, 0.14f, 1f);
    [SerializeField] private float dragStepPixels = 24f;
    [SerializeField] private float tubeAnimationDuration = 0.12f;

    private readonly List<int> drumValues = new();
    private readonly List<RectTransform> drumRects = new();
    private readonly List<RectTransform> tubeContents = new();
    private readonly List<Coroutine> tubeAnimations = new();

    private Camera eventCamera;
    private LockPickAudioController audioController;
    private Action onUnlocked;
    private Action onClosed;
    private RectTransform drumContainer;
    private RectTransform checkButtonRect;
    private string targetCode;
    private bool isOpen;
    private int draggedDrumIndex = -1;
    private float previousDragScreenY;

    public void Open(Camera camera, LockPickAudioController audio, string code, Action unlocked, Action closed)
    {
        ResolveReferences();

        eventCamera = camera != null ? camera : Camera.main;
        audioController = audio;
        onUnlocked = unlocked;
        onClosed = closed;
        targetCode = string.IsNullOrWhiteSpace(code) ? "1A4" : code.Trim().ToUpperInvariant();

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
        }

        if (lookAtCamera != null)
            lookAtCamera.SetCamera(eventCamera);

        BuildDrums();
        isOpen = true;
        activeInstance = this;
        SetStatus("KOD");
    }

    private void Update()
    {
        if (!isOpen || Mouse.current == null || eventCamera == null)
            return;

        Vector2 screenPosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (TryGetDrumIndex(screenPosition, out int drumIndex))
            {
                draggedDrumIndex = drumIndex;
                previousDragScreenY = screenPosition.y;
                return;
            }

            if (IsPointerOver(checkButtonRect, screenPosition))
                ValidateCode();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            draggedDrumIndex = -1;

        if (draggedDrumIndex >= 0 && Mouse.current.leftButton.isPressed)
        {
            float dragDelta = screenPosition.y - previousDragScreenY;
            if (Mathf.Abs(dragDelta) >= dragStepPixels)
            {
                ChangeDrum(draggedDrumIndex, dragDelta > 0f ? 1 : -1);
                previousDragScreenY = screenPosition.y;
            }
        }

        if (TryGetDrumIndex(screenPosition, out int hoveredDrumIndex))
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                ChangeDrum(hoveredDrumIndex, scroll > 0f ? 1 : -1);
        }
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        if (activeInstance == this)
            activeInstance = null;

        onClosed?.Invoke();
    }

    private void ValidateCode()
    {
        string attempt = GetAttempt();
        if (attempt == targetCode)
        {
            isOpen = false;
            if (activeInstance == this)
                activeInstance = null;

            SetStatus("Mechanizm ustapil.");
            onUnlocked?.Invoke();
            return;
        }

        Close();
    }

    private void ChangeDrum(int drumIndex, int direction)
    {
        if (drumIndex < 0 || drumIndex >= drumValues.Count || string.IsNullOrEmpty(availableSymbols))
            return;

        int nextValue = drumValues[drumIndex] + direction;
        if (nextValue < 0)
            nextValue = availableSymbols.Length - 1;
        else if (nextValue >= availableSymbols.Length)
            nextValue = 0;

        drumValues[drumIndex] = nextValue;
        RefreshDrum(drumIndex);
        AnimateTube(drumIndex, direction);
        audioController?.PlayTumblerClick(drumIndex + 1);
    }

    private void BuildDrums()
    {
        if (boardRect == null)
        {
            Debug.LogError($"{name}: BoardPanel is missing.");
            return;
        }

        Transform previousContainer = boardRect.Find("SafeCodeDrums");
        if (previousContainer != null)
            Destroy(previousContainer.gameObject);

        GameObject containerObject = CreateUiObject("SafeCodeDrums", boardRect);
        drumContainer = containerObject.GetComponent<RectTransform>();
        drumContainer.anchorMin = new Vector2(0.5f, 0.5f);
        drumContainer.anchorMax = new Vector2(0.5f, 0.5f);
        drumContainer.pivot = new Vector2(0.5f, 0.5f);
        drumContainer.anchoredPosition = new Vector2(-25f, 10f);
        drumContainer.sizeDelta = new Vector2(400f, 145f);

        drumValues.Clear();
        drumRects.Clear();
        tubeContents.Clear();
        tubeAnimations.Clear();

        for (int index = 0; index < targetCode.Length; index++)
        {
            drumValues.Add(0);
            CreateDrum(index, targetCode.Length);
        }

        CreateCheckButton();
    }

    private void CreateDrum(int drumIndex, int drumCount)
    {
        GameObject drumObject = CreateUiObject("Drum_" + drumIndex, drumContainer);
        Image drumImage = drumObject.AddComponent<Image>();
        drumImage.color = drumBackgroundColor;

        RectTransform drumRect = drumObject.GetComponent<RectTransform>();
        drumRect.anchorMin = new Vector2(0.5f, 0.5f);
        drumRect.anchorMax = new Vector2(0.5f, 0.5f);
        drumRect.pivot = new Vector2(0.5f, 0.5f);
        drumRect.sizeDelta = new Vector2(104f, 136f);
        drumRect.anchoredPosition = new Vector2((drumIndex - (drumCount - 1) * 0.5f) * 120f, 0f);
        drumRects.Add(drumRect);

        GameObject viewportObject = CreateUiObject("TubeViewport", drumRect);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewportRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = new Vector2(96f, 100f);
        viewportObject.AddComponent<RectMask2D>();

        GameObject tubeObject = CreateUiObject("TubeContent", viewportRect);
        RectTransform tubeRect = tubeObject.GetComponent<RectTransform>();
        tubeRect.anchorMin = new Vector2(0.5f, 0.5f);
        tubeRect.anchorMax = new Vector2(0.5f, 0.5f);
        tubeRect.pivot = new Vector2(0.5f, 0.5f);
        tubeRect.anchoredPosition = Vector2.zero;
        tubeRect.sizeDelta = viewportRect.sizeDelta;
        tubeContents.Add(tubeRect);

        CreateDrumSymbol("Previous", tubeRect, new Vector2(0f, 38f), 24f, inactiveSymbolColor);

        GameObject centerObject = CreateUiObject("Selected", tubeRect);
        Image centerImage = centerObject.AddComponent<Image>();
        centerImage.color = drumCenterColor;
        RectTransform centerRect = centerObject.GetComponent<RectTransform>();
        centerRect.anchorMin = new Vector2(0.5f, 0.5f);
        centerRect.anchorMax = new Vector2(0.5f, 0.5f);
        centerRect.pivot = new Vector2(0.5f, 0.5f);
        centerRect.anchoredPosition = Vector2.zero;
        centerRect.sizeDelta = new Vector2(96f, 50f);
        CreateDrumSymbol("Current", centerRect, Vector2.zero, 40f, symbolColor);

        CreateDrumSymbol("Next", tubeRect, new Vector2(0f, -38f), 24f, inactiveSymbolColor);
        RefreshDrum(drumIndex);
    }

    private void CreateCheckButton()
    {
        if (acceptButtonPrefab != null)
        {
            LockPickPinView acceptButton = Instantiate(acceptButtonPrefab, boardRect);
            acceptButton.name = "CheckCode";
            acceptButton.Initialize(0, "OK");
            acceptButton.SetRingVisible(false);
            checkButtonRect = acceptButton.GetComponent<RectTransform>();
        }
        else
        {
            GameObject buttonObject = CreateUiObject("CheckCode", boardRect);
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = buttonColor;
            checkButtonRect = buttonObject.GetComponent<RectTransform>();
            CreateDrumSymbol("Label", checkButtonRect, Vector2.zero, 22f, symbolColor, "OK");
        }

        checkButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkButtonRect.pivot = new Vector2(0.5f, 0.5f);
        checkButtonRect.anchoredPosition = new Vector2(225f, 10f);
        checkButtonRect.sizeDelta = new Vector2(68f, 68f);
    }

    private void RefreshDrum(int drumIndex)
    {
        if (drumIndex < 0 || drumIndex >= drumRects.Count || string.IsNullOrEmpty(availableSymbols))
            return;

        RectTransform tubeRect = tubeContents[drumIndex];
        int currentIndex = drumValues[drumIndex];
        SetText(tubeRect, "Previous", SymbolAt(currentIndex - 1));
        SetText(tubeRect, "Selected/Current", SymbolAt(currentIndex));
        SetText(tubeRect, "Next", SymbolAt(currentIndex + 1));
    }

    private string GetAttempt()
    {
        string attempt = string.Empty;
        foreach (int value in drumValues)
            attempt += SymbolAt(value);

        return attempt;
    }

    private string SymbolAt(int index)
    {
        if (string.IsNullOrEmpty(availableSymbols))
            return string.Empty;

        index %= availableSymbols.Length;
        if (index < 0)
            index += availableSymbols.Length;

        return availableSymbols[index].ToString();
    }

    private void ResolveReferences()
    {
        if (lookAtCamera == null)
            lookAtCamera = GetComponent<LookAtCameraUI>();

        if (!IsPrefabChild(boardRect))
            boardRect = FindRectTransform("BoardPanel");
    }

    private bool TryGetDrumIndex(Vector2 screenPosition, out int drumIndex)
    {
        for (int index = 0; index < drumRects.Count; index++)
        {
            if (IsPointerOver(drumRects[index], screenPosition))
            {
                drumIndex = index;
                return true;
            }
        }

        drumIndex = -1;
        return false;
    }

    private void AnimateTube(int drumIndex, int direction)
    {
        if (drumIndex < 0 || drumIndex >= tubeContents.Count)
            return;

        while (tubeAnimations.Count <= drumIndex)
            tubeAnimations.Add(null);

        if (tubeAnimations[drumIndex] != null)
            StopCoroutine(tubeAnimations[drumIndex]);

        tubeAnimations[drumIndex] = StartCoroutine(AnimateTubeRoutine(tubeContents[drumIndex], direction));
    }

    private IEnumerator AnimateTubeRoutine(RectTransform tube, int direction)
    {
        Vector2 startPosition = new Vector2(0f, -Mathf.Sign(direction) * 34f);
        tube.anchoredPosition = startPosition;

        float elapsed = 0f;
        while (elapsed < tubeAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / tubeAnimationDuration);
            tube.anchoredPosition = Vector2.Lerp(startPosition, Vector2.zero, t * t * (3f - 2f * t));
            yield return null;
        }

        tube.anchoredPosition = Vector2.zero;
    }

    private bool IsPointerOver(RectTransform rectTransform, Vector2 screenPosition)
    {
        return rectTransform != null &&
               RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera);
    }

    private bool IsPointerOverBoard()
    {
        return isOpen && Mouse.current != null &&
               IsPointerOver(boardRect, Mouse.current.position.ReadValue());
    }

    private void OnDestroy()
    {
        if (activeInstance == this)
            activeInstance = null;
    }

    private bool IsPrefabChild(RectTransform rectTransform)
    {
        return rectTransform != null &&
               (rectTransform.transform == transform || rectTransform.transform.IsChildOf(transform));
    }

    private RectTransform FindRectTransform(string objectName)
    {
        foreach (RectTransform rectTransform in GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform.name == objectName)
                return rectTransform;
        }

        return null;
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void CreateDrumSymbol(string objectName, RectTransform parent, Vector2 position, float fontSize, Color color, string text = "")
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = position;
        textRect.sizeDelta = parent.sizeDelta;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.text = text;
    }

    private void SetText(RectTransform parent, string path, string text)
    {
        Transform target = parent.Find(path);
        TMP_Text label = target != null ? target.GetComponent<TMP_Text>() : null;
        if (label != null)
            label.text = text;
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
