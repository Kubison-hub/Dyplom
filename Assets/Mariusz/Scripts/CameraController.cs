using System;
using DialogueEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private string currentZoomState;
    [SerializeField] private int currentZoomIndex;

    private CinemachineCamera cineCamera;
    private CinemachineOrbitalFollow orbitalFollow;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float rotationDamping = 0.15f;
    [SerializeField] private bool autoRotateCamera = true;
    [SerializeField] private float autoRotateSpeed = 8f;
    [SerializeField] private bool resumeAfterDelay = false;
    [SerializeField, Min(0f)] private float autoRotateResumeDelay = 15f;

    private float targetRotationInput;
    private float currentRotationInput;
    private float rotationInputVelocity;
    private float lastManualCameraInputTime;
    private bool autoRotationSuppressedByManualInput;
    private bool dialogAutoRotateActive;
    private bool dialogueJustEndedThisFrame;
    private bool hasScriptedHorizontalOrbit;
    private float scriptedHorizontalOrbitTarget;
    private float scriptedHorizontalOrbitSpeed;
    private bool horizontalRotationLocked;
    private float lockedHorizontalRotation;

    [Header("Dialogue Camera")]
    [SerializeField] private string dialogueZoomPresetName = "Narrow";
    [SerializeField, Min(0.1f)] private float dialogueZoomTransitionSpeed = 1f;
    [SerializeField, Min(0.01f)] private float dialogueRotationFadeInDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float dialogueRotationFadeOutDuration = 0.8f;

    [Header("Zoom")]
    [SerializeField] private float zoomSmoothSpeed = 8f;

    [SerializeField]
    private ZoomPreset[] zoomPresets =
    {
        new ZoomPreset("Top",    22f, 24f, 20f, 20f, 18f, 16f),
        new ZoomPreset("Wide",   18f, 20f, 16f, 16f, 14f, 12f),
        new ZoomPreset("Medium", 14f, 16f, 12f, 12f, 10f, 9f),
        new ZoomPreset("Narrow", 10f, 12f, 8f,  8f,  7f,  6f),
    };

    [SerializeField] private int startZoomIndex = 2;

    [Header("Manual Zoom Range")]
    [Tooltip("First zoom preset available through the mouse wheel.")]
    [SerializeField, Min(0)] private int manualZoomMinIndex = 0;
    [Tooltip("Last zoom preset available through the mouse wheel. Higher presets are reserved for scripted cameras.")]
    [SerializeField, Min(0)] private int manualZoomMaxIndex = 3;

    private int targetZoomIndex;
    private int previousZoomIndex = -1;
    private float activeZoomSmoothSpeed;
    private Transform overriddenFollowTarget;
    private Transform overriddenLookAtTarget;
    private bool hasTargetOverride;
    private Transform lookAtTargetBeforeOverride;
    private bool hasLookAtOverride;
    private int dialoguePreviousZoomIndex = -1;
    private float dialogueAutoRotationWeight;

    private void Awake()
    {
        cineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = cineCamera.GetComponentInChildren<CinemachineOrbitalFollow>();

        if (orbitalFollow == null)
        {
            Debug.LogError("CinemachineOrbitalFollow not found");
            enabled = false;
            return;
        }

        targetZoomIndex = Mathf.Clamp(startZoomIndex, 0, zoomPresets.Length - 1);
        activeZoomSmoothSpeed = zoomSmoothSpeed;
        ApplyZoomPresetImmediate(zoomPresets[targetZoomIndex]);
        lastManualCameraInputTime = Time.unscaledTime;
    }

    private void Update()
    {
        dialogueJustEndedThisFrame = false;
        UpdateDialogueAutoRotation();
        StopAutomaticRotationOnPlayerClick();
        SmoothZoomToPreset(zoomPresets[targetZoomIndex]);
        SmoothRotate();
        UpdateDebugZoomState();
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (!context.performed || orbitalFollow == null)
            return;

        if (MagnifierGlassController.IsScrollReservedForLoupe)
            return;

        float scrollDelta = context.ReadValue<Vector2>().y;

        if (Mathf.Abs(scrollDelta) < 0.01f)
            return;

        int direction = scrollDelta > 0 ? 1 : -1;

        GetManualZoomRange(out int minZoomIndex, out int maxZoomIndex);

        // Scripted presets (for example WallExam) must stay under script control.
        if (targetZoomIndex < minZoomIndex || targetZoomIndex > maxZoomIndex)
            return;

        targetZoomIndex = Mathf.Clamp(
            targetZoomIndex + direction,
            minZoomIndex,
            maxZoomIndex
        );

        UpdateDebugZoomState();

    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        if (orbitalFollow == null)
            return;

        Vector2 value = context.ReadValue<Vector2>();

        targetRotationInput = value.x;

        bool isClickHeld = Mouse.current != null &&
                           (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed);
        if (value.sqrMagnitude > 0.0001f && isClickHeld)
        {
            StopScriptedHorizontalOrbit();
            RegisterManualCameraInput();
        }
    }

    private void SmoothRotate()
    {
        if (horizontalRotationLocked)
        {
            orbitalFollow.HorizontalAxis.Value = lockedHorizontalRotation;
            return;
        }

        if (hasScriptedHorizontalOrbit)
        {
            float currentAxisValue = orbitalFollow.HorizontalAxis.Value;
            float nextAxisValue = Mathf.MoveTowardsAngle(
                currentAxisValue,
                scriptedHorizontalOrbitTarget,
                scriptedHorizontalOrbitSpeed * Time.deltaTime
            );

            orbitalFollow.HorizontalAxis.Value = nextAxisValue;

            return;
        }

        if (Mathf.Abs(targetRotationInput) > 0.01f)
        {
            currentRotationInput = targetRotationInput;
            rotationInputVelocity = 0f;
        }
        else
        {
            currentRotationInput = Mathf.SmoothDamp(
                currentRotationInput,
                0f,
                ref rotationInputVelocity,
                rotationDamping
            );
        }

        float manualRotation = currentRotationInput * rotationSpeed;
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            RegisterManualCameraInput();

        if (resumeAfterDelay && autoRotationSuppressedByManualInput &&
            Time.unscaledTime - lastManualCameraInputTime >= autoRotateResumeDelay)
        {
            autoRotateCamera = true;
            autoRotationSuppressedByManualInput = false;
        }

        float automaticRotation = autoRotateCamera ? autoRotateSpeed : 0f;
        automaticRotation += autoRotateSpeed * dialogueAutoRotationWeight;
        orbitalFollow.HorizontalAxis.Value += (manualRotation + automaticRotation) * Time.deltaTime;
    }

    public void OrbitHorizontalAxisTo(float horizontalAxisValue, float orbitSpeed)
    {
        if (orbitalFollow == null)
            return;

        scriptedHorizontalOrbitTarget = horizontalAxisValue;
        scriptedHorizontalOrbitSpeed = Mathf.Max(0.1f, orbitSpeed);
        hasScriptedHorizontalOrbit = true;
    }

    public void StopScriptedHorizontalOrbit()
    {
        hasScriptedHorizontalOrbit = false;
    }

    public bool IsHorizontalAxisAt(float horizontalAxisValue, float tolerance = 0.5f)
    {
        if (orbitalFollow == null)
            return true;

        return Mathf.Abs(Mathf.DeltaAngle(
            orbitalFollow.HorizontalAxis.Value,
            horizontalAxisValue)) <= Mathf.Max(0.01f, tolerance);
    }

    public void LockCurrentHorizontalRotation()
    {
        if (orbitalFollow == null)
            return;

        lockedHorizontalRotation = orbitalFollow.HorizontalAxis.Value;
        horizontalRotationLocked = true;
        autoRotateCamera = false;
        targetRotationInput = 0f;
        currentRotationInput = 0f;
        StopScriptedHorizontalOrbit();
    }

    public void UnlockHorizontalRotation()
    {
        horizontalRotationLocked = false;
        autoRotateCamera = false;
    }

    public void SetHorizontalRotation(float horizontalAxisValue)
    {
        if (orbitalFollow == null)
            return;

        orbitalFollow.HorizontalAxis.Value = horizontalAxisValue;
        autoRotateCamera = false;
        targetRotationInput = 0f;
        currentRotationInput = 0f;
        StopScriptedHorizontalOrbit();
    }

    private void RegisterManualCameraInput()
    {
        if (dialogAutoRotateActive)
            return;

        lastManualCameraInputTime = Time.unscaledTime;

        if (!autoRotateCamera && !autoRotationSuppressedByManualInput)
            return;

        autoRotateCamera = false;
        autoRotationSuppressedByManualInput = true;
    }

    private void StopAutomaticRotationOnPlayerClick()
    {
        if (dialogAutoRotateActive || dialogueJustEndedThisFrame || Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame && !Mouse.current.rightButton.wasPressedThisFrame)
            return;

        StopScriptedHorizontalOrbit();
        RegisterManualCameraInput();
    }

    private void UpdateDialogueAutoRotation()
    {
        bool isDialogueActive = ConversationManager.Instance != null &&
                                ConversationManager.Instance.IsConversationActive;

        if (isDialogueActive)
        {
            if (!dialogAutoRotateActive)
            {
                dialoguePreviousZoomIndex = targetZoomIndex;
                SetZoomPreset(dialogueZoomPresetName, dialogueZoomTransitionSpeed);
                autoRotateCamera = false;
            }

            dialogAutoRotateActive = true;
            autoRotationSuppressedByManualInput = false;
            dialogueAutoRotationWeight = Mathf.MoveTowards(
                dialogueAutoRotationWeight,
                1f,
                Time.unscaledDeltaTime / dialogueRotationFadeInDuration);
            return;
        }

        if (dialogAutoRotateActive)
        {
            dialogAutoRotateActive = false;
            dialogueJustEndedThisFrame = true;
            autoRotationSuppressedByManualInput = false;
            autoRotateCamera = false;

            if (dialoguePreviousZoomIndex >= 0)
                SetZoomIndex(dialoguePreviousZoomIndex, dialogueZoomTransitionSpeed);

            dialoguePreviousZoomIndex = -1;
        }

        dialogueAutoRotationWeight = Mathf.MoveTowards(
            dialogueAutoRotationWeight,
            0f,
            Time.unscaledDeltaTime / dialogueRotationFadeOutDuration);
    }

    private void SmoothZoomToPreset(ZoomPreset preset)
    {
        float t = 1f - Mathf.Exp(-activeZoomSmoothSpeed * Time.deltaTime);

        var top = orbitalFollow.Orbits.Top;
        var center = orbitalFollow.Orbits.Center;
        var bottom = orbitalFollow.Orbits.Bottom;

        top.Radius = Mathf.Lerp(top.Radius, preset.topRadius, t);
        top.Height = Mathf.Lerp(top.Height, preset.topHeight, t);

        center.Radius = Mathf.Lerp(center.Radius, preset.centerRadius, t);
        center.Height = Mathf.Lerp(center.Height, preset.centerHeight, t);

        bottom.Radius = Mathf.Lerp(bottom.Radius, preset.bottomRadius, t);
        bottom.Height = Mathf.Lerp(bottom.Height, preset.bottomHeight, t);

        orbitalFollow.Orbits.Top = top;
        orbitalFollow.Orbits.Center = center;
        orbitalFollow.Orbits.Bottom = bottom;
    }

    private void ApplyZoomPresetImmediate(ZoomPreset preset)
    {
        var top = orbitalFollow.Orbits.Top;
        var center = orbitalFollow.Orbits.Center;
        var bottom = orbitalFollow.Orbits.Bottom;

        top.Radius = preset.topRadius;
        top.Height = preset.topHeight;

        center.Radius = preset.centerRadius;
        center.Height = preset.centerHeight;

        bottom.Radius = preset.bottomRadius;
        bottom.Height = preset.bottomHeight;

        orbitalFollow.Orbits.Top = top;
        orbitalFollow.Orbits.Center = center;
        orbitalFollow.Orbits.Bottom = bottom;
    }

    [Serializable]
    private struct ZoomPreset
    {
        public string name;

        public float topRadius;
        public float topHeight;

        public float centerRadius;
        public float centerHeight;

        public float bottomRadius;
        public float bottomHeight;

        public ZoomPreset(
            string name,
            float topRadius,
            float topHeight,
            float centerRadius,
            float centerHeight,
            float bottomRadius,
            float bottomHeight)
        {
            this.name = name;

            this.topRadius = topRadius;
            this.topHeight = topHeight;

            this.centerRadius = centerRadius;
            this.centerHeight = centerHeight;

            this.bottomRadius = bottomRadius;
            this.bottomHeight = bottomHeight;
        }
    }

    public void SetZoomState(CameraZoomState state)
    {
        SetZoomIndex((int)state);
    }

    public bool SetZoomPreset(string presetName, float transitionSmoothSpeed = -1f)
    {
        if (zoomPresets == null || zoomPresets.Length == 0 || string.IsNullOrWhiteSpace(presetName))
            return false;

        for (int i = 0; i < zoomPresets.Length; i++)
        {
            if (string.Equals(zoomPresets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
            {
                SetZoomIndex(i, transitionSmoothSpeed);
                return true;
            }
        }

        Debug.LogWarning($"{name}: Zoom preset '{presetName}' was not found.");
        return false;
    }

    public void OverrideCameraTarget(Transform target)
    {
        if (cineCamera == null || target == null)
            return;

        if (!hasTargetOverride)
        {
            overriddenFollowTarget = cineCamera.Follow;
            overriddenLookAtTarget = cineCamera.LookAt;
            hasTargetOverride = true;
        }

        cineCamera.Follow = target;
        cineCamera.LookAt = target;
    }

    public void RestoreCameraTarget()
    {
        if (cineCamera == null || !hasTargetOverride)
            return;

        cineCamera.Follow = overriddenFollowTarget;
        cineCamera.LookAt = overriddenLookAtTarget;
        hasTargetOverride = false;
    }

    public Vector3 GetLookAtPosition()
    {
        if (cineCamera != null && cineCamera.LookAt != null)
            return cineCamera.LookAt.position;

        return transform.position;
    }

    public Vector3 GetFollowPosition()
    {
        if (cineCamera != null && cineCamera.Follow != null)
            return cineCamera.Follow.position;

        return transform.position;
    }

    public void OverrideLookAtTarget(Transform target)
    {
        if (cineCamera == null || target == null)
            return;

        if (!hasLookAtOverride)
        {
            lookAtTargetBeforeOverride = cineCamera.LookAt;
            hasLookAtOverride = true;
        }

        cineCamera.LookAt = target;
    }

    public void RestoreLookAtTarget()
    {
        if (cineCamera == null || !hasLookAtOverride)
            return;

        cineCamera.LookAt = lookAtTargetBeforeOverride;
        hasLookAtOverride = false;
    }

    public void SetZoomIndex(int index, float transitionSmoothSpeed = -1f)
    {
        if (zoomPresets == null || zoomPresets.Length == 0)
            return;

        activeZoomSmoothSpeed = transitionSmoothSpeed > 0f
            ? transitionSmoothSpeed
            : zoomSmoothSpeed;

        int clampedIndex = Mathf.Clamp(index, 0, zoomPresets.Length - 1);

        if (clampedIndex == targetZoomIndex)
            return;

        previousZoomIndex = targetZoomIndex;
        targetZoomIndex = clampedIndex;

        UpdateDebugZoomState();
    }
    public void ReturnToPreviousZoomState(float transitionSmoothSpeed = -1f)
    {
        if (previousZoomIndex < 0)
            return;

        int indexToReturn = previousZoomIndex;

        previousZoomIndex = targetZoomIndex;
        targetZoomIndex = indexToReturn;
        activeZoomSmoothSpeed = transitionSmoothSpeed > 0f
            ? transitionSmoothSpeed
            : zoomSmoothSpeed;

        UpdateDebugZoomState();
    }

    private void GetManualZoomRange(out int minZoomIndex, out int maxZoomIndex)
    {
        int lastPresetIndex = Mathf.Max(0, zoomPresets.Length - 1);
        minZoomIndex = Mathf.Clamp(manualZoomMinIndex, 0, lastPresetIndex);
        maxZoomIndex = Mathf.Clamp(manualZoomMaxIndex, minZoomIndex, lastPresetIndex);
    }


    private void UpdateDebugZoomState()
    {
        currentZoomIndex = targetZoomIndex;

        if (zoomPresets == null || zoomPresets.Length == 0)
        {
            currentZoomState = "No zoom presets";
            return;
        }

        currentZoomState = zoomPresets[targetZoomIndex].name;
    }


    //FIX
}

public enum CameraZoomState
{
    Top,
    Wide,
    Medium,
    Narrow
}
