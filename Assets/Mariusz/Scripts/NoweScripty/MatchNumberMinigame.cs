using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MatchNumberMinigame : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private float hitRadius = 34f;
    [SerializeField] private float resetDelay = 0.7f;

    [Header("UI References")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private RectTransform pinGridRect;
    [SerializeField] private GridLayoutGroup pinGridLayout;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private LockPickPinView pinPrefab;
    [SerializeField] private UILine linePrefab;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private LookAtCameraUI lookAtCamera;

    [Header("Symbol Lock")]
    [SerializeField] private string startingSymbol = ".";
    [SerializeField] private string firstRowSymbols = "ABCDE";
    [SerializeField] private string secondRowSymbols = ".FGHIJ";
    [SerializeField] private int connectionsPerAttempt = 4;
    [SerializeField] private Color lineColor = new(0.84f, 0.70f, 0.42f, 1f);

    private readonly List<LockPickPinView> pins = new();
    private readonly List<string> targetPath = new();
    private readonly List<string> currentPath = new();

    private Camera eventCamera;
    private LockPickAudioController audioController;
    private Action onUnlocked;
    private Action onClosed;
    private UILine previewLine;
    private bool isOpen;
    private bool isResetting;
    private Vector2 pointerPosition;

    public void Open(Camera camera, LockPickAudioController audio, string code, Action unlocked, Action closed)
    {
        ResolvePrefabReferences();
        eventCamera = camera != null ? camera : Camera.main;
        audioController = audio;
        onUnlocked = unlocked;
        onClosed = closed;

        BuildTargetPath(code);

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eventCamera;
        }

        if (lookAtCamera != null)
            lookAtCamera.SetCamera(eventCamera);

        BuildPins();
        ResetAttempt();
        isOpen = true;
        SetStatus("Rozpocznij od kropki.");
    }

    private void ResolvePrefabReferences()
    {
        if (lookAtCamera == null)
            lookAtCamera = GetComponent<LookAtCameraUI>();

        if (!IsPrefabChild(boardRect))
            boardRect = FindRectTransform("BoardPanel");

        if (!IsPrefabChild(pinGridRect))
            pinGridRect = FindRectTransform("PinGrid");

        if (pinGridLayout == null && pinGridRect != null)
            pinGridLayout = pinGridRect.GetComponent<GridLayoutGroup>();

        if (!IsPrefabChild(lineContainer))
            lineContainer = FindRectTransform("LineContainer");
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

    private void Update()
    {
        if (!isOpen || isResetting || Mouse.current == null)
            return;

        // Matches the Level 2 lockpick: a regular world click closes the board,
        // then PlayerController receives that same click and can move Sherlock away.
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (!TryGetPointerLinePosition(out pointerPosition))
            return;

        UpdatePreviewLine(pointerPosition);

        LockPickPinView pin = GetPinUnderPointer(pointerPosition);
        if (pin == null || currentPath.Count == 0 || pin.Label == currentPath[^1])
            return;

        string currentSymbol = currentPath[^1];
        RemovePreviewLine();
        DrawLine(currentSymbol, pin.Label);
        currentPath.Add(pin.Label);
        audioController?.PlayTumblerClick(currentPath.Count);

        if (currentPath.Count - 1 >= connectionsPerAttempt)
        {
            FinishAttempt();
            return;
        }

        CreatePreviewLine();
        SetStatus("Jeszcze " + (connectionsPerAttempt - (currentPath.Count - 1)) + ".");
    }

    private void FinishAttempt()
    {
        RemovePreviewLine();

        if (PathsMatch())
        {
            isOpen = false;
            SetStatus("Mechanizm odblokowany.");
            onUnlocked?.Invoke();
            return;
        }

        isResetting = true;
        SetStatus("Mechanizm nie ustapil. Sprobuj ponownie.");
        audioController?.PlayReset();
        StartCoroutine(ResetAfterDelay());
    }

    private bool PathsMatch()
    {
        if (currentPath.Count != targetPath.Count)
            return false;

        for (int index = 0; index < targetPath.Count; index++)
        {
            if (!string.Equals(currentPath[index], targetPath[index], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        isResetting = false;
        ResetAttempt();
        SetStatus("Rozpocznij od kropki.");
    }

    private void ResetAttempt()
    {
        currentPath.Clear();
        currentPath.Add(startingSymbol);
        RemovePreviewLine();
        ClearLines();

        foreach (LockPickPinView pin in pins)
            pin.SetState(LockPickPinState.Normal);

        pointerPosition = GetPinLinePosition(GetPin(startingSymbol));
        CreatePreviewLine();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        onClosed?.Invoke();
    }

    private void BuildTargetPath(string code)
    {
        targetPath.Clear();
        targetPath.Add(startingSymbol);

        string resolvedCode = string.IsNullOrWhiteSpace(code) ? "AGDC" : code.Trim();
        foreach (char symbol in resolvedCode)
        {
            if (!char.IsWhiteSpace(symbol) && symbol.ToString() != startingSymbol)
                targetPath.Add(symbol.ToString().ToUpperInvariant());
        }

        connectionsPerAttempt = Mathf.Max(1, targetPath.Count - 1);
    }

    private void BuildPins()
    {
        if (pinGridRect == null || pinPrefab == null)
            return;

        if (pinGridLayout != null)
        {
            pinGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            pinGridLayout.constraintCount = 6;
        }

        foreach (Transform child in pinGridRect)
            Destroy(child.gameObject);

        pins.Clear();

        int id = 0;
        foreach (char symbol in firstRowSymbols)
            CreatePin(++id, symbol.ToString());

        // Keeps the starting dot at the beginning of the second row: ABCDE [blank] / .FGHIJ.
        GameObject spacer = new GameObject("SymbolLock_Spacer", typeof(RectTransform), typeof(LayoutElement));
        spacer.transform.SetParent(pinGridRect, false);

        foreach (char symbol in secondRowSymbols)
            CreatePin(++id, symbol.ToString());

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(pinGridRect);
    }

    private void CreatePin(int id, string symbol)
    {
        LockPickPinView pin = Instantiate(pinPrefab, pinGridRect);
        pin.Initialize(id, symbol.ToUpperInvariant());
        pin.SetRingVisible(false);
        pins.Add(pin);
    }

    private bool TryGetPointerLinePosition(out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (eventCamera == null || boardRect == null || lineContainer == null)
            return false;

        if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(boardRect, Mouse.current.position.ReadValue(), eventCamera, out Vector3 worldPoint))
            return false;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, worldPoint);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, screenPoint, eventCamera, out localPoint);
    }

    private LockPickPinView GetPinUnderPointer(Vector2 point)
    {
        LockPickPinView closest = null;
        float closestDistance = float.MaxValue;

        foreach (LockPickPinView pin in pins)
        {
            float distance = Vector2.Distance(point, GetPinLinePosition(pin));
            if (distance <= hitRadius && distance < closestDistance)
            {
                closest = pin;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private LockPickPinView GetPin(string symbol)
    {
        return pins.Find(pin => pin.Label == symbol);
    }

    private Vector2 GetPinLinePosition(LockPickPinView pin)
    {
        if (pin == null || eventCamera == null || lineContainer == null)
            return Vector2.zero;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, pin.transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, screenPoint, eventCamera, out Vector2 localPoint);
        return localPoint;
    }

    private void CreatePreviewLine()
    {
        if (currentPath.Count == 0)
            return;

        previewLine = CreateLineObject();
        if (previewLine == null)
            return;

        previewLine.SetPoints(GetPinLinePosition(GetPin(currentPath[^1])), pointerPosition);
        previewLine.color = lineColor;
    }

    private void UpdatePreviewLine(Vector2 point)
    {
        if (previewLine != null && currentPath.Count > 0)
            previewLine.SetPoints(GetPinLinePosition(GetPin(currentPath[^1])), point);
    }

    private void DrawLine(string from, string to)
    {
        UILine line = CreateLineObject();
        if (line == null)
            return;

        line.SetPoints(GetPinLinePosition(GetPin(from)), GetPinLinePosition(GetPin(to)));
        line.color = lineColor;
    }

    private UILine CreateLineObject()
    {
        if (linePrefab == null || lineContainer == null)
            return null;

        UILine line = Instantiate(linePrefab, lineContainer);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return line;
    }

    private void RemovePreviewLine()
    {
        if (previewLine == null)
            return;

        Destroy(previewLine.gameObject);
        previewLine = null;
    }

    private void ClearLines()
    {
        if (lineContainer == null)
            return;

        foreach (Transform child in lineContainer)
            Destroy(child.gameObject);

        previewLine = null;
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
