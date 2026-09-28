using System;
using DialogueEditor;
using UnityEngine;

// Alias chroni przed 'using System.Diagnostics;' dopisywanym przez Visual Studio (CS0104).
using Debug = UnityEngine.Debug;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    public static bool IsManualZoomLocked { get; private set; }

    public static void SetManualZoomLocked(bool locked)
    {
        IsManualZoomLocked = locked;
    }

    [Header("Debug")]
    [SerializeField] private string currentZoomState;
    [SerializeField] private int currentZoomIndex;
    [Tooltip("In Play Mode, applies the currently selected preset every frame so its Radius and Height values can be tuned live.")]
    [SerializeField] private bool livePresetPreview;
    public int CurrentZoomIndex => targetZoomIndex;
    public int PresetScaleIndex => targetZoomIndex;
    public bool IsCurrentGameplayCamera => IsActiveGameplayCamera();

    private CinemachineCamera cineCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachineRotationComposer rotationComposer;

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
    private bool tutorialCameraInputBlocked;
    private float defaultRotationSpeed;
    private float lastManualCameraInputTime;
    private bool autoRotationSuppressedByManualInput;
    private bool dialogAutoRotateActive;
    private bool dialogAutoRotationSuppressedByManualInput;
    private bool dialogueJustEndedThisFrame;
    private bool hasScriptedHorizontalOrbit;
    private float scriptedHorizontalOrbitTarget;
    private float scriptedHorizontalOrbitSpeed;
    private bool horizontalRotationLocked;
    private float lockedHorizontalRotation;

    [Header("Dialogue Camera")]
    [SerializeField] private string dialogueZoomPresetName = "Narrow";
    [SerializeField, Min(0f)] private float dialogueZoomReturnDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float dialogueLookAtReturnSpeed = 1f;

    [Header("Zoom")]
    [Tooltip("Camera travel speed in world units per second for non-dialogue preset transitions.")]
    [InspectorName("Zoom Transition Speed")]
    [SerializeField, Min(0.01f)] private float zoomTransitionWorldSpeed = 10f;
    [Tooltip("Acceleration and deceleration time when Use Zoom Transition Curve is disabled.")]
    [SerializeField, Min(0f)] private float zoomTransitionRampTime = 0.2f;
    [Tooltip("Use the curve instead of the speed ramp for non-dialogue preset transitions.")]
    [SerializeField] private bool useZoomTransitionCurve;
    [InspectorName("Zoom Transition Curve")]
    [SerializeField] private AnimationCurve zoomTransitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Depth Of Field")]
    [Tooltip("Dedicated global Volume containing only the Depth Of Field override.")]
    [SerializeField] private Volume depthOfFieldVolume;
    [SerializeField, Min(0f)] private float depthOfFieldFadeDuration = 0.3f;
    [SerializeField] private AnimationCurve depthOfFieldFadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField]
    private ZoomPreset[] zoomPresets =
    {
        new ZoomPreset("Top",    22f, 24f, 20f, 20f, 18f, 16f),
        new ZoomPreset("Wide",   18f, 20f, 16f, 16f, 14f, 12f),
        new ZoomPreset("Medium", 14f, 16f, 12f, 12f, 10f, 9f),
        new ZoomPreset("Narrow", 10f, 12f, 8f,  8f,  7f,  6f),
    };

    [SerializeField] private int startZoomIndex = 2;

    private bool hasMirrorPreset;
    private bool mirrorZoneActive;

    [Header("Manual Zoom Range")]
    [Tooltip("First zoom preset available through the mouse wheel outside the mirror zone.")]
    [SerializeField, Min(0)] private int manualZoomMinIndex = 1;
    [Tooltip("Last zoom preset available through the mouse wheel.")]
    [SerializeField, Min(0)] private int manualZoomMaxIndex = 5;

    private int targetZoomIndex;
    private int previousZoomIndex = -1;
    private bool zoomTransitionActive;
    private float zoomTransitionElapsed;
    private float activeZoomTransitionDuration;
    private float zoomTransitionDistance;
    private float zoomTransitionRampDuration;
    private float zoomTransitionCruiseDuration;
    private float zoomTransitionPeakSpeed;
    private bool zoomTransitionUsesCurve;
    private Vector2 rotationComposerDampingBeforeZoom;
    private bool zoomRotationDampingOverridden;
    private bool zoomRotationDampingRestorePending;
    private int zoomTransitionTargetIndex = -1;
    private ZoomPreset zoomTransitionStart;
    private DepthOfField depthOfField;
    private bool depthOfFieldTransitionActive;
    private float depthOfFieldTransitionElapsed;
    private float depthOfFieldTransitionStartStrength;
    private float depthOfFieldTargetStrength;
    private float depthOfFieldConfiguredStart;
    private float depthOfFieldConfiguredEnd;
    private float depthOfFieldConfiguredMaxRadius;
    private float depthOfFieldProfileStart;
    private float depthOfFieldProfileEnd;
    private float depthOfFieldProfileMaxRadius;
    private bool depthOfFieldSettingsTransitionActive;
    private float depthOfFieldSettingsTransitionElapsed;
    private float depthOfFieldSettingsStartStart;
    private float depthOfFieldSettingsStartEnd;
    private float depthOfFieldSettingsStartMaxRadius;
    private float depthOfFieldSettingsTargetStart;
    private float depthOfFieldSettingsTargetEnd;
    private float depthOfFieldSettingsTargetMaxRadius;
    private bool depthOfFieldTargetInitialized;
    private bool depthOfFieldWarningLogged;
    private bool wasActiveGameplayCameraForDepthOfField;
    private Transform overriddenFollowTarget;
    private Transform overriddenLookAtTarget;
    private bool hasTargetOverride;
    private Transform lookAtTargetBeforeOverride;
    private bool hasLookAtOverride;
    private Transform smoothLookAtProxy;
    private Transform smoothLookAtDestination;
    private Vector3 smoothLookAtStartPosition;
    private Vector3 smoothLookAtDestinationPosition;
    private float smoothLookAtTransitionDuration;
    private float smoothLookAtTransitionElapsed;
    private bool smoothLookAtUsesAnchoredDestination;
    private float smoothLookAtAnchorSnapDistance;
    private bool restoringSmoothLookAtTarget;
    private int dialoguePreviousZoomIndex = -1;
    private bool dialogueZoomWasOverridden;
    private bool dialogueZoomPreRollActive;
    private float dialogueZoomPreRollElapsed;
    private float dialogueZoomPreRollDuration;
    private int dialogueZoomPreRollTargetIndex = -1;
    private ZoomPreset dialogueZoomPreRollStart;
    private Transform dialogueLookAtProxy;
    private Vector3 dialogueLookAtStartPosition;
    private Vector3 dialogueLookAtTargetPosition;
    private float dialogueLookAtPreRollElapsed;
    private float dialogueLookAtPreRollDuration;
    private bool dialogueLookAtPreRollActive;
    private bool dialogueLookAtManagedExternally;
    private bool dialogueLookAtGenericAnchor;
    private bool hasLoupeOrbitFollowOverride;
    private float loupeOrbitGameplayStartHorizontalAxis;
    private float loupeOrbitLockedVerticalAxis;
    private float loupeOrbitAppliedYaw;
    private Vector3 loupeOrbitPivotPosition;
    private Vector3 loupeOrbitMainCameraOffset;
    private Quaternion loupeOrbitMainCameraRotation;
    private bool loupeOrbitOrbitalFollowWasEnabled;
    private bool loupeOrbitRotationComposerWasEnabled;

    private void Awake()
    {
        cineCamera = GetComponent<CinemachineCamera>();
        orbitalFollow = cineCamera.GetComponentInChildren<CinemachineOrbitalFollow>();
        rotationComposer = cineCamera.GetComponentInChildren<CinemachineRotationComposer>();
        defaultRotationSpeed = rotationSpeed;

        if (orbitalFollow == null)
        {
            Debug.LogError("CinemachineOrbitalFollow not found");
            enabled = false;
            return;
        }

        hasMirrorPreset = zoomPresets != null && zoomPresets.Length > 1 &&
            string.Equals(zoomPresets[0].name, "Mirror", StringComparison.OrdinalIgnoreCase);

        targetZoomIndex = Mathf.Clamp(startZoomIndex, 0, zoomPresets.Length - 1);
        ApplyZoomPresetImmediate(zoomPresets[targetZoomIndex]);
        ResolveDepthOfField();
        lastManualCameraInputTime = Time.unscaledTime;
    }

    private void OnEnable()
    {
        ConversationManager.OnConversationStarted += HandleConversationStarted;
        ConversationManager.OnConversationUIWillShow += HandleConversationUIWillShow;
        ConversationManager.OnConversationUIHidden += HandleConversationUIHidden;
    }

    private void OnDisable()
    {
        RestoreZoomRotationDamping();
        ConversationManager.OnConversationStarted -= HandleConversationStarted;
        ConversationManager.OnConversationUIWillShow -= HandleConversationUIWillShow;
        ConversationManager.OnConversationUIHidden -= HandleConversationUIHidden;
    }

    private void Update()
    {
        if (zoomRotationDampingRestorePending)
            RestoreZoomRotationDamping();

        if (IsTutorialBlockingCamera())
        {
            if (!tutorialCameraInputBlocked)
            {
                tutorialCameraInputBlocked = true;
                targetRotationInput = 0f;
                currentRotationInput = 0f;
                rotationInputVelocity = 0f;
            }

            UpdateDebugZoomState();
            return;
        }

        tutorialCameraInputBlocked = false;
        dialogueJustEndedThisFrame = false;
        UpdateDialogueLookAtPreRoll();
        UpdateSmoothLookAtTarget();
        UpdateDialogueAutoRotation();
        StopAutomaticRotationOnPlayerClick();
        if (!hasLoupeOrbitFollowOverride)
        {
            if (dialogueZoomPreRollActive)
                UpdateDialogueZoomPreRoll();
            else
                UpdateZoomTransition();

            if (livePresetPreview && zoomPresets != null && zoomPresets.Length > 0)
            {
                int previewIndex = Mathf.Clamp(targetZoomIndex, 0, zoomPresets.Length - 1);
                ApplyZoomPresetImmediate(zoomPresets[previewIndex]);
            }
        }
        SmoothRotate();
        UpdateDepthOfFieldForActiveCamera();
        UpdateDebugZoomState();
    }

    public void OnCameraZoom(InputAction.CallbackContext context)
    {
        if (!context.performed || orbitalFollow == null)
            return;

        if (IsTutorialBlockingCamera())
            return;

        if (IsManualZoomLocked)
            return;

        if (MagnifierGlassController.IsScrollReservedForLoupe)
            return;

        if (SafeCodeDrumMinigame.IsPointerOverActiveBoard)
            return;

        float scrollDelta = context.ReadValue<Vector2>().y;

        if (Mathf.Abs(scrollDelta) < 0.01f)
            return;

        int direction = scrollDelta > 0 ? -1 : 1;

        GetManualZoomRange(out int minZoomIndex, out int maxZoomIndex);

        bool dialogueAllowsManualZoom = dialogAutoRotateActive;
        if (dialogueAllowsManualZoom)
            dialogueZoomPreRollActive = false;

        if (!dialogueAllowsManualZoom &&
            (targetZoomIndex < minZoomIndex || targetZoomIndex > maxZoomIndex))
            return;

        int manualStartIndex = Mathf.Clamp(targetZoomIndex, minZoomIndex, maxZoomIndex);

        int requestedZoomIndex = Mathf.Clamp(
            manualStartIndex + direction,
            minZoomIndex,
            maxZoomIndex
        );
        SetZoomIndex(requestedZoomIndex);

    }

    public void OnCameraRotate(InputAction.CallbackContext context)
    {
        if (orbitalFollow == null)
            return;

        if (IsTutorialBlockingCamera())
        {
            targetRotationInput = 0f;
            currentRotationInput = 0f;
            rotationInputVelocity = 0f;
            return;
        }

        Vector2 value = context.ReadValue<Vector2>();

        if (IsActiveGameplayCamera() &&
            MagnifierGlassController.TryConsumeCameraRotation(value, rotationSpeed))
        {
            targetRotationInput = 0f;
            currentRotationInput = 0f;
            rotationInputVelocity = 0f;

            if (value.sqrMagnitude > 0.0001f)
            {
                StopScriptedHorizontalOrbit();
                RegisterManualCameraInput();
            }

            return;
        }

        targetRotationInput = value.x;

        bool isClickHeld = Mouse.current != null &&
                           (Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed);
        if (value.sqrMagnitude > 0.0001f && isClickHeld)
        {
            StopScriptedHorizontalOrbit();
            RegisterManualCameraInput();
        }
    }

    public void SetMouseSensitivityMultiplier(float multiplier)
    {
        rotationSpeed = defaultRotationSpeed * Mathf.Max(0.01f, multiplier);
    }

    private void SmoothRotate()
    {
        if (IsActiveGameplayCamera() &&
            MagnifierGlassController.IsRotationReservedForLoupe)
        {
            targetRotationInput = 0f;
            currentRotationInput = 0f;
            rotationInputVelocity = 0f;
            return;
        }

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
        {
            // The player has taken over the framing. Keep dialogue zoom, but never
            // re-enable the dialogue orbit until this conversation ends.
            dialogAutoRotationSuppressedByManualInput = true;
            return;
        }

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
                NPCConversation conversation = ConversationManager.Instance.ActiveConversationSource;
                TryBeginDialogueCamera(conversation);
                if (!dialogAutoRotateActive)
                    return;
            }

            dialogAutoRotateActive = true;
            autoRotationSuppressedByManualInput = false;
            // Dialogues may change the zoom preset, but keep the current horizontal framing.
            return;
        }

        if (dialogAutoRotateActive)
        {
            dialogAutoRotateActive = false;
            dialogAutoRotationSuppressedByManualInput = false;
            dialogueJustEndedThisFrame = true;
            autoRotationSuppressedByManualInput = false;
            autoRotateCamera = false;

            if (dialogueZoomWasOverridden && dialoguePreviousZoomIndex >= 0)
                SetZoomIndexInternal(dialoguePreviousZoomIndex, dialogueZoomReturnDuration, true);

            dialoguePreviousZoomIndex = -1;
            dialogueZoomWasOverridden = false;
        }

    }

    private void HandleConversationStarted()
    {
        NPCConversation conversation = ConversationManager.Instance != null
            ? ConversationManager.Instance.ActiveConversationSource
            : null;
        TryBeginDialogueCamera(conversation);
    }

    private void TryBeginDialogueCamera(NPCConversation conversation)
    {
        if (dialogAutoRotateActive || !IsActiveGameplayCamera())
            return;

        bool useAutomaticDialogueCamera = conversation == null || conversation.UseAutomaticDialogueCamera;
        dialogueZoomWasOverridden = useAutomaticDialogueCamera;
        if (useAutomaticDialogueCamera)
        {
            dialoguePreviousZoomIndex = hasMirrorPreset ? Mathf.Max(1, targetZoomIndex) : targetZoomIndex;
            float preRollDuration = conversation != null
                ? conversation.AutomaticDialogueCameraPreRollTime
                : 0.35f;

            if (conversation != null && !conversation.SetCustomPreset)
            {
                StartDialogueZoomPreRoll(Mathf.Max(hasMirrorPreset ? 1 : 0, targetZoomIndex - 1), preRollDuration);
            }
            else
            {
                string presetName = conversation != null && !string.IsNullOrWhiteSpace(conversation.AutomaticDialogueCameraPreset)
                    ? conversation.AutomaticDialogueCameraPreset
                    : dialogueZoomPresetName;
                StartDialogueZoomPreRoll(presetName, preRollDuration);
            }
        }

        autoRotateCamera = false;
        StopScriptedHorizontalOrbit();
        dialogAutoRotationSuppressedByManualInput = false;
        dialogAutoRotateActive = true;
    }

    private bool IsActiveGameplayCamera()
    {
        SwitchCharacter switchCharacter = SwitchCharacter.Instance;
        if (switchCharacter != null && switchCharacter.playersCamera != null)
        {
            int activeIndex = switchCharacter.activePlayerIndex;
            if (activeIndex >= 0 && activeIndex < switchCharacter.playersCamera.Length)
                return switchCharacter.playersCamera[activeIndex] == cineCamera;
        }

        return cineCamera != null && cineCamera.isActiveAndEnabled;
    }

    private void StartDialogueZoomPreRoll(string presetName, float duration)
    {
        if (!TryGetZoomPresetIndex(presetName, out int presetIndex))
            return;

        StartDialogueZoomPreRoll(presetIndex, duration);
    }

    private void StartDialogueZoomPreRoll(int presetIndex, float duration)
    {
        if (zoomPresets == null || zoomPresets.Length == 0)
            return;

        presetIndex = Mathf.Clamp(presetIndex, hasMirrorPreset ? 1 : 0, zoomPresets.Length - 1);

        dialogueZoomPreRollStart = CaptureCurrentZoomPreset();
        zoomTransitionActive = false;
        zoomTransitionTargetIndex = -1;
        RestoreZoomRotationDamping();
        dialogueZoomPreRollTargetIndex = presetIndex;
        targetZoomIndex = presetIndex;
        dialogueZoomPreRollElapsed = 0f;
        dialogueZoomPreRollDuration = Mathf.Max(0f, duration);
        dialogueZoomPreRollActive = dialogueZoomPreRollDuration > 0f;

        if (!dialogueZoomPreRollActive)
            CompleteDialogueZoomPreRoll();

        UpdateDebugZoomState();
    }

    private void UpdateDialogueZoomPreRoll()
    {
        if (!dialogueZoomPreRollActive || dialogueZoomPreRollTargetIndex < 0)
            return;

        dialogueZoomPreRollElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(dialogueZoomPreRollElapsed / dialogueZoomPreRollDuration);
        ApplyZoomPresetInterpolated(
            dialogueZoomPreRollStart,
            zoomPresets[dialogueZoomPreRollTargetIndex],
            Mathf.SmoothStep(0f, 1f, normalizedTime));

        if (normalizedTime >= 1f)
            CompleteDialogueZoomPreRoll();
    }

    private void CompleteDialogueZoomPreRoll()
    {
        if (dialogueZoomPreRollTargetIndex >= 0 && dialogueZoomPreRollTargetIndex < zoomPresets.Length)
            ApplyZoomPresetImmediate(zoomPresets[dialogueZoomPreRollTargetIndex]);

        dialogueZoomPreRollActive = false;
        dialogueZoomPreRollTargetIndex = -1;
    }

    private void StartZoomTransition(int presetIndex, float duration, bool useDialogueDuration = false)
    {
        zoomTransitionStart = CaptureCurrentZoomPreset();
        zoomTransitionTargetIndex = presetIndex;
        zoomTransitionElapsed = 0f;
        zoomTransitionUsesCurve = useDialogueDuration || useZoomTransitionCurve;
        if (useDialogueDuration)
        {
            activeZoomTransitionDuration = Mathf.Max(0f, duration);
        }
        else
        {
            ConfigureConstantSpeedZoomTransition(zoomTransitionStart, zoomPresets[presetIndex]);
        }
        zoomTransitionActive = activeZoomTransitionDuration > 0f;
        if (zoomTransitionActive && !useDialogueDuration)
            SuppressZoomRotationDamping();
        else
            RestoreZoomRotationDamping();
        dialogueZoomPreRollActive = false;
        dialogueZoomPreRollTargetIndex = -1;

        if (!zoomTransitionActive)
            CompleteZoomTransition();
    }

    private void ConfigureConstantSpeedZoomTransition(ZoomPreset from, ZoomPreset to)
    {
        float verticalPosition = orbitalFollow.VerticalAxis.GetNormalizedValue();
        Vector2 startOrbitPoint = GetZoomOrbitPoint(from, verticalPosition);
        Vector2 targetOrbitPoint = GetZoomOrbitPoint(to, verticalPosition);
        zoomTransitionDistance = Vector2.Distance(startOrbitPoint, targetOrbitPoint) *
                                 Mathf.Abs(orbitalFollow.RadialAxis.Value);
        if (zoomTransitionDistance <= 0.0001f)
        {
            activeZoomTransitionDuration = 0f;
            return;
        }

        float speed = Mathf.Max(0.01f, zoomTransitionWorldSpeed);
        if (zoomTransitionUsesCurve)
        {
            activeZoomTransitionDuration = zoomTransitionDistance / speed;
            return;
        }

        float rampTime = Mathf.Max(0f, zoomTransitionRampTime);
        if (rampTime <= 0f)
        {
            zoomTransitionRampDuration = 0f;
            zoomTransitionCruiseDuration = zoomTransitionDistance / speed;
            zoomTransitionPeakSpeed = speed;
        }
        else
        {
            zoomTransitionRampDuration = Mathf.Min(rampTime,
                Mathf.Sqrt(zoomTransitionDistance * rampTime / speed));
            zoomTransitionPeakSpeed = speed * zoomTransitionRampDuration / rampTime;
            zoomTransitionCruiseDuration = Mathf.Max(0f,
                (zoomTransitionDistance - zoomTransitionPeakSpeed * zoomTransitionRampDuration) /
                zoomTransitionPeakSpeed);
        }

        activeZoomTransitionDuration = 2f * zoomTransitionRampDuration + zoomTransitionCruiseDuration;
    }

    private Vector2 GetZoomOrbitPoint(ZoomPreset preset, float verticalPosition)
    {
        Vector2[] knots = new Vector2[5];
        knots[1] = new Vector2(-preset.bottomRadius, preset.bottomHeight);
        knots[2] = new Vector2(-preset.centerRadius, preset.centerHeight);
        knots[3] = new Vector2(-preset.topRadius, preset.topHeight);

        float curvature = orbitalFollow.Orbits.SplineCurvature;
        knots[0] = Vector2.Lerp(knots[1] + (knots[1] - knots[2]) * 0.5f, Vector2.zero, curvature);
        knots[4] = Vector2.Lerp(knots[3] + (knots[3] - knots[2]) * 0.5f, Vector2.zero, curvature);

        const int segmentCount = 4;
        float[] lower = new float[segmentCount];
        float[] diagonal = new float[segmentCount];
        float[] upper = new float[segmentCount];
        Vector2[] right = new Vector2[segmentCount];
        Vector2[] firstControl = new Vector2[segmentCount];

        diagonal[0] = 2f;
        upper[0] = 1f;
        right[0] = knots[0] + 2f * knots[1];

        for (int i = 1; i < segmentCount - 1; i++)
        {
            lower[i] = 1f;
            diagonal[i] = 4f;
            upper[i] = 1f;
            right[i] = 4f * knots[i] + 2f * knots[i + 1];
        }

        lower[segmentCount - 1] = 2f;
        diagonal[segmentCount - 1] = 7f;
        right[segmentCount - 1] = 8f * knots[segmentCount - 1] + knots[segmentCount];

        for (int i = 1; i < segmentCount; i++)
        {
            float factor = lower[i] / diagonal[i - 1];
            diagonal[i] -= factor * upper[i - 1];
            right[i] -= factor * right[i - 1];
        }

        firstControl[segmentCount - 1] = right[segmentCount - 1] / diagonal[segmentCount - 1];
        for (int i = segmentCount - 2; i >= 0; i--)
            firstControl[i] = (right[i] - upper[i] * firstControl[i + 1]) / diagonal[i];

        int segment = verticalPosition > 0.5f ? 2 : 1;
        float t = verticalPosition > 0.5f ? (verticalPosition - 0.5f) * 2f : verticalPosition * 2f;
        Vector2 secondControl = 2f * knots[segment + 1] - firstControl[segment + 1];
        float inverseT = 1f - t;
        return inverseT * inverseT * inverseT * knots[segment]
            + 3f * inverseT * inverseT * t * firstControl[segment]
            + 3f * inverseT * t * t * secondControl
            + t * t * t * knots[segment + 1];
    }

    private static bool IsTutorialBlockingCamera()
    {
        return (TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput) ||
               (TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput) ||
               (NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen) ||
               Mathf.Approximately(Time.timeScale, 0f);
    }

    private void UpdateZoomTransition()
    {
        if (!zoomTransitionActive || zoomTransitionTargetIndex < 0 ||
            zoomTransitionTargetIndex >= zoomPresets.Length)
            return;

        zoomTransitionElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(zoomTransitionElapsed / activeZoomTransitionDuration);
        float easedTime = zoomTransitionUsesCurve
            ? EvaluateNormalizedZoomTransitionCurve(normalizedTime)
            : EvaluateConstantSpeedZoomProgress();
        ApplyZoomPresetInterpolated(
            zoomTransitionStart,
            zoomPresets[zoomTransitionTargetIndex],
            easedTime);

        if (normalizedTime >= 1f)
            CompleteZoomTransition();
    }

    private float EvaluateConstantSpeedZoomProgress()
    {
        if (zoomTransitionDistance <= 0f)
            return 1f;

        float elapsed = Mathf.Min(zoomTransitionElapsed, activeZoomTransitionDuration);
        float distanceTravelled;
        if (zoomTransitionRampDuration > 0f && elapsed < zoomTransitionRampDuration)
        {
            distanceTravelled = 0.5f * zoomTransitionPeakSpeed * elapsed * elapsed /
                                zoomTransitionRampDuration;
        }
        else if (elapsed < zoomTransitionRampDuration + zoomTransitionCruiseDuration)
        {
            distanceTravelled = 0.5f * zoomTransitionPeakSpeed * zoomTransitionRampDuration +
                                zoomTransitionPeakSpeed * (elapsed - zoomTransitionRampDuration);
        }
        else
        {
            float remaining = activeZoomTransitionDuration - elapsed;
            distanceTravelled = zoomTransitionDistance -
                                0.5f * zoomTransitionPeakSpeed * remaining * remaining /
                                Mathf.Max(0.0001f, zoomTransitionRampDuration);
        }

        return Mathf.Clamp01(distanceTravelled / zoomTransitionDistance);
    }

    private float EvaluateNormalizedZoomTransitionCurve(float normalizedTime)
    {
        if (zoomTransitionCurve == null || zoomTransitionCurve.length == 0)
            return Mathf.SmoothStep(0f, 1f, normalizedTime);

        float curveStart = zoomTransitionCurve.Evaluate(0f);
        float curveEnd = zoomTransitionCurve.Evaluate(1f);
        if (Mathf.Approximately(curveStart, curveEnd))
            return Mathf.SmoothStep(0f, 1f, normalizedTime);

        return Mathf.Clamp01(Mathf.InverseLerp(
            curveStart,
            curveEnd,
            zoomTransitionCurve.Evaluate(normalizedTime)));
    }

    private void CompleteZoomTransition()
    {
        if (zoomTransitionTargetIndex >= 0 && zoomTransitionTargetIndex < zoomPresets.Length)
            ApplyZoomPresetImmediate(zoomPresets[zoomTransitionTargetIndex]);

        zoomRotationDampingRestorePending = zoomRotationDampingOverridden;
        zoomTransitionActive = false;
        zoomTransitionTargetIndex = -1;
    }

    private void SuppressZoomRotationDamping()
    {
        if (rotationComposer == null)
            return;

        if (!zoomRotationDampingOverridden)
        {
            rotationComposerDampingBeforeZoom = rotationComposer.Damping;
            zoomRotationDampingOverridden = true;
        }

        zoomRotationDampingRestorePending = false;
        rotationComposer.Damping = Vector2.zero;
    }

    private void RestoreZoomRotationDamping()
    {
        if (zoomRotationDampingOverridden && rotationComposer != null)
            rotationComposer.Damping = rotationComposerDampingBeforeZoom;

        zoomRotationDampingOverridden = false;
        zoomRotationDampingRestorePending = false;
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

    private ZoomPreset CaptureCurrentZoomPreset()
    {
        return new ZoomPreset(
            "Dialogue Pre-Roll Start",
            orbitalFollow.Orbits.Top.Radius,
            orbitalFollow.Orbits.Top.Height,
            orbitalFollow.Orbits.Center.Radius,
            orbitalFollow.Orbits.Center.Height,
            orbitalFollow.Orbits.Bottom.Radius,
            orbitalFollow.Orbits.Bottom.Height);
    }

    private void ApplyZoomPresetInterpolated(ZoomPreset from, ZoomPreset to, float t)
    {
        ApplyZoomPresetImmediate(new ZoomPreset(
            to.name,
            Mathf.Lerp(from.topRadius, to.topRadius, t),
            Mathf.Lerp(from.topHeight, to.topHeight, t),
            Mathf.Lerp(from.centerRadius, to.centerRadius, t),
            Mathf.Lerp(from.centerHeight, to.centerHeight, t),
            Mathf.Lerp(from.bottomRadius, to.bottomRadius, t),
            Mathf.Lerp(from.bottomHeight, to.bottomHeight, t)));
    }

    private void ResolveDepthOfField()
    {
        if (TryGetDepthOfField(depthOfFieldVolume, out depthOfField))
        {
            depthOfFieldProfileStart = depthOfField.gaussianStart.value;
            depthOfFieldProfileEnd = Mathf.Max(
                depthOfFieldProfileStart + 0.01f,
                depthOfField.gaussianEnd.value);
            depthOfFieldProfileMaxRadius = depthOfField.gaussianMaxRadius.value;
            depthOfFieldConfiguredStart = depthOfFieldProfileStart;
            depthOfFieldConfiguredEnd = depthOfFieldProfileEnd;
            depthOfFieldConfiguredMaxRadius = depthOfFieldProfileMaxRadius;
            return;
        }

        depthOfField = null;
        if (!depthOfFieldWarningLogged)
        {
            Debug.LogWarning(
                $"{name}: Assign a dedicated global Volume with a Depth Of Field override.",
                this);
            depthOfFieldWarningLogged = true;
        }
    }

    private static bool TryGetDepthOfField(Volume volume, out DepthOfField result)
    {
        result = null;
        return volume != null && volume.profile != null && volume.profile.TryGet(out result);
    }

    private void UpdateDepthOfFieldForActiveCamera()
    {
        bool isActiveGameplayCamera = IsActiveGameplayCamera();
        if (!isActiveGameplayCamera)
        {
            wasActiveGameplayCameraForDepthOfField = false;
            return;
        }

        if (!wasActiveGameplayCameraForDepthOfField)
        {
            depthOfFieldTransitionActive = false;
            depthOfFieldSettingsTransitionActive = false;
            depthOfFieldTargetInitialized = false;
            wasActiveGameplayCameraForDepthOfField = true;
        }

        if (depthOfField == null)
            ResolveDepthOfField();

        if (depthOfField == null || zoomPresets == null || zoomPresets.Length == 0)
            return;

        int presetIndex = Mathf.Clamp(targetZoomIndex, 0, zoomPresets.Length - 1);
        ZoomPreset selectedPreset = zoomPresets[presetIndex];
        bool cameraHasReachedPreset = !zoomTransitionActive && !dialogueZoomPreRollActive;
        bool presetWantsDepthOfField = selectedPreset.enableDepthOfField;
        float currentStrength = GetCurrentDepthOfFieldStrength();

        if (presetWantsDepthOfField && currentStrength > 0.0001f)
        {
            BeginDepthOfFieldSettingsTransitionIfNeeded(selectedPreset);
            UpdateDepthOfFieldSettingsTransition(currentStrength);
        }
        else if (presetWantsDepthOfField && cameraHasReachedPreset &&
                 !DepthOfFieldSettingsMatch(selectedPreset))
        {
            ApplyDepthOfFieldPresetSettings(selectedPreset);
        }
        else if (!presetWantsDepthOfField)
        {
            depthOfFieldSettingsTransitionActive = false;
        }

        bool canShowDepthOfField = presetWantsDepthOfField &&
                                   (currentStrength > 0.0001f || cameraHasReachedPreset);
        float requestedStrength = canShowDepthOfField ? 1f : 0f;

        depthOfField.active = true;
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(depthOfFieldConfiguredStart);
        depthOfField.gaussianEnd.overrideState = true;
        depthOfField.gaussianMaxRadius.Override(depthOfFieldConfiguredMaxRadius);

        if (!depthOfFieldTargetInitialized && depthOfFieldVolume.weight <= 0.0001f)
            ApplyDepthOfFieldStrength(0f);

        depthOfFieldVolume.weight = 1f;

        bool targetChanged = !depthOfFieldTargetInitialized ||
                             !Mathf.Approximately(depthOfFieldTargetStrength, requestedStrength);
        bool strengthWasChangedExternally = !depthOfFieldTransitionActive &&
                                            !Mathf.Approximately(
                                                currentStrength,
                                                requestedStrength);
        if (targetChanged || strengthWasChangedExternally)
            BeginDepthOfFieldTransition(requestedStrength);

        UpdateDepthOfFieldTransition();
    }

    private void BeginDepthOfFieldTransition(float targetStrength)
    {
        depthOfFieldTargetInitialized = true;
        depthOfFieldTargetStrength = Mathf.Clamp01(targetStrength);
        depthOfFieldTransitionStartStrength = GetCurrentDepthOfFieldStrength();
        depthOfFieldTransitionElapsed = 0f;
        depthOfFieldTransitionActive = depthOfFieldFadeDuration > 0f &&
                                       !Mathf.Approximately(
                                           depthOfFieldTransitionStartStrength,
                                           depthOfFieldTargetStrength);

        if (!depthOfFieldTransitionActive && depthOfField != null)
            ApplyDepthOfFieldStrength(depthOfFieldTargetStrength);
    }

    private void UpdateDepthOfFieldTransition()
    {
        if (!depthOfFieldTransitionActive || depthOfField == null)
            return;

        depthOfFieldTransitionElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(
            depthOfFieldTransitionElapsed / depthOfFieldFadeDuration);
        float easedTime = EvaluateNormalizedCurve(depthOfFieldFadeCurve, normalizedTime);
        float strength = Mathf.Lerp(
            depthOfFieldTransitionStartStrength,
            depthOfFieldTargetStrength,
            easedTime);
        ApplyDepthOfFieldStrength(strength);

        if (normalizedTime < 1f)
            return;

        ApplyDepthOfFieldStrength(depthOfFieldTargetStrength);
        depthOfFieldTransitionActive = false;
    }

    private float GetCurrentDepthOfFieldStrength()
    {
        if (depthOfField == null)
            return 0f;

        float configuredRange = Mathf.Max(
            0.01f,
            depthOfFieldConfiguredEnd - depthOfFieldConfiguredStart);
        float currentRange = Mathf.Max(
            configuredRange,
            depthOfField.gaussianEnd.value - depthOfFieldConfiguredStart);
        if (currentRange >= configuredRange * 9999f)
            return 0f;

        return Mathf.Clamp01(configuredRange / currentRange);
    }

    private void ApplyDepthOfFieldStrength(float strength)
    {
        if (depthOfField == null)
            return;

        float configuredRange = Mathf.Max(
            0.01f,
            depthOfFieldConfiguredEnd - depthOfFieldConfiguredStart);
        float safeStrength = Mathf.Max(0.0001f, Mathf.Clamp01(strength));
        depthOfField.gaussianEnd.value = depthOfFieldConfiguredStart +
                                         configuredRange / safeStrength;
    }

    private bool DepthOfFieldSettingsMatch(ZoomPreset preset)
    {
        GetDepthOfFieldPresetSettings(
            preset,
            out float start,
            out float end,
            out float maxRadius);
        return Mathf.Approximately(depthOfFieldConfiguredStart, start) &&
               Mathf.Approximately(depthOfFieldConfiguredEnd, end) &&
               Mathf.Approximately(depthOfFieldConfiguredMaxRadius, maxRadius);
    }

    private void ApplyDepthOfFieldPresetSettings(ZoomPreset preset)
    {
        GetDepthOfFieldPresetSettings(
            preset,
            out depthOfFieldConfiguredStart,
            out depthOfFieldConfiguredEnd,
            out depthOfFieldConfiguredMaxRadius);

        depthOfField.gaussianStart.Override(depthOfFieldConfiguredStart);
        depthOfField.gaussianMaxRadius.Override(depthOfFieldConfiguredMaxRadius);
        ApplyDepthOfFieldStrength(0f);
    }

    private void BeginDepthOfFieldSettingsTransitionIfNeeded(ZoomPreset preset)
    {
        GetDepthOfFieldPresetSettings(
            preset,
            out float targetStart,
            out float targetEnd,
            out float targetMaxRadius);

        bool targetIsCurrentTransitionTarget = depthOfFieldSettingsTransitionActive &&
                                               Mathf.Approximately(depthOfFieldSettingsTargetStart, targetStart) &&
                                               Mathf.Approximately(depthOfFieldSettingsTargetEnd, targetEnd) &&
                                               Mathf.Approximately(depthOfFieldSettingsTargetMaxRadius, targetMaxRadius);
        if (targetIsCurrentTransitionTarget ||
            !depthOfFieldSettingsTransitionActive &&
            Mathf.Approximately(depthOfFieldConfiguredStart, targetStart) &&
            Mathf.Approximately(depthOfFieldConfiguredEnd, targetEnd) &&
            Mathf.Approximately(depthOfFieldConfiguredMaxRadius, targetMaxRadius))
        {
            return;
        }

        depthOfFieldSettingsStartStart = depthOfFieldConfiguredStart;
        depthOfFieldSettingsStartEnd = depthOfFieldConfiguredEnd;
        depthOfFieldSettingsStartMaxRadius = depthOfFieldConfiguredMaxRadius;
        depthOfFieldSettingsTargetStart = targetStart;
        depthOfFieldSettingsTargetEnd = targetEnd;
        depthOfFieldSettingsTargetMaxRadius = targetMaxRadius;
        depthOfFieldSettingsTransitionElapsed = 0f;
        depthOfFieldSettingsTransitionActive = depthOfFieldFadeDuration > 0f;

        if (!depthOfFieldSettingsTransitionActive)
        {
            depthOfFieldConfiguredStart = targetStart;
            depthOfFieldConfiguredEnd = targetEnd;
            depthOfFieldConfiguredMaxRadius = targetMaxRadius;
        }
    }

    private void UpdateDepthOfFieldSettingsTransition(float currentStrength)
    {
        if (!depthOfFieldSettingsTransitionActive || depthOfField == null)
            return;

        depthOfFieldSettingsTransitionElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(
            depthOfFieldSettingsTransitionElapsed / depthOfFieldFadeDuration);
        float easedTime = EvaluateNormalizedCurve(depthOfFieldFadeCurve, normalizedTime);

        depthOfFieldConfiguredStart = Mathf.Lerp(
            depthOfFieldSettingsStartStart,
            depthOfFieldSettingsTargetStart,
            easedTime);
        depthOfFieldConfiguredEnd = Mathf.Lerp(
            depthOfFieldSettingsStartEnd,
            depthOfFieldSettingsTargetEnd,
            easedTime);
        depthOfFieldConfiguredMaxRadius = Mathf.Lerp(
            depthOfFieldSettingsStartMaxRadius,
            depthOfFieldSettingsTargetMaxRadius,
            easedTime);

        depthOfField.gaussianStart.Override(depthOfFieldConfiguredStart);
        depthOfField.gaussianMaxRadius.Override(depthOfFieldConfiguredMaxRadius);
        ApplyDepthOfFieldStrength(currentStrength);

        if (normalizedTime < 1f)
            return;

        depthOfFieldConfiguredStart = depthOfFieldSettingsTargetStart;
        depthOfFieldConfiguredEnd = depthOfFieldSettingsTargetEnd;
        depthOfFieldConfiguredMaxRadius = depthOfFieldSettingsTargetMaxRadius;
        depthOfFieldSettingsTransitionActive = false;
    }

    private void GetDepthOfFieldPresetSettings(
        ZoomPreset preset,
        out float start,
        out float end,
        out float maxRadius)
    {
        bool hasValidPresetRange = preset.depthOfFieldEnd > preset.depthOfFieldStart;
        start = hasValidPresetRange
            ? Mathf.Max(0f, preset.depthOfFieldStart)
            : depthOfFieldProfileStart;
        end = hasValidPresetRange
            ? Mathf.Max(start + 0.01f, preset.depthOfFieldEnd)
            : depthOfFieldProfileEnd;
        maxRadius = preset.depthOfFieldMaxRadius >= 0.5f
            ? Mathf.Clamp(preset.depthOfFieldMaxRadius, 0.5f, 1.5f)
            : depthOfFieldProfileMaxRadius;
    }

    private static float EvaluateNormalizedCurve(AnimationCurve curve, float normalizedTime)
    {
        if (curve == null || curve.length == 0)
            return Mathf.SmoothStep(0f, 1f, normalizedTime);

        float curveStart = curve.Evaluate(0f);
        float curveEnd = curve.Evaluate(1f);
        if (Mathf.Approximately(curveStart, curveEnd))
            return Mathf.SmoothStep(0f, 1f, normalizedTime);

        return Mathf.Clamp01(Mathf.InverseLerp(
            curveStart,
            curveEnd,
            curve.Evaluate(normalizedTime)));
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
        [InspectorName("Depth Of Field")]
        public bool enableDepthOfField;
        [Min(0f)] public float depthOfFieldStart;
        [Min(0f)] public float depthOfFieldEnd;
        [Range(0.5f, 1.5f)] public float depthOfFieldMaxRadius;

        public ZoomPreset(
            string name,
            float topRadius,
            float topHeight,
            float centerRadius,
            float centerHeight,
            float bottomRadius,
            float bottomHeight,
            bool enableDepthOfField = false,
            float depthOfFieldStart = 10f,
            float depthOfFieldEnd = 30f,
            float depthOfFieldMaxRadius = 1f)
        {
            this.name = name;

            this.topRadius = topRadius;
            this.topHeight = topHeight;

            this.centerRadius = centerRadius;
            this.centerHeight = centerHeight;

            this.bottomRadius = bottomRadius;
            this.bottomHeight = bottomHeight;
            this.enableDepthOfField = enableDepthOfField;
            this.depthOfFieldStart = depthOfFieldStart;
            this.depthOfFieldEnd = depthOfFieldEnd;
            this.depthOfFieldMaxRadius = depthOfFieldMaxRadius;
        }
    }

    public void SetZoomState(CameraZoomState state)
    {
        SetZoomPreset(state.ToString());
    }

    public void SetMirrorZoneActive(bool active)
    {
        if (!hasMirrorPreset || mirrorZoneActive == active)
            return;

        mirrorZoneActive = active;
        if (!active && targetZoomIndex == 0)
            SetZoomIndex(1);
    }

    public void SetZoomInOneStep(float transitionDuration = -1f)
    {
        SetZoomIndex(Mathf.Max(hasMirrorPreset ? 1 : 0, targetZoomIndex - 1), transitionDuration);
    }

    public bool SetZoomPreset(string presetName, float transitionDuration = -1f)
    {
        if (zoomPresets == null || zoomPresets.Length == 0 || string.IsNullOrWhiteSpace(presetName))
            return false;

        for (int i = 0; i < zoomPresets.Length; i++)
        {
            if (string.Equals(zoomPresets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
            {
                SetZoomIndex(i, transitionDuration);
                return true;
            }
        }

        Debug.LogWarning($"{name}: Zoom preset '{presetName}' was not found.");
        return false;
    }

    public bool BeginDialogueZoomPreset(string presetName, float preRollDuration)
    {
        if (!dialogAutoRotateActive)
        {
            dialoguePreviousZoomIndex = targetZoomIndex;
            dialogAutoRotateActive = true;
            autoRotateCamera = false;
            dialogAutoRotationSuppressedByManualInput = false;
        }

        dialogueZoomWasOverridden = true;
        if (!TryGetZoomPresetIndex(presetName, out _))
            return false;

        StartDialogueZoomPreRoll(presetName, preRollDuration);
        return true;
    }

    private bool TryGetZoomPresetIndex(string presetName, out int presetIndex)
    {
        presetIndex = -1;
        if (zoomPresets == null || zoomPresets.Length == 0 || string.IsNullOrWhiteSpace(presetName))
            return false;

        for (int i = 0; i < zoomPresets.Length; i++)
        {
            if (!string.Equals(zoomPresets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
                continue;

            presetIndex = i;
            return true;
        }

        Debug.LogWarning($"{name}: Zoom preset '{presetName}' was not found.");
        return false;
    }

    public bool SetDialogueReturnZoomPreset(string presetName)
    {
        if (zoomPresets == null || string.IsNullOrWhiteSpace(presetName))
            return false;

        for (int i = 0; i < zoomPresets.Length; i++)
        {
            if (!string.Equals(zoomPresets[i].name, presetName, StringComparison.OrdinalIgnoreCase))
                continue;

            dialoguePreviousZoomIndex = i;
            return true;
        }

        Debug.LogWarning($"{name}: Dialogue return zoom preset '{presetName}' was not found.");
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

    public void BeginLoupeOrbitPivot(Transform pivot, Vector2 screenPosition)
    {
        if (cineCamera == null || pivot == null || !IsActiveGameplayCamera())
            return;

        if (!hasLoupeOrbitFollowOverride)
        {
            Vector3 cameraPosition = Camera.main != null
                ? Camera.main.transform.position
                : cineCamera.State.GetFinalPosition();
            Quaternion cameraRotation = Camera.main != null
                ? Camera.main.transform.rotation
                : cineCamera.State.GetFinalOrientation();

            loupeOrbitGameplayStartHorizontalAxis = orbitalFollow.HorizontalAxis.Value;
            loupeOrbitLockedVerticalAxis = orbitalFollow.VerticalAxis.Value;
            loupeOrbitAppliedYaw = 0f;
            loupeOrbitPivotPosition = pivot.position;
            loupeOrbitMainCameraOffset = cameraPosition - loupeOrbitPivotPosition;
            loupeOrbitMainCameraRotation = cameraRotation;
            loupeOrbitOrbitalFollowWasEnabled = orbitalFollow.enabled;
            loupeOrbitRotationComposerWasEnabled = rotationComposer != null && rotationComposer.enabled;
            hasLoupeOrbitFollowOverride = true;

            orbitalFollow.enabled = false;
            if (rotationComposer != null)
                rotationComposer.enabled = false;

            cineCamera.ForceCameraPosition(cameraPosition, cameraRotation);
        }
    }

    public void ApplyLoupeOrbitYaw(float yaw)
    {
        if (orbitalFollow == null || !hasLoupeOrbitFollowOverride)
            return;

        loupeOrbitAppliedYaw = yaw;
        Quaternion yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
        Vector3 rotatedOffset = yawRotation * loupeOrbitMainCameraOffset;
        Vector3 cameraPosition = loupeOrbitPivotPosition + rotatedOffset;
        Quaternion cameraRotation = yawRotation * loupeOrbitMainCameraRotation;
        cineCamera.ForceCameraPosition(cameraPosition, cameraRotation);
    }

    public void RestoreLoupeOrbitPivot()
    {
        if (cineCamera == null || !hasLoupeOrbitFollowOverride)
            return;

        orbitalFollow.enabled = loupeOrbitOrbitalFollowWasEnabled;
        if (rotationComposer != null)
            rotationComposer.enabled = loupeOrbitRotationComposerWasEnabled;

        orbitalFollow.HorizontalAxis.Value = loupeOrbitGameplayStartHorizontalAxis + loupeOrbitAppliedYaw;
        orbitalFollow.VerticalAxis.Value = loupeOrbitLockedVerticalAxis;

        hasLoupeOrbitFollowOverride = false;
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

    public void OverrideLookAtTargetSmooth(Transform target, float transitionSpeed)
    {
        if (cineCamera == null || target == null)
            return;

        if (!hasLookAtOverride)
        {
            lookAtTargetBeforeOverride = cineCamera.LookAt;
            hasLookAtOverride = true;
        }

        EnsureSmoothLookAtProxy();
        smoothLookAtStartPosition = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : target.position;
        smoothLookAtProxy.position = smoothLookAtStartPosition;
        smoothLookAtDestination = target;
        smoothLookAtDestinationPosition = target.position;
        smoothLookAtUsesAnchoredDestination = false;
        smoothLookAtTransitionDuration = CalculateLookAtTransitionDuration(
            smoothLookAtStartPosition,
            smoothLookAtDestinationPosition,
            transitionSpeed);
        smoothLookAtTransitionElapsed = 0f;
        restoringSmoothLookAtTarget = false;
        cineCamera.LookAt = smoothLookAtProxy;

        if (smoothLookAtTransitionDuration <= 0f)
            CompleteSmoothLookAtTransition();
    }

    public void BeginDialogueLookAt(Transform target, float preRollDuration)
    {
        if (cineCamera == null || target == null || !IsActiveGameplayCamera())
            return;

        if (!hasLookAtOverride)
        {
            lookAtTargetBeforeOverride = cineCamera.LookAt;
            hasLookAtOverride = true;
        }

        EnsureDialogueLookAtProxy();
        dialogueLookAtStartPosition = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : target.position;
        dialogueLookAtTargetPosition = target.position;
        dialogueLookAtPreRollElapsed = 0f;
        dialogueLookAtPreRollDuration = Mathf.Max(0f, preRollDuration);
        dialogueLookAtPreRollActive = dialogueLookAtPreRollDuration > 0f;
        dialogueLookAtManagedExternally = true;
        dialogueLookAtGenericAnchor = false;
        dialogueLookAtProxy.position = dialogueLookAtStartPosition;
        cineCamera.LookAt = dialogueLookAtProxy;

        if (!dialogueLookAtPreRollActive)
            CompleteDialogueLookAtPreRoll();
    }

    public void RestoreDialogueLookAt(Transform target, float transitionSpeed)
    {
        dialogueLookAtPreRollActive = false;
        dialogueLookAtManagedExternally = false;
        dialogueLookAtGenericAnchor = false;
        ReturnLookAtToTargetSmooth(target, transitionSpeed);
    }

    private void EnsureDialogueLookAtProxy()
    {
        if (dialogueLookAtProxy != null)
            return;

        GameObject proxy = new GameObject($"{name}_DialogueLookAtTarget");
        proxy.hideFlags = HideFlags.HideInHierarchy;
        dialogueLookAtProxy = proxy.transform;
    }

    private void UpdateDialogueLookAtPreRoll()
    {
        if (!dialogueLookAtPreRollActive || dialogueLookAtProxy == null || cineCamera == null ||
            cineCamera.LookAt != dialogueLookAtProxy)
            return;

        dialogueLookAtPreRollElapsed += Time.unscaledDeltaTime;
        float normalizedTime = Mathf.Clamp01(dialogueLookAtPreRollElapsed / dialogueLookAtPreRollDuration);
        dialogueLookAtProxy.position = Vector3.Lerp(
            dialogueLookAtStartPosition,
            dialogueLookAtTargetPosition,
            Mathf.SmoothStep(0f, 1f, normalizedTime));

        if (normalizedTime >= 1f)
            CompleteDialogueLookAtPreRoll();
    }

    private void CompleteDialogueLookAtPreRoll()
    {
        if (dialogueLookAtProxy != null)
        {
            dialogueLookAtProxy.position = dialogueLookAtTargetPosition;
            cineCamera.LookAt = dialogueLookAtProxy;
        }

        dialogueLookAtPreRollActive = false;
    }

    private void HandleConversationUIWillShow()
    {
        if (!IsActiveGameplayCamera())
            return;

        NPCConversation conversation = ConversationManager.Instance != null
            ? ConversationManager.Instance.ActiveConversationSource
            : null;
        if (conversation != null && !conversation.UseAutomaticDialogueCamera)
            return;

        if (dialogueZoomPreRollActive)
            CompleteDialogueZoomPreRoll();

        if (dialogueLookAtPreRollActive)
        {
            CompleteDialogueLookAtPreRoll();
            return;
        }

        if (dialogueLookAtManagedExternally || dialogueLookAtGenericAnchor || cineCamera == null)
            return;

        if (!hasLookAtOverride)
        {
            lookAtTargetBeforeOverride = cineCamera.LookAt;
            hasLookAtOverride = true;
        }

        EnsureDialogueLookAtProxy();
        dialogueLookAtProxy.position = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : transform.position;
        cineCamera.LookAt = dialogueLookAtProxy;
        dialogueLookAtGenericAnchor = true;
    }

    private void HandleConversationUIHidden()
    {
        if (!dialogueLookAtGenericAnchor)
            return;

        dialogueLookAtGenericAnchor = false;
        RestoreLookAtTargetSmooth(dialogueLookAtReturnSpeed);
    }

    public void RestoreLookAtTarget()
    {
        if (cineCamera == null || !hasLookAtOverride)
            return;

        cineCamera.LookAt = lookAtTargetBeforeOverride;
        hasLookAtOverride = false;
        smoothLookAtDestination = null;
        smoothLookAtDestinationPosition = Vector3.zero;
        smoothLookAtUsesAnchoredDestination = false;
        restoringSmoothLookAtTarget = false;
    }

    public void RestoreLookAtTargetSmooth(float transitionSpeed)
    {
        if (cineCamera == null || !hasLookAtOverride)
            return;

        if (lookAtTargetBeforeOverride == null)
        {
            RestoreLookAtTarget();
            return;
        }

        EnsureSmoothLookAtProxy();
        smoothLookAtStartPosition = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : lookAtTargetBeforeOverride.position;
        smoothLookAtProxy.position = smoothLookAtStartPosition;
        smoothLookAtDestination = lookAtTargetBeforeOverride;
        smoothLookAtDestinationPosition = lookAtTargetBeforeOverride.position;
        smoothLookAtUsesAnchoredDestination = false;
        smoothLookAtTransitionDuration = CalculateLookAtTransitionDuration(
            smoothLookAtStartPosition,
            smoothLookAtDestinationPosition,
            transitionSpeed);
        smoothLookAtTransitionElapsed = 0f;
        restoringSmoothLookAtTarget = true;
        cineCamera.LookAt = smoothLookAtProxy;

        if (smoothLookAtTransitionDuration <= 0f)
            CompleteSmoothLookAtTransition();
    }

    public void RestoreLookAtTargetSmoothAndAttach(float transitionSpeed, float snapDistance)
    {
        if (cineCamera == null || !hasLookAtOverride)
            return;

        if (lookAtTargetBeforeOverride == null)
        {
            RestoreLookAtTarget();
            return;
        }

        ReturnLookAtToTargetSmoothAndAttach(
            lookAtTargetBeforeOverride,
            transitionSpeed,
            snapDistance);
    }

    public void ReturnLookAtToTargetSmooth(Transform target, float transitionSpeed)
    {
        if (target == null)
        {
            RestoreLookAtTargetSmooth(transitionSpeed);
            return;
        }

        if (cineCamera == null)
            return;

        EnsureSmoothLookAtProxy();
        smoothLookAtStartPosition = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : target.position;
        smoothLookAtProxy.position = smoothLookAtStartPosition;
        smoothLookAtDestination = target;
        smoothLookAtDestinationPosition = target.position;
        smoothLookAtUsesAnchoredDestination = false;
        smoothLookAtTransitionDuration = CalculateLookAtTransitionDuration(
            smoothLookAtStartPosition,
            smoothLookAtDestinationPosition,
            transitionSpeed);
        smoothLookAtTransitionElapsed = 0f;
        restoringSmoothLookAtTarget = true;
        cineCamera.LookAt = smoothLookAtProxy;

        if (smoothLookAtTransitionDuration <= 0f)
            CompleteSmoothLookAtTransition();
    }

    // Used only by scripted sequences that must finish at a stable point before
    // attaching the camera back to a moving gameplay target.
    public void ReturnLookAtToTargetSmoothAndAttach(Transform target, float transitionSpeed, float snapDistance)
    {
        if (target == null)
        {
            RestoreLookAtTargetSmooth(transitionSpeed);
            return;
        }

        if (cineCamera == null)
            return;

        EnsureSmoothLookAtProxy();
        smoothLookAtStartPosition = cineCamera.LookAt != null
            ? cineCamera.LookAt.position
            : target.position;
        smoothLookAtProxy.position = smoothLookAtStartPosition;
        smoothLookAtDestination = target;
        smoothLookAtDestinationPosition = target.position;
        smoothLookAtUsesAnchoredDestination = true;
        smoothLookAtAnchorSnapDistance = Mathf.Max(0.01f, snapDistance);
        smoothLookAtTransitionDuration = CalculateLookAtTransitionDuration(
            smoothLookAtStartPosition,
            smoothLookAtDestinationPosition,
            transitionSpeed);
        smoothLookAtTransitionElapsed = 0f;
        restoringSmoothLookAtTarget = true;
        cineCamera.LookAt = smoothLookAtProxy;

        if (smoothLookAtTransitionDuration <= 0f)
            CompleteSmoothLookAtTransition();
    }

    private void EnsureSmoothLookAtProxy()
    {
        if (smoothLookAtProxy != null)
            return;

        GameObject proxy = new GameObject($"{name}_SmoothLookAtTarget");
        proxy.hideFlags = HideFlags.HideInHierarchy;
        smoothLookAtProxy = proxy.transform;
    }

    private static float CalculateLookAtTransitionDuration(
        Vector3 startPosition,
        Vector3 destinationPosition,
        float transitionSpeed)
    {
        float distance = Vector3.Distance(startPosition, destinationPosition);
        return distance <= Mathf.Epsilon
            ? 0f
            : distance / Mathf.Max(0.01f, transitionSpeed);
    }

    private void UpdateSmoothLookAtTarget()
    {
        if (smoothLookAtProxy == null || smoothLookAtDestination == null || cineCamera == null ||
            cineCamera.LookAt != smoothLookAtProxy)
            return;

        smoothLookAtTransitionElapsed = Mathf.Min(
            smoothLookAtTransitionElapsed + Time.unscaledDeltaTime,
            smoothLookAtTransitionDuration);
        float normalizedTime = smoothLookAtTransitionDuration > 0f
            ? smoothLookAtTransitionElapsed / smoothLookAtTransitionDuration
            : 1f;
        float blend = Mathf.SmoothStep(0f, 1f, normalizedTime);
        Vector3 destinationPosition = smoothLookAtUsesAnchoredDestination
            ? smoothLookAtDestinationPosition
            : smoothLookAtDestination.position;

        smoothLookAtProxy.position = Vector3.Lerp(
            smoothLookAtStartPosition,
            destinationPosition,
            blend);

        if (normalizedTime < 1f)
            return;

        CompleteSmoothLookAtTransition();
    }

    private void CompleteSmoothLookAtTransition()
    {
        if (smoothLookAtDestination == null || cineCamera == null)
            return;

        Vector3 destinationPosition = smoothLookAtUsesAnchoredDestination
            ? smoothLookAtDestinationPosition
            : smoothLookAtDestination.position;

        smoothLookAtProxy.position = destinationPosition;
        cineCamera.LookAt = smoothLookAtDestination;
        smoothLookAtDestination = null;
        smoothLookAtUsesAnchoredDestination = false;

        if (restoringSmoothLookAtTarget)
        {
            hasLookAtOverride = false;
            restoringSmoothLookAtTarget = false;
        }
    }

    public void SetZoomIndex(int index, float transitionDuration = -1f)
    {
        SetZoomIndexInternal(index, transitionDuration, false);
    }

    public void SetZoomIndexImmediate(int index)
    {
        if (zoomPresets == null || zoomPresets.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(index, hasMirrorPreset && !mirrorZoneActive ? 1 : 0, zoomPresets.Length - 1);
        if (clampedIndex != targetZoomIndex)
            previousZoomIndex = targetZoomIndex;

        targetZoomIndex = clampedIndex;
        zoomTransitionActive = false;
        zoomTransitionTargetIndex = -1;
        dialogueZoomPreRollActive = false;
        dialogueZoomPreRollTargetIndex = -1;
        RestoreZoomRotationDamping();
        ApplyZoomPresetImmediate(zoomPresets[targetZoomIndex]);
        UpdateDebugZoomState();
    }

    private void SetZoomIndexInternal(int index, float transitionDuration, bool useDialogueDuration)
    {
        if (zoomPresets == null || zoomPresets.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(index, hasMirrorPreset && !mirrorZoneActive ? 1 : 0, zoomPresets.Length - 1);

        if (clampedIndex == targetZoomIndex)
            return;

        if (clampedIndex != targetZoomIndex)
            previousZoomIndex = targetZoomIndex;
        targetZoomIndex = clampedIndex;
        StartZoomTransition(targetZoomIndex, transitionDuration, useDialogueDuration);

        UpdateDebugZoomState();
    }
    public void ReturnToPreviousZoomState(float transitionDuration = -1f)
    {
        if (previousZoomIndex < 0)
            return;

        int indexToReturn = hasMirrorPreset && !mirrorZoneActive
            ? Mathf.Max(1, previousZoomIndex)
            : previousZoomIndex;

        previousZoomIndex = targetZoomIndex;
        targetZoomIndex = indexToReturn;
        StartZoomTransition(targetZoomIndex, transitionDuration);

        UpdateDebugZoomState();
    }

    private void GetManualZoomRange(out int minZoomIndex, out int maxZoomIndex)
    {
        int lastPresetIndex = Mathf.Max(0, zoomPresets.Length - 1);
        int availableMin = hasMirrorPreset && !mirrorZoneActive ? 1 : 0;
        if (livePresetPreview)
        {
            minZoomIndex = availableMin;
            maxZoomIndex = lastPresetIndex;
            return;
        }

        minZoomIndex = hasMirrorPreset && mirrorZoneActive
            ? 0
            : Mathf.Clamp(manualZoomMinIndex, availableMin, lastPresetIndex);
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

    // ---------------------------------------------------------------
    // SYSTEM ZAPISU - cel kamery
    // ---------------------------------------------------------------

    // Aktualny cel patrzenia. Jesli trwa gladkie przejscie, zwraca
    // cel docelowy, a nie pomocniczy proxy.
    public Transform CurrentLookAtTarget
    {
        get
        {
            if (cineCamera == null)
                return null;

            if (smoothLookAtDestination != null)
                return smoothLookAtDestination;

            if (hasLookAtOverride && lookAtTargetBeforeOverride != null &&
                cineCamera.LookAt == smoothLookAtProxy)
            {
                return lookAtTargetBeforeOverride;
            }

            return cineCamera.LookAt;
        }
    }

    public bool IsSmoothLookAtTransitionActive =>
        smoothLookAtProxy != null &&
        smoothLookAtDestination != null &&
        cineCamera != null &&
        cineCamera.LookAt == smoothLookAtProxy;

    // Ustawia cel kamery natychmiast i czysci wszystkie nadpisania.
    // Uzywane przy wczytywaniu zapisu, zeby kamera nie zostala
    // uwiazana do obiektu z przerwanego dialogu.
    public void ForceLookAtTarget(Transform target)
    {
        if (cineCamera == null || target == null)
            return;

        hasLookAtOverride = false;
        lookAtTargetBeforeOverride = null;
        hasTargetOverride = false;
        overriddenLookAtTarget = null;
        smoothLookAtDestination = null;
        smoothLookAtDestinationPosition = Vector3.zero;
        smoothLookAtUsesAnchoredDestination = false;
        restoringSmoothLookAtTarget = false;

        cineCamera.LookAt = target;
    }
}

public enum CameraZoomState
{
    Narrow,
    Medium,
    Wide,
    Top,
    Total
}
