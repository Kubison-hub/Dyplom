using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class MagnifierGlassController : MonoBehaviour
{
    private static MagnifierGlassController instance;

    public static bool IsScrollReservedForLoupe =>
        instance != null && instance.ShouldReserveScrollForLoupe;

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

    [Header("Custom Loupe Cursor")]
    [SerializeField] private RectTransform loupeCursorFlare;
    [SerializeField] private CanvasGroup loupeCursorFlareCanvasGroup;
    [SerializeField] private Vector2 loupeCursorFlareOffset = new Vector2(14f, -12f);
    [SerializeField, Min(0f)] private float loupeCursorPulseSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float loupeCursorMinAlpha = 0.55f;
    [SerializeField, Range(0f, 1f)] private float loupeCursorMaxAlpha = 0.9f;
    [SerializeField, Min(0f)] private float loupeCursorScalePulse = 0.08f;

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
    [SerializeField] private bool useLoupeLookAt = true;
    [SerializeField, Range(0f, 1f)] private float loupeLookAtRaycastInfluence = 0.12f;
    [SerializeField, Min(0.01f)] private float loupeLookAtSmoothTime = 1.2f;

    [Header("Experimental Lens Scroll")]
    [SerializeField] private bool enableLensDistanceScroll = true;
    [SerializeField, Min(0.00001f)] private float lensScrollSensitivity = 0.00025f;
    [SerializeField, Min(0.31f)] private float minLensDistanceFromHit = 0.31f;
    [SerializeField, Min(0.31f)] private float maxLensDistanceFromHit = 1.2f;
    [SerializeField] private bool enableLoupeSizeScroll = true;
    [SerializeField, Min(0.00001f)] private float loupeSizeScrollSensitivity = 0.001f;

    [Header("First Sherlock Loupe Tutorial")]
    [SerializeField] private bool enableFirstUseTutorialPopup;
    [SerializeField] private string firstUseTutorialTitle = "LUPA SHERLOCKA";
    [SerializeField, TextArea] private string firstUseTutorialText =
        "Sherlock moze uzywac swojej lupy, aby odkrywac szczegoly i tropy.";
    [SerializeField] private VideoClip firstUseTutorialVideoClip;

    [Header("Debug")]
    [SerializeField] private bool drawInspectionGizmos = true;
    [SerializeField] private float gizmoSphereRadius = 0.08f;

    private readonly Vector3[] corners = new Vector3[4];
    private Vector3 loupeFrameBaseScale;
    private Vector3 loupeCursorFlareBaseScale;
    private bool hasInspectionHit;
    private Vector3 lastRayOrigin;
    private Vector3 lastHitPoint;
    private Vector3 lastHitNormal;
    private Vector3 lastMainCameraPosition;
    private Vector3 lastLensCameraPosition;
    private Vector3 lastHybridLookPoint;
    private RaycastHit lastInspectionHit;
    private Transform loupeLookAtTarget;
    private CameraController loupeLookAtCameraController;
    private Vector3 loupeLookAtVelocity;
    private bool resetLoupeLookAtOnNextUse = true;
    private bool wasLoupeHeld;
    private bool firstUseTutorialShown;

    private bool ShouldReserveScrollForLoupe =>
        isActiveAndEnabled && IsLoupeHeld() &&
        (enableLensDistanceScroll || enableLoupeSizeScroll && Input.GetKey(KeyCode.LeftAlt));

    public bool TryGetActiveLoupeHit(out RaycastHit hit)
    {
        hit = lastInspectionHit;
        return IsLoupeHeld() && hasInspectionHit;
    }

    public bool IsLoupeActive => IsLoupeHeld();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        loupeFrameBaseScale = loupeFrame != null ? loupeFrame.localScale : Vector3.one;
        loupeCursorFlareBaseScale = loupeCursorFlare != null ? loupeCursorFlare.localScale : Vector3.one;
        ApplyLoupeVisualScale();
        DetachInspectionCamera(hiddenCluesCamera);
        DetachInspectionCamera(loupeSceneCamera);
        ConfigureMaterial();
        SetInspectionCamerasActive(false);
        HideCustomLoupeCursor();
        HideLoupe();
    }

    private void Update()
    {
        bool loupeHeld = IsLoupeHeld();
        TryShowFirstSherlockLoupeTutorial(loupeHeld);
        wasLoupeHeld = loupeHeld;
        SetInspectionCamerasActive(loupeHeld);

        if (!loupeHeld)
        {
            hasInspectionHit = false;
            RestoreLoupeLookAt();
            HideLoupe();
            return;
        }

        if (Mouse.current == null || magnifierMaterial == null || loupeFrame == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector2 lensCenterScreen = mousePosition + lensCenterOffset;

        ApplyLoupeVisualScale();
        loupeFrame.position = mousePosition + loupeFrameOffset;
        UpdateInspectionCameras(lensCenterScreen);
        HandleLensDistanceScroll();
        UpdateMaterial(lensCenterScreen);
        ShowLoupe();
        UpdateCustomLoupeCursor();
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

    private bool IsLoupeHeld()
    {
        return EagleVisionSystem.Instance != null &&
               EagleVisionSystem.Instance.isActive &&
               Input.GetKey(loupeKey);
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
            RestoreLoupeLookAt();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(lensCenterScreen);
        lastRayOrigin = ray.origin;

        if (!Physics.Raycast(ray, out RaycastHit hit, raycastDistance, inspectionLayers, inspectionTriggerInteraction))
        {
            hasInspectionHit = false;
            RestoreLoupeLookAt();
            ApplyInspectionPose(mainCamera.transform.position, mainCamera.transform.rotation, GetCameraBlend());
            return;
        }


        Vector3 mainCameraPosition = mainCamera.transform.position;
        Vector3 directionToViewer = mainCameraPosition - hit.point;

        if (directionToViewer.sqrMagnitude < 0.001f)
        {
            hasInspectionHit = false;
            RestoreLoupeLookAt();
            return;
        }

        directionToViewer.Normalize();

        Vector3 lensCameraPosition = hit.point + directionToViewer * lensDistanceFromHit;
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
        Vector3 viewDirection = Vector3.Slerp(directionToHit, limitedNormalDirection, normalInfluence).normalized;
        Vector3 up = Vector3.ProjectOnPlane(mainCamera.transform.up, viewDirection);
        if (up.sqrMagnitude < 0.001f)
            up = Vector3.ProjectOnPlane(mainCamera.transform.right, viewDirection);

        Quaternion lensCameraRotation = Quaternion.LookRotation(viewDirection, up.normalized);
        Vector3 hybridLookPoint = lensCameraPosition + viewDirection * Vector3.Distance(lensCameraPosition, hit.point);
        ApplyInspectionPose(lensCameraPosition, lensCameraRotation, GetCameraBlend());

        hasInspectionHit = true;
        lastInspectionHit = hit;
        UpdateLoupeLookAt(hit.point);
        lastHitPoint = hit.point;
        lastHitNormal = hit.normal;
        lastMainCameraPosition = mainCameraPosition;
        lastLensCameraPosition = lensCameraPosition;
        lastHybridLookPoint = hybridLookPoint;
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
        if (!useLoupeLookAt)
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
            loupeLookAtCameraController.RestoreLookAtTarget();

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
        HideCustomLoupeCursor();
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
        RestoreLoupeLookAt();

        if (loupeLookAtTarget != null)
            Destroy(loupeLookAtTarget.gameObject);

        if (instance == this)
            instance = null;

        Cursor.visible = true;
    }
}
