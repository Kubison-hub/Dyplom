using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class LockPickMinigameController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int lockFieldCount = 8;       // 6 albo 8
    [SerializeField] private int sequenceLength = 6;       // 4 do lockFieldCount
    [SerializeField] private int testsToOpen = 1;
    [SerializeField] private bool showDebugCode = true;

    
    [SerializeField] private bool showRings = true;        // w³¹cza/wy³¹cza podgl¹d ringów
    [SerializeField] private bool keepCode = false;        // po b³êdzie zachowuje ten sam kod

    [Header("Input")]
    [SerializeField] private float hitRadius = 34f;

    [Header("Ring Logic")]
    [SerializeField] private float ringGapSize = 58f;
    [SerializeField] private float ringSpinDegreesPerPixel = 2.15f;
    [SerializeField] private float wrongRingOffset = 112f;

    [Header("Timing")]
    [SerializeField] private float resetDelay = 0.7f;
    [SerializeField] private float nextTestDelay = 0.7f;

    [Header("UI References")]
    [SerializeField] private RectTransform boardRect;
    [SerializeField] private RectTransform pinGridRect;
    [SerializeField] private GridLayoutGroup pinGridLayout;
    [SerializeField] private RectTransform lineContainer;
    [SerializeField] private LockPickPinView pinPrefab;
    [SerializeField] private UILine linePrefab;
    [SerializeField] private TMP_Text statusText;

    [Header("World UI")]
    [SerializeField] private LookAtCameraUI lookAtCamera;

    [Header("Audio")]
    [SerializeField] private LockPickAudioController audioController;
   
    [SerializeField] private float minPointerMoveForAudio = 1.5f;
    [SerializeField] private float maxPointerMoveForAudio = 38f;

    private readonly int[] layout5 = { 4, 5, 6, 1, 2 };
    private readonly int[] layout6 = { 4, 5, 6, 1, 2, 3 };
    private readonly int[] layout8 = { 5, 6, 7, 8, 1, 2, 3, 4 };

    private readonly List<LockPickPinView> pins = new();
    private readonly List<int> targetCode = new();
    private readonly List<int> currentPath = new();
    private readonly Dictionary<int, float> ringBaseRotations = new();

    private Camera eventCamera;
    private Action onUnlocked;
    private Action onClosed;

    private int completedTests;
    private bool canInteract;
    private Vector2 lastPointerLocal;
    private UILine previewLine;

    private Vector2 previousPointerLocal;
    private bool hasPreviousPointer;

    private int[] ActiveLayout
    {
        get
        {
            return lockFieldCount switch
            {
                5 => layout5,
                6 => layout6,
                8 => layout8,
                _ => layout8
            };
        }
    }

    public void Open(
    Camera camera,
    LockPickAudioController audio,
    Action onUnlocked,
    Action onClosed,
     
    int fields,
    int length
)
    {
        lockFieldCount = fields;
        sequenceLength = length;
        eventCamera = camera;
        audioController = audio;
        this.onUnlocked = onUnlocked;
        this.onClosed = onClosed;

        lockFieldCount = lockFieldCount switch
        {
            5 => 5,
            6 => 6,
            8 => 8,
            _ => 8
        };

        sequenceLength = Mathf.Clamp(sequenceLength, 4, lockFieldCount);
        sequenceLength = Mathf.Clamp(sequenceLength, 4, lockFieldCount);

        Canvas canvas = GetComponentInChildren<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
        }

        if (lookAtCamera != null)
            lookAtCamera.SetCamera(camera);

        BuildPins();
        ApplyRingVisibility();
        StartLock();
    }

    private void Update()
    {
        if (!canInteract)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            onClosed?.Invoke();
            return;
        }

        if (!TryGetPointerLinePosition(out Vector2 pointerLocal))
            return;

        float pointerMoveDelta = hasPreviousPointer
    ? Vector2.Distance(pointerLocal, previousPointerLocal)
    : 0f;

        float movement01 = Mathf.InverseLerp(
            minPointerMoveForAudio,
            maxPointerMoveForAudio,
            pointerMoveDelta
        );

        previousPointerLocal = pointerLocal;
        hasPreviousPointer = true;

        lastPointerLocal = pointerLocal;

        if (showRings)
            ApplyRingRotations(pointerLocal);

        UpdatePreviewLine(pointerLocal);
        PlayPickWorkAudio(pointerLocal, movement01);

        UpdatePreviewLine(pointerLocal);


      
        LockPickPinView hoveredPin = GetPinUnderPointer(pointerLocal);

        if (hoveredPin != null)
            TryConnect(hoveredPin.Id);
    }

    private void PlayPickWorkAudio(Vector2 pointerLocal, float movement01)
    {
        if (audioController == null)
            return;

        if (movement01 <= 0f)
            return;

        int currentPinId = currentPath[^1];
        Vector2 currentCenter = GetPinLinePosition(currentPinId);

        float distance = Vector2.Distance(pointerLocal, currentCenter);
        float normalizedDistance = Mathf.Clamp01(distance / 420f);

        audioController.PlayMoveWork(normalizedDistance, movement01);
    }

    private void BuildPins()
    {
        ApplyGridSettings();

        foreach (Transform child in pinGridRect)
        {
            Destroy(child.gameObject);
        }

        pins.Clear();

        foreach (int id in ActiveLayout)
        {
            LockPickPinView pin = Instantiate(pinPrefab, pinGridRect);
            pin.Initialize(id);
            pin.SetRingVisible(showRings);
            pins.Add(pin);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(pinGridRect);
    }

    private void ApplyGridSettings()
    {
        if (pinGridLayout == null)
            return;

        pinGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        pinGridLayout.constraintCount = lockFieldCount switch
        {
            5 => 3,
            6 => 3,
            8 => 4,
            _ => 4
        };
    }

    private void StartLock()
    {
        completedTests = 0;
        GenerateNewTest(true);
    }

    private void GenerateNewTest(bool forceNewCode)
    {
        if (forceNewCode || targetCode.Count == 0)
            GenerateCode();

        ResetAttempt();

        SetStatus($"Test {completedTests + 1}/{testsToOpen}");
        UpdateDebugStatus();
    }

    private void GenerateCode()
    {
        targetCode.Clear();

        List<int> available = new List<int>(ActiveLayout);
        available.Remove(1);

        targetCode.Add(1);

        while (targetCode.Count < sequenceLength && available.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, available.Count);

            targetCode.Add(available[index]);
            available.RemoveAt(index);
        }
    }

    private void ResetAttempt()
    {
        currentPath.Clear();
        currentPath.Add(1);

        canInteract = true;

        ClearLines();
        ClearPinStates();

        GetPin(1).SetState(LockPickPinState.Active);

        lastPointerLocal = GetPinLinePosition(1);

        if (showRings)
        {
            CalculateRingPhasesForCurrentStep();
            ApplyRingRotations(lastPointerLocal);
        }

        CreatePreviewLine();
        UpdateDebugStatus();
    }

    private void TryConnect(int pinId)
    {
        if (!canInteract)
            return;

        int currentPinId = currentPath[^1];

        if (pinId == currentPinId)
            return;

        if (currentPath.Contains(pinId))
            return;

        if (currentPath.Count >= targetCode.Count)
            return;

        int expectedPinId = targetCode[currentPath.Count];

        RemovePreviewLine();

        if (pinId != expectedPinId)
        {
            DrawLine(currentPinId, pinId, LockPickLineState.Error);
            OnFail(pinId);
            return;
        }

        DrawLine(currentPinId, pinId, LockPickLineState.Success);

        currentPath.Add(pinId);
        GetPin(pinId).SetState(LockPickPinState.Active);

        if (audioController != null)
            audioController.PlayTumblerClick(currentPath.Count);

        if (showRings)
        {
            CalculateRingPhasesForCurrentStep();
            ApplyRingRotations(lastPointerLocal);
        }

        if (currentPath.Count >= targetCode.Count)
        {
            OnTestComplete();
            return;
        }

        CreatePreviewLine();
        SetStatus("Dobrze");
        UpdateDebugStatus();
    }

    private void OnTestComplete()
    {

        if (audioController != null)
            audioController.PlayTestComplete();

        completedTests++;
        canInteract = false;

        RemovePreviewLine();

        if (completedTests >= testsToOpen)
        {
            SetStatus("Zamek otwarty");
            onUnlocked?.Invoke();
            return;
        }

        SetStatus($"Sekwencja poprawna. Test {completedTests}/{testsToOpen}");

        Invoke(nameof(GenerateNextTestAfterDelay), nextTestDelay);
    }

    private void GenerateNextTestAfterDelay()
    {
        GenerateNewTest(true);
    }

    private void OnFail(int wrongPinId)
    {
        if (audioController != null)
            audioController.PlayReset();

        canInteract = false;

        foreach (int id in currentPath)
            GetPin(id).SetState(LockPickPinState.Error);

        GetPin(wrongPinId).SetState(LockPickPinState.Error);

        SetStatus("B³¹d. Reset zamka.");

        Invoke(nameof(RestartAfterFail), resetDelay);
    }

    private void RestartAfterFail()
    {
        completedTests = 0;

        // keepCode = true  -> zostaje ten sam targetCode
        // keepCode = false -> generuje nowy targetCode
        GenerateNewTest(!keepCode);
    }

    private void CalculateRingPhasesForCurrentStep()
    {
        ringBaseRotations.Clear();

        if (!showRings)
            return;

        int currentPinId = currentPath[^1];

        if (currentPath.Count >= targetCode.Count)
            return;

        int nextCorrectPinId = targetCode[currentPath.Count];

        for (int i = 0; i < ActiveLayout.Length; i++)
        {
            int id = ActiveLayout[i];

            if (currentPath.Contains(id))
                continue;

            float contactDistance = GetContactDistance(currentPinId, id);
            float targetGapStart = GetGapStartAngleForConnection(currentPinId, id);
            float baseForPerfectContact = targetGapStart - contactDistance * ringSpinDegreesPerPixel;

            if (id == nextCorrectPinId)
            {
                ringBaseRotations[id] = baseForPerfectContact;
            }
            else
            {
                ringBaseRotations[id] = baseForPerfectContact + wrongRingOffset + i * 37f;
            }
        }
    }

    private void ApplyRingRotations(Vector2 pointerLocal)
    {
        if (!showRings)
            return;

        int currentPinId = currentPath[^1];
        Vector2 currentCenter = GetPinLinePosition(currentPinId);

        float pointerDistance = Vector2.Distance(pointerLocal, currentCenter);

        foreach (LockPickPinView pin in pins)
        {
            if (currentPath.Contains(pin.Id))
                continue;

            if (!ringBaseRotations.TryGetValue(pin.Id, out float baseRotation))
                continue;

            float angle = baseRotation + pointerDistance * ringSpinDegreesPerPixel;
            pin.SetRingRotation(angle);
        }
    }

    private void ApplyRingVisibility()
    {
        foreach (LockPickPinView pin in pins)
            pin.SetRingVisible(showRings);
    }

    private float GetContactDistance(int fromId, int toId)
    {
        Vector2 from = GetPinLinePosition(fromId);
        Vector2 to = GetPinLinePosition(toId);

        return Mathf.Max(0f, Vector2.Distance(from, to) - hitRadius);
    }

    private float GetGapStartAngleForConnection(int fromId, int toId)
    {
        Vector2 from = GetPinLinePosition(toId);
        Vector2 to = GetPinLinePosition(fromId);

        Vector2 direction = to - from;

        float angleFromTargetToCurrent = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float gapCenterAngle = angleFromTargetToCurrent + 90f;

        return gapCenterAngle - ringGapSize / 2f;
    }

    private bool TryGetPointerLinePosition(out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        if (eventCamera == null)
            return false;

        bool hasWorldPoint = RectTransformUtility.ScreenPointToWorldPointInRectangle(
            boardRect,
            Mouse.current.position.ReadValue(),
            eventCamera,
            out Vector3 boardWorldPoint
        );

        if (!hasWorldPoint)
            return false;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            boardWorldPoint
        );

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineContainer,
            screenPoint,
            eventCamera,
            out Vector2 rawLocalPoint
        );

        if (!converted)
            return false;

        localPoint = ClampLinePointToBoard(rawLocalPoint);
        return true;
    }

    private Vector2 GetPinLinePosition(int id)
    {
        LockPickPinView pin = GetPin(id);

        if (pin == null)
            return Vector2.zero;

        RectTransform pinRect = pin.GetComponent<RectTransform>();

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
            eventCamera,
            pinRect.position
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            lineContainer,
            screenPoint,
            eventCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private LockPickPinView GetPinUnderPointer(Vector2 pointerLocal)
    {
        LockPickPinView closestPin = null;
        float closestDistance = float.MaxValue;

        foreach (LockPickPinView pin in pins)
        {
            float distance = Vector2.Distance(pointerLocal, GetPinLinePosition(pin.Id));

            if (distance <= hitRadius && distance < closestDistance)
            {
                closestPin = pin;
                closestDistance = distance;
            }
        }

        return closestPin;
    }

    private LockPickPinView GetPin(int id)
    {
        return pins.Find(pin => pin.Id == id);
    }

    private UILine CreateLineObject()
    {
        UILine line = Instantiate(linePrefab, lineContainer);

        RectTransform rect = line.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchoredPosition = Vector2.zero;

        return line;
    }

    private void CreatePreviewLine()
    {
        previewLine = CreateLineObject();

        previewLine.SetPoints(
            GetPinLinePosition(currentPath[^1]),
            lastPointerLocal
        );

        SetLineColor(previewLine, LockPickLineState.Preview);
    }

    private void UpdatePreviewLine(Vector2 pointerLocal)
    {
        if (previewLine == null)
            return;

        previewLine.SetPoints(
            GetPinLinePosition(currentPath[^1]),
            pointerLocal
        );
    }

    private void RemovePreviewLine()
    {
        if (previewLine == null)
            return;

        Destroy(previewLine.gameObject);
        previewLine = null;
    }

    private void DrawLine(int fromId, int toId, LockPickLineState state)
    {
        UILine line = CreateLineObject();

        line.SetPoints(
            GetPinLinePosition(fromId),
            GetPinLinePosition(toId)
        );

        SetLineColor(line, state);
    }

    private void SetLineColor(UILine line, LockPickLineState state)
    {
        if (line == null)
            return;

        line.color = state switch
        {
            LockPickLineState.Preview => new Color(0.84f, 0.70f, 0.42f, 1f),
            LockPickLineState.Success => new Color(0.31f, 0.71f, 0.45f, 1f),
            LockPickLineState.Error => new Color(0.71f, 0.30f, 0.26f, 1f),
            _ => Color.white
        };
    }

    private void ClearLines()
    {
        foreach (Transform child in lineContainer)
        {
            Destroy(child.gameObject);
        }

        previewLine = null;
    }

    private void ClearPinStates()
    {
        foreach (LockPickPinView pin in pins)
        {
            pin.SetState(LockPickPinState.Normal);
            pin.SetRingVisible(showRings);
        }
    }

    private void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    private void UpdateDebugStatus()
    {
        if (!showDebugCode || statusText == null)
            return;

        string code = string.Join("", targetCode);
        string attempt = string.Join("", currentPath);

        statusText.text =
            $"Kod: {code} | Próba: {attempt} | Test {completedTests + 1}/{testsToOpen}";
    }

    private Vector2 ClampLinePointToBoard(Vector2 linePoint)
    {
        Vector3[] corners = new Vector3[4];
        boardRect.GetWorldCorners(corners);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                corners[i]
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                lineContainer,
                screenPoint,
                eventCamera,
                out Vector2 localCorner
            );

            min = Vector2.Min(min, localCorner);
            max = Vector2.Max(max, localCorner);
        }

        return new Vector2(
            Mathf.Clamp(linePoint.x, min.x, max.x),
            Mathf.Clamp(linePoint.y, min.y, max.y)
        );
    }

}

public enum LockPickLineState
{
    Preview,
    Success,
    Error
}