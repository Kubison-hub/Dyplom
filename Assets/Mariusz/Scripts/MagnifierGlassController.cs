using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class MagnifierGlassController : MonoBehaviour
{
    private static MagnifierGlassController instance;

    public static bool IsScrollReservedForLoupe =>
        instance != null && instance.ShouldReserveScrollForLoupe;
    public static bool IsRotationReservedForLoupe =>
        instance != null && instance.ShouldReserveRotationForLoupe;
    public static bool IsWatsonGripActive =>
        WatsonEscortController.IsWatsonEagleVisionInteractionActive;

    [Header("References")]
    [SerializeField] private Camera hiddenCluesCamera;
    [SerializeField] private RenderTexture hiddenCluesTexture;
    [SerializeField] private Camera loupeSceneCamera;
    [SerializeField] private RenderTexture loupeSceneTexture;
    [SerializeField] private RectTransform loupeFrame;
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Material magnifierMaterial;

    [Header("Lens Appearance")]
    [SerializeField, Range(0f, 1f)] private float desaturateScene = 0.7f;
    [SerializeField] private float hiddenCluesStrength = 1f;
    [SerializeField, Min(1f)] private float zoom = 2f;
    [SerializeField] private float lensRadiusMultiplier = 0.32f;
    [SerializeField, Range(0.5f, 3f)] private float loupeVisualScale = 1f;
    [SerializeField] private Vector2 loupeFrameOffset = Vector2.zero;
    [SerializeField] private Vector2 lensCenterOffset = Vector2.zero;
    [SerializeField] private bool hideCursor = true;
    [SerializeField] private KeyCode loupeKey = KeyCode.F;

    [Header("Sherlock Hand Loupe")]
    [Tooltip("Physical magnifying-glass model parented to Sherlock's hand. It is visible only while Sherlock uses the loupe.")]
    [SerializeField] private GameObject sherlockHandLoupe;

    [Header("Sherlock Loupe Audio")]
    [SerializeField] private AudioSource loupeAudioSource;
    [SerializeField] private AudioClip loupeEnterAudio;
    [SerializeField] private AudioClip loupeExitAudio;

    [Header("Sherlock Loupe Hand IK")]
    [Tooltip("Rig containing the RightHand Two Bone IK Constraint.")]
    [SerializeField] private Rig rightHandLoupeRig;
    [Tooltip("Target assigned to the Two Bone IK Constraint.")]
    [SerializeField] private Transform rightHandTarget;
    [Tooltip("A separate rest marker for the target. Parent it to Sherlock's chest or rig root, never to the IK arm.")]
    [SerializeField] private Transform rightHandRestTarget;
    [Tooltip("Shared world-space target placed at the same RaycastHit point examined by the loupe.")]
    [SerializeField] private Transform loupeAimTarget;
    [Tooltip("World-space correction applied to the loupe RaycastHit point before it is used as the shared Aim Target.")]
    [SerializeField] private Vector3 loupeAimTargetOffset = Vector3.zero;
    [Tooltip("Time in seconds used to smooth RaycastHit changes before they reach Sherlock's loupe hand IK.")]
    [SerializeField, Min(0.01f)] private float loupeAimTargetSmoothTime = 0.18f;
    [SerializeField, Min(0.01f)] private float rightHandRigBlendDuration = 0.2f;
    [SerializeField, Range(1f, 90f)] private float maxLoupeHandCursorAngle = 50f;
    [SerializeField, Min(0f)] private float maxLoupeHandHorizontalOffset = 0.14f;
    [SerializeField, Min(0f)] private float maxLoupeHandVerticalOffset = 0.07f;
    [Tooltip("Horizontal distance from Sherlock at which the hand begins to extend.")]
    [SerializeField, Min(0f)] private float loupeHandNearDistance = 1f;
    [Tooltip("Horizontal distance from Sherlock at which the hand reaches its maximum extension.")]
    [SerializeField, Min(0.01f)] private float loupeHandFarDistance = 4f;
    [Tooltip("Forward offset applied to the hand target for a close inspection point.")]
    [SerializeField] private float loupeHandNearForwardOffset = 0f;
    [Tooltip("Forward offset applied to the hand target for a distant inspection point.")]
    [SerializeField] private float loupeHandFarForwardOffset = 0.16f;
    [SerializeField, Min(0.01f)] private float loupeHandOffsetSmoothTime = 0.15f;
    [SerializeField] private bool rotateRightHandTargetTowardAimTarget = true;
    [Tooltip("Corrects the model's local axes while the target aims at the shared Aim Target.")]
    [SerializeField] private Vector3 rightHandTargetAimRotationOffset;
    [SerializeField, Min(0.01f)] private float rightHandTargetRotationSmoothTime = 0.12f;

    [Header("Custom Loupe Cursor")]
    [SerializeField] private RectTransform loupeCursorFlare;
    [SerializeField] private CanvasGroup loupeCursorFlareCanvasGroup;
    [SerializeField] private Vector2 loupeCursorFlareOffset = new Vector2(14f, -12f);
    [SerializeField, Min(0f)] private float loupeCursorPulseSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float loupeCursorMinAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float loupeCursorMaxAlpha = 0.9f;
    [SerializeField, Min(0f)] private float loupeCursorScalePulse = 0.08f;

    [Header("Watson Grip Cursor")]
    [Tooltip("A hand cursor shown instead of Sherlock's loupe while Watson holds F.")]
    [SerializeField] private RectTransform watsonGripCursor;
    [SerializeField] private CanvasGroup watsonGripCursorCanvasGroup;
    [SerializeField] private Vector2 watsonGripCursorOffset = new Vector2(18f, -18f);

    [Header("Watson Escort Cursor")]
    [Tooltip("Shown while Watson escorts an NPC and points at walkable ground in Vision Eye.")]
    [SerializeField] private RectTransform watsonEscortMoveCursor;
    [Tooltip("Shown while Watson escorts an NPC and points at another escortable NPC in Vision Eye.")]
    [SerializeField] private RectTransform watsonEscortNpcCursor;

    [Header("Watson Cursor Overrides")]
    [Tooltip("Optional cursor used over a WatsonCarryable. Leave empty to keep the CustomCursor default.")]
    [SerializeField] private Texture2D watsonCarryCursorTexture;
    [SerializeField] private Vector2 watsonCarryCursorHotSpot = Vector2.zero;
    [Tooltip("Optional cursor used over a WatsonEscortNPC. Leave empty to keep the CustomCursor default.")]
    [SerializeField] private Texture2D watsonEscortCursorTexture;
    [SerializeField] private Vector2 watsonEscortCursorHotSpot = Vector2.zero;

    [Header("3D Inspection")]
    [SerializeField] private bool useSurfaceInspection = true;
    [SerializeField] private LayerMask inspectionLayers = ~0;
    [SerializeField] private QueryTriggerInteraction inspectionTriggerInteraction = QueryTriggerInteraction.Ignore;
    [SerializeField, Min(0.31f)] private float lensDistanceFromHit = 0.45f;
    [SerializeField, Range(0f, 1f)] private float normalInfluence = 0.4f;
    [SerializeField, Range(0f, 90f)] private float maxNormalTiltDegrees = 30f;
    [SerializeField, Min(0f)] private float cameraFollowSpeed = 18f;
    [SerializeField] private float raycastDistance = 100f;

    [Header("Player Camera Look At")]
    [Tooltip("When enabled, the loupe may temporarily change the gameplay camera LookAt target.")]
    [SerializeField] private bool changeLookAt = false;
    [SerializeField, Range(0f, 1f)] private float loupeLookAtRaycastInfluence = 0.12f;
    [SerializeField, Min(0.01f)] private float loupeLookAtSmoothTime = 1.2f;

    [Header("Sherlock Loupe Rotation")]
    [Tooltip("While Sherlock holds F, he turns toward the point currently examined by the loupe.")]
    [SerializeField] private bool rotateSherlockTowardLoupeHit = true;
    [Tooltip("Time in seconds Sherlock needs to smoothly settle into the loupe direction.")]
    [SerializeField, Min(0.01f)] private float sherlockLoupeRotationDamping = 0.45f;

    [Header("Experimental Lens Scroll")]
    [SerializeField] private bool enableLensDistanceScroll = true;
    [SerializeField, Min(0.00001f)] private float lensScrollSensitivity = 0.00025f;
    [SerializeField, Min(0.31f)] private float minLensDistanceFromHit = 0.31f;
    [SerializeField, Min(0.31f)] private float maxLensDistanceFromHit = 1.2f;
    [SerializeField] private bool enableLoupeSizeScroll = true;
    [SerializeField, Min(0.00001f)] private float loupeSizeScrollSensitivity = 0.001f;

    [Header("Loupe Orbit Rotation")]
    [Tooltip("When enabled, right mouse drag rotates the loupe cameras around the inspected point instead of rotating the gameplay camera.")]
    [SerializeField] private bool enableLoupeOrbitRotation = true;
    [Tooltip("When enabled, right mouse drag also rotates the active gameplay camera through its normal camera input.")]
    [SerializeField] private bool allowMainCameraRotationDuringLoupeOrbit;
    [Tooltip("When enabled, the gameplay camera copies only the loupe yaw and uses its RaycastHit as the pivot while preserving its own zoom and vertical axis.")]
    [SerializeField] private bool mainCameraUsesLoupeHitPivot;
    [Tooltip("Maximum horizontal orbit angle relative to the view captured when orbiting begins.")]
    [SerializeField, Range(0f, 180f)] private float loupeOrbitMaxYaw = 45f;
    [Tooltip("Maximum vertical orbit angle relative to the view captured when orbiting begins.")]
    [SerializeField, Range(0f, 89f)] private float loupeOrbitMaxPitch = 30f;

    [Header("First Sherlock Loupe Tutorial")]
    [SerializeField] private bool enableFirstUseTutorialPopup;
    [SerializeField] private string firstUseTutorialTitle = "LUPA SHERLOCKA";
    [SerializeField, TextArea] private string firstUseTutorialText =
        "Sherlock moze uzywac swojej lupy, aby odkrywac szczegoly i tropy.";
    [SerializeField] private VideoClip firstUseTutorialVideoClip;

    [Header("Debug")]
    [Tooltip("Press G to toggle Sherlock's loupe for rig and animation testing.")]
    [SerializeField] private bool enableDebugLoupeToggle = true;
    [SerializeField] private KeyCode debugLoupeToggleKey = KeyCode.G;
    [SerializeField] private bool drawInspectionGizmos = true;
    [SerializeField] private float gizmoSphereRadius = 0.08f;

    private readonly Vector3[] corners = new Vector3[4];
    private Vector3 loupeFrameBaseScale;
    private Vector3 loupeCursorFlareBaseScale;
    private Vector3 watsonGripCursorBaseScale;
    private bool hasInspectionHit;
    private Vector3 lastRayOrigin;
    private Vector3 lastHitPoint;
    private Vector3 lastHitNormal;
    private Vector3 lastMainCameraPosition;
    private Vector3 lastLensCameraPosition;
    private Vector3 lastHybridLookPoint;
    private RaycastHit lastInspectionHit;
    private Transform loupeLookAtTarget;
    private Transform mainCameraLoupeOrbitPivot;
    private CameraController loupeLookAtCameraController;
    private CameraController mainCameraLoupeOrbitController;
    private Vector3 loupeLookAtVelocity;
    private bool resetLoupeLookAtOnNextUse = true;
    private bool wasLoupeHeld;
    private bool wasSherlockLoupeActive;
    private bool firstUseTutorialShown;
    private float sherlockLoupeRotationVelocity;
    private Vector3 rightHandTargetVelocity;
    private Vector3 loupeAimTargetVelocity;
    private bool resetLoupeAimTargetOnNextHit = true;
    private bool debugLoupeToggleActive;
    private bool loupeSuppressedUntilKeyRelease;
    private bool loupeOrbitActive;
    private bool hasLoupeOrbitOffset;
    private bool loupeOrbitReturning;
    private Vector3 loupeOrbitPivot;
    private Vector3 loupeOrbitBaseDirection;
    private Vector2 loupeOrbitMousePosition;
    private bool useLoupeOrbitMousePositionThisFrame;
    private Vector2 pendingLoupeOrbitInput;
    private float loupeOrbitYaw;
    private float loupeOrbitPitch;
    private float loupeOrbitRotationSpeed;
    private Vector3 loupeOrbitInspectionCameraOffset;
    private Quaternion loupeOrbitInspectionCameraRotation;

    private bool ShouldReserveScrollForLoupe =>
        isActiveAndEnabled && !IsWatsonActive() && IsLoupeHeld();

    private bool ShouldReserveRotationForLoupe =>
        ShouldHandleLoupeRotation &&
        (mainCameraUsesLoupeHitPivot || !allowMainCameraRotationDuringLoupeOrbit);

    private bool ShouldHandleLoupeRotation =>
        isActiveAndEnabled && enableLoupeOrbitRotation && !IsWatsonActive() && IsLoupeHeld();

    public static bool TryConsumeCameraRotation(Vector2 input, float rotationSpeed)
    {
        return instance != null && instance.TryConsumeLoupeRotation(input, rotationSpeed);
    }

    public bool TryGetActiveLoupeHit(out RaycastHit hit)
    {
        hit = lastInspectionHit;
        return IsLoupeHeld() && hasInspectionHit;
    }

    public bool IsLoupeActive => IsLoupeHeld();

    public static void ForceCloseLoupeUntilKeyReleased()
    {
        instance?.SuppressLoupeUntilKeyReleased();
    }

    private Vector3 GetLoupeAimPosition()
    {
        return loupeAimTarget != null
            ? loupeAimTarget.position
            : lastInspectionHit.point + loupeAimTargetOffset;
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        loupeFrameBaseScale = loupeFrame != null ? loupeFrame.localScale : Vector3.one;
        loupeCursorFlareBaseScale = loupeCursorFlare != null ? loupeCursorFlare.localScale : Vector3.one;
        watsonGripCursorBaseScale = watsonGripCursor != null ? watsonGripCursor.localScale : Vector3.one;
        ApplyLoupeVisualScale();
        DetachInspectionCamera(hiddenCluesCamera);
        DetachInspectionCamera(loupeSceneCamera);
        ConfigureMaterial();
        SetInspectionCamerasActive(false);
        HideCustomLoupeCursor();
        HideWatsonGripCursor();
        HideLoupe();
        SetSherlockHandLoupeVisible(false);
        UpdateRightHandLoupeRig(false, true);
        wasSherlockLoupeActive = false;
    }

    private void Update()
    {
        UpdateDebugLoupeToggle();

        if (DialogueEditor.ConversationManager.Instance != null &&
            DialogueEditor.ConversationManager.Instance.IsConversationActive ||
            NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen ||
            TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
            TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput)
        {
            EndLoupeOrbit(true);
            UpdateSherlockLoupeAudio(false);
            SetSherlockHandLoupeVisible(false);
            ResetLoupeAimTargetSmoothing();
            UpdateRightHandLoupeRig(false);
            SetInspectionCamerasActive(false);
            EagleVisionScanner.Instance?.SetTooltipParentForLoupe(false);
            HideLoupe();
            return;
        }

        bool loupeHeld = IsLoupeHeld();
        bool isWatsonGrip = IsWatsonGripHeld();

        if (loupeOrbitActive && (!enableLoupeOrbitRotation || !loupeHeld || isWatsonGrip))
            EndLoupeOrbit(true);

        bool sherlockLoupeActive = loupeHeld && !isWatsonGrip;
        UpdateSherlockLoupeAudio(sherlockLoupeActive);
        SetSherlockHandLoupeVisible(sherlockLoupeActive);
        TryShowFirstSherlockLoupeTutorial(loupeHeld);
        wasLoupeHeld = loupeHeld;
        SetInspectionCamerasActive(loupeHeld && !isWatsonGrip);
        EagleVisionScanner.Instance?.SetTooltipParentForLoupe(loupeHeld && !isWatsonGrip);

        if (isWatsonGrip)
        {
            EndLoupeOrbit(true);
            ResetLoupeAimTargetSmoothing();
            UpdateRightHandLoupeRig(false);
            SetInspectionCamerasActive(false);
            EagleVisionScanner.Instance?.SetTooltipParentForLoupe(false);
            HideLoupeFrame();
            HideWatsonEscortCursors();
            HideWatsonGripCursor();
            UpdateWatsonInteractionCursor();
            return;
        }

        if (ShouldShowWatsonEscortCursor())
        {
            HideLoupeFrame();
            UpdateWatsonInteractionCursor();
            return;
        }

        if (!loupeHeld)
        {
            EndLoupeOrbit(true);
            hasInspectionHit = false;
            ResetLoupeAimTargetSmoothing();
            UpdateRightHandLoupeRig(false);
            RestoreLoupeLookAt();
            HideLoupe();
            return;
        }

        if (Mouse.current == null || magnifierMaterial == null || loupeFrame == null)
        {
            ResetLoupeAimTargetSmoothing();
            UpdateRightHandLoupeRig(false);
            return;
        }

        if (loupeOrbitActive && !Mouse.current.rightButton.isPressed)
            StopLoupeOrbitDrag(true);

        UpdateLoupeOrbitReturn();

        Vector2 mousePosition = loupeOrbitActive || useLoupeOrbitMousePositionThisFrame
            ? loupeOrbitMousePosition
            : Mouse.current.position.ReadValue();
        useLoupeOrbitMousePositionThisFrame = false;
        Vector2 lensCenterScreen = mousePosition + lensCenterOffset;

        ApplyLoupeVisualScale();
        loupeFrame.position = mousePosition + loupeFrameOffset;
        if (loupeOrbitActive)
            UpdateLoupeOrbitCameras();
        else
            UpdateInspectionCameras(lensCenterScreen);
        UpdateRightHandLoupeRig(hasInspectionHit);
        HandleLensDistanceScroll();
        UpdateMaterial(lensCenterScreen);
        ShowLoupe();
        UpdateCustomLoupeCursor();
    }

    private void LateUpdate()
    {
        if ((DialogueEditor.ConversationManager.Instance != null &&
             DialogueEditor.ConversationManager.Instance.IsConversationActive) ||
            (NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen) ||
            (TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput) ||
            (TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput) ||
            !rotateSherlockTowardLoupeHit || !IsLoupeHeld() || !hasInspectionHit || IsWatsonActive())
        {
            sherlockLoupeRotationVelocity = 0f;
            return;
        }

        Transform sherlock = GetSherlockTransform();
        if (sherlock == null)
            return;

        Vector3 direction = GetLoupeAimPosition() - sherlock.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        float targetYaw = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles.y;
        float smoothYaw = Mathf.SmoothDampAngle(
            sherlock.eulerAngles.y,
            targetYaw,
            ref sherlockLoupeRotationVelocity,
            sherlockLoupeRotationDamping,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        sherlock.rotation = Quaternion.Euler(0f, smoothYaw, 0f);
    }

    private void HandleLensDistanceScroll()
    {
        if (!ShouldReserveScrollForLoupe || Mouse.current == null)
            return;

        float scrollDelta = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollDelta) < 0.01f)
            return;

        if (enableLoupeSizeScroll && Input.GetKey(KeyCode.LeftAlt))
        {
            loupeVisualScale = Mathf.Clamp(
                loupeVisualScale + scrollDelta * loupeSizeScrollSensitivity,
                0.5f,
                3f
            );
            ApplyLoupeVisualScale();
            return;
        }

        if (!enableLensDistanceScroll || !hasInspectionHit)
            return;

        float minDistance = Mathf.Min(minLensDistanceFromHit, maxLensDistanceFromHit);
        float maxDistance = Mathf.Max(minLensDistanceFromHit, maxLensDistanceFromHit);
        lensDistanceFromHit = Mathf.Clamp(
            lensDistanceFromHit - scrollDelta * lensScrollSensitivity,
            minDistance,
            maxDistance
        );
    }

    private bool TryConsumeLoupeRotation(Vector2 input, float rotationSpeed)
    {
        if (!ShouldHandleLoupeRotation)
            return false;

        loupeOrbitRotationSpeed = Mathf.Max(0f, rotationSpeed);

        if (input.sqrMagnitude <= 0.0001f)
            return ShouldReserveRotationForLoupe;

        if (!loupeOrbitActive)
        {
            if (!hasInspectionHit)
                return ShouldReserveRotationForLoupe;

            BeginLoupeOrbit();
        }

        pendingLoupeOrbitInput += input;
        return ShouldReserveRotationForLoupe;
    }

    private void BeginLoupeOrbit()
    {
        loupeOrbitPivot = lastInspectionHit.point;
        loupeOrbitMousePosition = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        Vector3 pivotToCamera;
        if (Camera.main != null)
            pivotToCamera = Camera.main.transform.position - loupeOrbitPivot;
        else
            pivotToCamera = -lastInspectionHit.normal;

        if (pivotToCamera.sqrMagnitude < 0.0001f)
            pivotToCamera = -lastInspectionHit.normal;
        if (pivotToCamera.sqrMagnitude < 0.0001f)
            pivotToCamera = Vector3.back;

        loupeOrbitBaseDirection = pivotToCamera.normalized;
        if (loupeSceneCamera != null)
        {
            loupeOrbitInspectionCameraOffset = loupeSceneCamera.transform.position - loupeOrbitPivot;
            loupeOrbitInspectionCameraRotation = loupeSceneCamera.transform.rotation;
        }
        else
        {
            loupeOrbitInspectionCameraOffset = loupeOrbitBaseDirection * lensDistanceFromHit;
            loupeOrbitInspectionCameraRotation = Quaternion.LookRotation(-loupeOrbitBaseDirection, Vector3.up);
        }
        pendingLoupeOrbitInput = Vector2.zero;
        loupeOrbitActive = true;
        hasLoupeOrbitOffset = true;
        loupeOrbitReturning = false;
        BeginMainCameraLoupeOrbitPivot();
    }

    private void UpdateLoupeOrbitCameras()
    {
        Vector2 input = pendingLoupeOrbitInput;
        pendingLoupeOrbitInput = Vector2.zero;

        float inputScale = loupeOrbitRotationSpeed * Time.unscaledDeltaTime;
        loupeOrbitYaw = Mathf.Clamp(
            loupeOrbitYaw + input.x * inputScale,
            -loupeOrbitMaxYaw,
            loupeOrbitMaxYaw);
        loupeOrbitPitch = 0f;

        if (mainCameraLoupeOrbitController != null)
            mainCameraLoupeOrbitController.ApplyLoupeOrbitYaw(loupeOrbitYaw);

        Quaternion yawRotation = Quaternion.AngleAxis(loupeOrbitYaw, Vector3.up);
        Vector3 cameraPosition = loupeOrbitPivot + yawRotation * loupeOrbitInspectionCameraOffset;
        Quaternion cameraRotation = yawRotation * loupeOrbitInspectionCameraRotation;

        ApplyInspectionPose(cameraPosition, cameraRotation, GetCameraBlend());

        hasInspectionHit = true;
        UpdateLoupeAimTarget(loupeOrbitPivot + loupeAimTargetOffset);
        if (mainCameraLoupeOrbitController == null)
            UpdateLoupeLookAt(GetLoupeAimPosition());
        lastHitPoint = loupeOrbitPivot;
        lastLensCameraPosition = cameraPosition;
        lastHybridLookPoint = loupeOrbitPivot;
    }

    private Vector3 ApplyLoupeOrbit(Vector3 baseDirection)
    {
        Vector3 yawDirection = Quaternion.AngleAxis(loupeOrbitYaw, Vector3.up) * baseDirection.normalized;
        Vector3 lookDirectionAfterYaw = -yawDirection;
        Vector3 pitchAxis = Vector3.Cross(Vector3.up, lookDirectionAfterYaw);
        if (pitchAxis.sqrMagnitude < 0.0001f)
            pitchAxis = Vector3.right;
        else
            pitchAxis.Normalize();

        return (Quaternion.AngleAxis(loupeOrbitPitch, pitchAxis) * yawDirection).normalized;
    }

    private void StopLoupeOrbitDrag(bool restoreCursorPosition)
    {
        if (!loupeOrbitActive)
            return;

        if (restoreCursorPosition && Mouse.current != null)
        {
            Mouse.current.WarpCursorPosition(loupeOrbitMousePosition);
            useLoupeOrbitMousePositionThisFrame = true;
        }

        loupeOrbitActive = false;
        pendingLoupeOrbitInput = Vector2.zero;
        // Releasing PPM ends the drag, but keeps the camera orientation reached
        // during the orbit. The next normal loupe update takes over from there.
        loupeOrbitReturning = false;
        hasLoupeOrbitOffset = false;
        RestoreMainCameraLoupeOrbitPivot();
    }

    private void UpdateLoupeOrbitReturn()
    {
        if (!loupeOrbitReturning)
            return;

        float blend = GetCameraBlend();
        loupeOrbitYaw = Mathf.Lerp(loupeOrbitYaw, 0f, blend);
        loupeOrbitPitch = Mathf.Lerp(loupeOrbitPitch, 0f, blend);

        if (Mathf.Abs(loupeOrbitYaw) > 0.05f || Mathf.Abs(loupeOrbitPitch) > 0.05f)
            return;

        loupeOrbitYaw = 0f;
        loupeOrbitPitch = 0f;
        hasLoupeOrbitOffset = false;
        loupeOrbitReturning = false;
    }

    private void EndLoupeOrbit(bool restoreCursorPosition)
    {
        StopLoupeOrbitDrag(restoreCursorPosition);
        RestoreMainCameraLoupeOrbitPivot();
        hasLoupeOrbitOffset = false;
        loupeOrbitReturning = false;
        useLoupeOrbitMousePositionThisFrame = false;
        pendingLoupeOrbitInput = Vector2.zero;
        loupeOrbitYaw = 0f;
        loupeOrbitPitch = 0f;
    }

    private void BeginMainCameraLoupeOrbitPivot()
    {
        if (!changeLookAt || !mainCameraUsesLoupeHitPivot)
            return;

        CameraController cameraController = GetActiveCameraController();
        if (cameraController == null)
            return;

        if (mainCameraLoupeOrbitPivot == null)
        {
            GameObject pivotObject = new GameObject("MainCameraLoupeOrbitPivot");
            pivotObject.hideFlags = HideFlags.HideInHierarchy;
            mainCameraLoupeOrbitPivot = pivotObject.transform;
        }

        mainCameraLoupeOrbitPivot.position = loupeOrbitPivot;
        mainCameraLoupeOrbitController = cameraController;

        Vector2 compositionScreenPosition = Vector2.zero;
        if (Camera.main != null)
        {
            Vector2 lensCenterScreen = loupeOrbitMousePosition + lensCenterOffset;
            Vector3 viewportPosition = Camera.main.ScreenToViewportPoint(lensCenterScreen);
            compositionScreenPosition = new Vector2(
                viewportPosition.x - 0.5f,
                viewportPosition.y - 0.5f);
        }

        mainCameraLoupeOrbitController.BeginLoupeOrbitPivot(
            mainCameraLoupeOrbitPivot,
            compositionScreenPosition);
    }

    private void RestoreMainCameraLoupeOrbitPivot()
    {
        if (mainCameraLoupeOrbitController != null)
            mainCameraLoupeOrbitController.RestoreLoupeOrbitPivot();

        mainCameraLoupeOrbitController = null;
    }

    private bool IsLoupeHeld()
    {
        if (IsWatsonActive())
            return false;

        if (DialogueEditor.ConversationManager.Instance != null &&
            DialogueEditor.ConversationManager.Instance.IsConversationActive ||
            TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput)
            return false;

        // Dragging an IdeaPoint owns the F input until the line is released or cancelled.
        if (DetectiveIdeaManager.Instance != null && DetectiveIdeaManager.Instance.IsDraggingIdea())
            return false;

        if (loupeSuppressedUntilKeyRelease)
        {
            if (Input.GetKey(loupeKey))
                return false;

            loupeSuppressedUntilKeyRelease = false;
        }

        return EagleVisionSystem.Instance != null &&
               EagleVisionSystem.Instance.isActive &&
               (Input.GetKey(loupeKey) || debugLoupeToggleActive);
    }

    private void UpdateDebugLoupeToggle()
    {
        if (!enableDebugLoupeToggle)
        {
            debugLoupeToggleActive = false;
            return;
        }

        if (IsWatsonActive())
        {
            debugLoupeToggleActive = false;
            return;
        }

        if (Input.GetKeyDown(debugLoupeToggleKey))
            debugLoupeToggleActive = !debugLoupeToggleActive;

        if (debugLoupeToggleActive)
            EagleVisionSystem.Instance?.HoldVisionFor(0.2f);
    }

    private void SuppressLoupeUntilKeyReleased()
    {
        EndLoupeOrbit(true);
        debugLoupeToggleActive = false;
        loupeSuppressedUntilKeyRelease = true;
        hasInspectionHit = false;
        ResetLoupeAimTargetSmoothing();
        RestoreLoupeLookAt();
        SetInspectionCamerasActive(false);
        EagleVisionScanner.Instance?.SetTooltipParentForLoupe(false);
        HideLoupeFrame();
        HideCustomLoupeCursor();
        HideLoupe();
        SetSherlockHandLoupeVisible(false);
        UpdateRightHandLoupeRig(false, true);
    }

    private bool IsWatsonGripHeld()
    {
        return false;
    }

    private bool IsWatsonActive()
    {
        return SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1;
    }

    private bool ShouldShowWatsonEscortCursor()
    {
        return IsWatsonActive() &&
               EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive;
    }

    private void UpdateRightHandLoupeRig(bool useLoupeRig, bool immediate = false)
    {
        if (rightHandLoupeRig == null)
            return;

        float targetWeight = useLoupeRig && !IsWatsonActive() ? 1f : 0f;
        rightHandLoupeRig.weight = immediate
            ? targetWeight
            : Mathf.MoveTowards(
                rightHandLoupeRig.weight,
                targetWeight,
                Time.deltaTime / rightHandRigBlendDuration);

        if (rightHandTarget == null || rightHandRestTarget == null)
            return;

        if (targetWeight <= 0f)
        {
            rightHandTarget.position = rightHandRestTarget.position;
            rightHandTarget.rotation = rightHandRestTarget.rotation;
            rightHandTargetVelocity = Vector3.zero;
            return;
        }

        Transform sherlock = GetSherlockTransform();
        if (sherlock == null)
            return;

        Vector3 aimPosition = GetLoupeAimPosition();
        Vector3 toHit = aimPosition - sherlock.position;
        Vector3 flatToHit = Vector3.ProjectOnPlane(toHit, Vector3.up);
        if (flatToHit.sqrMagnitude < 0.0001f)
            flatToHit = sherlock.forward;

        float signedAngle = Vector3.SignedAngle(sherlock.forward, flatToHit.normalized, Vector3.up);
        float horizontalOffset = Mathf.Clamp(signedAngle / maxLoupeHandCursorAngle, -1f, 1f) *
                                 maxLoupeHandHorizontalOffset;
        float verticalOffset = Mathf.Clamp(toHit.y / 2f, -1f, 1f) * maxLoupeHandVerticalOffset;
        float horizontalDistance = flatToHit.magnitude;
        float nearDistance = Mathf.Min(loupeHandNearDistance, loupeHandFarDistance);
        float farDistance = Mathf.Max(loupeHandNearDistance, loupeHandFarDistance);
        float distanceProgress = Mathf.InverseLerp(nearDistance, farDistance, horizontalDistance);
        float forwardOffset = Mathf.Lerp(
            loupeHandNearForwardOffset,
            loupeHandFarForwardOffset,
            distanceProgress);

        Vector3 extendDirection = rightHandRestTarget.position - sherlock.position;
        if (extendDirection.sqrMagnitude < 0.0001f)
            extendDirection = sherlock.forward;
        else
            extendDirection.Normalize();

        Vector3 desiredPosition = rightHandRestTarget.position +
                                  sherlock.right * horizontalOffset +
                                  Vector3.up * verticalOffset +
                                  extendDirection * forwardOffset;

        rightHandTarget.position = immediate
            ? desiredPosition
            : Vector3.SmoothDamp(
                rightHandTarget.position,
                desiredPosition,
                ref rightHandTargetVelocity,
                loupeHandOffsetSmoothTime);

        if (!rotateRightHandTargetTowardAimTarget)
            return;

        Vector3 directionToAim = aimPosition - rightHandTarget.position;
        if (directionToAim.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(directionToAim.normalized, sherlock.up) *
                                    Quaternion.Euler(rightHandTargetAimRotationOffset);
        float rotationBlend = 1f - Mathf.Exp(-Time.deltaTime / rightHandTargetRotationSmoothTime);
        rightHandTarget.rotation = Quaternion.Slerp(rightHandTarget.rotation, targetRotation, rotationBlend);
    }

    private Transform GetSherlockTransform()
    {
        if (SwitchCharacter.Instance == null || SwitchCharacter.Instance.players == null)
            return null;

        foreach (PlayerInput playerInput in SwitchCharacter.Instance.players)
        {
            if (playerInput == null)
                continue;

            PlayerController playerController = playerInput.GetComponent<PlayerController>();
            if (playerController != null && playerController.playerCharacter == PlayerCharacter.Sherlock)
                return playerController.transform;
        }

        return null;
    }

    private void SetSherlockHandLoupeVisible(bool isVisible)
    {
        if (sherlockHandLoupe != null && sherlockHandLoupe.activeSelf != isVisible)
            sherlockHandLoupe.SetActive(isVisible);
    }

    private void UpdateSherlockLoupeAudio(bool isLoupeActive)
    {
        if (isLoupeActive == wasSherlockLoupeActive)
            return;

        AudioClip transitionClip = isLoupeActive ? loupeEnterAudio : loupeExitAudio;
        if (loupeAudioSource != null && transitionClip != null)
            loupeAudioSource.PlayOneShot(transitionClip);

        wasSherlockLoupeActive = isLoupeActive;
    }

    private void TryShowFirstSherlockLoupeTutorial(bool loupeHeld)
    {
        if (!enableFirstUseTutorialPopup || firstUseTutorialShown || wasLoupeHeld || !loupeHeld)
            return;

        if (SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex != 0)
            return;

        if (TutorialTimeline.Instance == null)
            return;

        firstUseTutorialShown = true;
        TutorialTimeline.Instance.ShowGameplayTutorialPopup(
            firstUseTutorialTitle,
            firstUseTutorialText,
            firstUseTutorialVideoClip);
    }

    private void ConfigureMaterial()
    {
        if (magnifierMaterial == null)
            return;

        if (hiddenCluesTexture != null)
            magnifierMaterial.SetTexture("_HiddenCluesTex", hiddenCluesTexture);

        if (loupeSceneTexture != null)
            magnifierMaterial.SetTexture("_SceneTex", loupeSceneTexture);

        magnifierMaterial.SetVector("_RenderCenter", new Vector4(0.5f, 0.5f, 0f, 0f));
    }

    private void ApplyLoupeVisualScale()
    {
        if (loupeFrame == null)
            return;

        loupeFrame.localScale = loupeFrameBaseScale * loupeVisualScale;
    }

    private void UpdateMaterial(Vector2 lensCenterScreen)
    {
        Vector2 lensCenterUv = new Vector2(
            lensCenterScreen.x / Screen.width,
            lensCenterScreen.y / Screen.height
        );

        magnifierMaterial.SetVector("_Center", new Vector4(lensCenterUv.x, lensCenterUv.y, 0f, 0f));
        magnifierMaterial.SetVector("_RenderCenter", new Vector4(0.5f, 0.5f, 0f, 0f));
        magnifierMaterial.SetFloat("_Radius", GetLoupeRadiusNormalized());
        magnifierMaterial.SetFloat("_Zoom", zoom);
        magnifierMaterial.SetFloat("_DesaturateScene", desaturateScene);
        magnifierMaterial.SetFloat("_HiddenCluesStrength", hiddenCluesStrength);
    }

    private void UpdateInspectionCameras(Vector2 lensCenterScreen)
    {
        Camera mainCamera = Camera.main;
        if (!useSurfaceInspection || mainCamera == null)
        {
            hasInspectionHit = false;
            ResetLoupeAimTargetSmoothing();
            RestoreLoupeLookAt();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(lensCenterScreen);
        lastRayOrigin = ray.origin;

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, inspectionLayers, inspectionTriggerInteraction))
        {
            hasInspectionHit = false;
            ResetLoupeAimTargetSmoothing();
            RestoreLoupeLookAt();
            ApplyInspectionPose(mainCamera.transform.position, mainCamera.transform.rotation, GetCameraBlend());
            return;
        }


        Vector3 mainCameraPosition = mainCamera.transform.position;
        Vector3 directionToViewer = mainCameraPosition - hit.point;

        if (directionToViewer.sqrMagnitude < 0.001f)
        {
            hasInspectionHit = false;
            ResetLoupeAimTargetSmoothing();
            RestoreLoupeLookAt();
            return;
        }

        directionToViewer.Normalize();

        Vector3 lensDirection = hasLoupeOrbitOffset
            ? ApplyLoupeOrbit(directionToViewer)
            : directionToViewer;
        Vector3 lensCameraPosition = hit.point + lensDirection * lensDistanceFromHit;
        Vector3 directionToHit = (hit.point - lensCameraPosition).normalized;
        Vector3 normalDirection = -hit.normal.normalized;

        if (Vector3.Dot(normalDirection, directionToHit) < 0f)
            normalDirection = -normalDirection;

        Vector3 limitedNormalDirection = Vector3.RotateTowards(
            directionToHit,
            normalDirection,
            maxNormalTiltDegrees * Mathf.Deg2Rad,
            0f
        );
        Vector3 viewDirection = hasLoupeOrbitOffset
            ? directionToHit
            : Vector3.Slerp(directionToHit, limitedNormalDirection, normalInfluence).normalized;
        Vector3 up = Vector3.ProjectOnPlane(mainCamera.transform.up, viewDirection);
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.ProjectOnPlane(mainCamera.transform.right, viewDirection);

        Quaternion lensCameraRotation = Quaternion.LookRotation(viewDirection, up.normalized);
        Vector3 hybridLookPoint = lensCameraPosition + viewDirection * Vector3.Distance(lensCameraPosition, hit.point);
        ApplyInspectionPose(lensCameraPosition, lensCameraRotation, GetCameraBlend());

        hasInspectionHit = true;
        lastInspectionHit = hit;
        UpdateLoupeAimTarget(hit.point + loupeAimTargetOffset);

        UpdateLoupeLookAt(GetLoupeAimPosition());
        lastHitPoint = hit.point;
        lastHitNormal = hit.normal;
        lastMainCameraPosition = mainCameraPosition;
        lastLensCameraPosition = lensCameraPosition;
        lastHybridLookPoint = hybridLookPoint;
    }

    private void UpdateLoupeAimTarget(Vector3 rawAimPosition)
    {
        if (loupeAimTarget == null)
            return;

        if (resetLoupeAimTargetOnNextHit)
        {
            loupeAimTarget.position = rawAimPosition;
            loupeAimTargetVelocity = Vector3.zero;
            resetLoupeAimTargetOnNextHit = false;
            return;
        }

        loupeAimTarget.position = Vector3.SmoothDamp(
            loupeAimTarget.position,
            rawAimPosition,
            ref loupeAimTargetVelocity,
            loupeAimTargetSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
    }

    private void ResetLoupeAimTargetSmoothing()
    {
        loupeAimTargetVelocity = Vector3.zero;
        resetLoupeAimTargetOnNextHit = true;
    }

    private float GetCameraBlend()
    {
        return 1f - Mathf.Exp(-cameraFollowSpeed * Time.unscaledDeltaTime);
    }

    private void ApplyInspectionPose(Vector3 position, Quaternion rotation, float blend)
    {
        MoveInspectionCamera(hiddenCluesCamera, position, rotation, blend);
        MoveInspectionCamera(loupeSceneCamera, position, rotation, blend);
    }

    private void MoveInspectionCamera(Camera inspectionCamera, Vector3 position, Quaternion rotation, float blend)
    {
        if (inspectionCamera == null)
            return;

        Transform cameraTransform = inspectionCamera.transform;
        cameraTransform.SetPositionAndRotation(
            Vector3.Lerp(cameraTransform.position, position, blend),
            Quaternion.Slerp(cameraTransform.rotation, rotation, blend)
        );
    }

    private void UpdateLoupeLookAt(Vector3 hitPoint)
    {
        if (!changeLookAt)
            return;

        CameraController cameraController = GetActiveCameraController();
        if (cameraController == null)
            return;

        if (loupeLookAtCameraController != cameraController)
        {
            RestoreLoupeLookAt();
            loupeLookAtCameraController = cameraController;
        }

        if (loupeLookAtTarget == null)
        {
            GameObject targetObject = new GameObject("LoupeLookAtTarget");
            loupeLookAtTarget = targetObject.transform;
        }

        if (resetLoupeLookAtOnNextUse)
        {
            loupeLookAtTarget.position = cameraController.GetLookAtPosition();
            loupeLookAtVelocity = Vector3.zero;
            resetLoupeLookAtOnNextUse = false;
        }

        Vector3 desiredLookAt = Vector3.Lerp(
            cameraController.GetFollowPosition(),
            hitPoint,
            loupeLookAtRaycastInfluence);

        loupeLookAtTarget.position = Vector3.SmoothDamp(
            loupeLookAtTarget.position,
            desiredLookAt,
            ref loupeLookAtVelocity,
            loupeLookAtSmoothTime,
            Mathf.Infinity,
            Time.unscaledDeltaTime);
        loupeLookAtCameraController.OverrideLookAtTarget(loupeLookAtTarget);
    }

    private CameraController GetActiveCameraController()
    {
        if (SwitchCharacter.Instance == null)
            return null;

        int activeIndex = SwitchCharacter.Instance.activePlayerIndex;
        var cameras = SwitchCharacter.Instance.playersCamera;
        if (cameras == null || activeIndex < 0 || activeIndex >= cameras.Length || cameras[activeIndex] == null)
            return null;

        return cameras[activeIndex].GetComponent<CameraController>();
    }

    private void RestoreLoupeLookAt()
    {
        if (loupeLookAtCameraController != null)
        {
            Transform sherlock = GetSherlockTransform();
            if (sherlock != null)
            {
                // The loupe is Sherlock-only, so its exit should always return
                // to the gameplay target instead of reviving an old camera focus.
                loupeLookAtCameraController.ForceLookAtTarget(sherlock);
            }
            else
            {
                loupeLookAtCameraController.RestoreLookAtTarget();
            }
        }

        loupeLookAtCameraController = null;
        loupeLookAtVelocity = Vector3.zero;
        resetLoupeLookAtOnNextUse = true;
    }

    private void DetachInspectionCamera(Camera inspectionCamera)
    {
        if (inspectionCamera != null)
            inspectionCamera.transform.SetParent(null, true);
    }
    private void SetInspectionCamerasActive(bool active)
    {
        if (hiddenCluesCamera != null)
            hiddenCluesCamera.enabled = active;

        if (loupeSceneCamera != null)
            loupeSceneCamera.enabled = active;
    }


    private float GetLoupeRadiusNormalized()
    {
        loupeFrame.GetWorldCorners(corners);

        float widthOnScreen = Vector3.Distance(corners[0], corners[3]);
        float heightOnScreen = Vector3.Distance(corners[0], corners[1]);
        float diameter = Mathf.Min(widthOnScreen, heightOnScreen);

        return diameter * lensRadiusMultiplier / Mathf.Min(Screen.width, Screen.height);
    }

    private void ShowLoupe()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }

        if (hideCursor)
            Cursor.visible = false;

        HideWatsonGripCursor();
    }

    private void HideLoupeFrame()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }

        HideCustomLoupeCursor();
        Cursor.visible = false;
    }

    private void HideLoupe()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.blocksRaycasts = false;
            rootCanvasGroup.interactable = false;
        }

        Cursor.visible = true;
        CustomCursorBridge.RestoreDefault();
        HideCustomLoupeCursor();
        HideWatsonGripCursor();
        HideWatsonEscortCursors();
    }

    private void UpdateCustomLoupeCursor()
    {
        if (loupeCursorFlare == null)
            return;

        if (loupeCursorFlareCanvasGroup == null)
            loupeCursorFlareCanvasGroup = loupeCursorFlare.GetComponent<CanvasGroup>();

        loupeCursorFlare.gameObject.SetActive(true);
        loupeCursorFlare.anchoredPosition = loupeCursorFlareOffset;

        float pulse = (Mathf.Sin(Time.unscaledTime * loupeCursorPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float scale = 1f + (pulse - 0.5f) * 2f * loupeCursorScalePulse;
        loupeCursorFlare.localScale = loupeCursorFlareBaseScale * scale;

        if (loupeCursorFlareCanvasGroup != null)
            loupeCursorFlareCanvasGroup.alpha = Mathf.Lerp(loupeCursorMinAlpha, loupeCursorMaxAlpha, pulse);
    }

    private void HideCustomLoupeCursor()
    {
        if (loupeCursorFlare == null)
            return;

        loupeCursorFlare.gameObject.SetActive(false);
    }

    private void UpdateWatsonGripCursor()
    {
        if (watsonGripCursor == null)
            return;

        if (Mouse.current == null)
            return;

        if (watsonGripCursorCanvasGroup == null)
            watsonGripCursorCanvasGroup = watsonGripCursor.GetComponent<CanvasGroup>();

        watsonGripCursor.gameObject.SetActive(true);
        watsonGripCursor.position = Mouse.current.position.ReadValue() + watsonGripCursorOffset;
        watsonGripCursor.localScale = watsonGripCursorBaseScale;

        if (watsonGripCursorCanvasGroup != null)
            watsonGripCursorCanvasGroup.alpha = 1f;
    }

    private void HideWatsonGripCursor()
    {
        if (watsonGripCursor != null)
            watsonGripCursor.gameObject.SetActive(false);
    }

    private void UpdateWatsonInteractionCursor()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        Texture2D cursorTexture = null;
        Vector2 hotSpot = Vector2.zero;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide))
        {
            WatsonEscortNPC escortNpc = hit.collider.GetComponentInParent<WatsonEscortNPC>();
            WatsonCarryable carryable = hit.collider.GetComponentInParent<WatsonCarryable>();

            if (escortNpc != null)
            {
                cursorTexture = watsonEscortCursorTexture;
                hotSpot = watsonEscortCursorHotSpot;
            }
            else if (carryable != null)
            {
                cursorTexture = watsonCarryCursorTexture;
                hotSpot = watsonCarryCursorHotSpot;
            }
        }

        Cursor.visible = true;
        CustomCursorBridge.SetOverride(cursorTexture, hotSpot);
    }

    private void HideWatsonEscortCursors()
    {
        if (watsonEscortMoveCursor != null)
            watsonEscortMoveCursor.gameObject.SetActive(false);

        if (watsonEscortNpcCursor != null)
            watsonEscortNpcCursor.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (!drawInspectionGizmos || !hasInspectionHit)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(lastRayOrigin, lastHitPoint);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(lastHitPoint, gizmoSphereRadius);

        Gizmos.color = Color.gray;
        Gizmos.DrawRay(lastHitPoint, lastHitNormal * gizmoSphereRadius * 4f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(lastHitPoint, lastMainCameraPosition);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastLensCameraPosition, gizmoSphereRadius * 1.4f);
        Gizmos.DrawLine(lastMainCameraPosition, lastLensCameraPosition);
        Gizmos.DrawLine(lastLensCameraPosition, lastHitPoint);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(lastLensCameraPosition, lastHybridLookPoint);
        Gizmos.DrawWireSphere(lastHybridLookPoint, gizmoSphereRadius * 0.7f);
    }

    private void OnDisable()
    {
        EndLoupeOrbit(true);
        debugLoupeToggleActive = false;
        wasSherlockLoupeActive = false;
        SetSherlockHandLoupeVisible(false);
        UpdateRightHandLoupeRig(false, true);
        RestoreLoupeLookAt();

        if (loupeLookAtTarget != null)
            Destroy(loupeLookAtTarget.gameObject);

        if (mainCameraLoupeOrbitPivot != null)
            Destroy(mainCameraLoupeOrbitPivot.gameObject);

        if (instance == this)
            instance = null;

        EagleVisionScanner.Instance?.SetTooltipParentForLoupe(false);
        Cursor.visible = true;
        HideWatsonGripCursor();
    }
}
