using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WatsonCarryable : MonoBehaviour
{
    [Header("Carry Root")]
    [Tooltip("Assign the highest root that contains the visible object, colliders and NavMeshObstacle. Leave empty when this component already sits on that root.")]
    [SerializeField] private Transform carryRoot;

    [Header("NavMesh Blocking")]
    [Tooltip("Creates a carved NavMeshObstacle from the carry root colliders, so this object can block routes after it is put down.")]
    [SerializeField] private bool blockNavMeshWhenDropped = true;
    [SerializeField, Min(0.01f)] private float navMeshBlockerPadding = 0.03f;

    [Header("Carry Placement")]
    [SerializeField] private Vector3 carryLocalPosition = new Vector3(0f, 0.9f, 0.45f);
    [SerializeField] private Vector3 carryLocalEulerAngles;

    [Header("Drop Placement")]
    [Tooltip("Local offset from Watson when the object is put down. Positive Z places it in front of him.")]
    [SerializeField] private Vector3 dropLocalOffset = new Vector3(0f, 0f, 0.75f);

    [Header("Watson Grip")]
    [InspectorName("Require Watson Eagle Vision")]
    [Tooltip("Watson może podnieść ten obiekt tylko podczas aktywnego Eagle Vision.")]
    [SerializeField] private bool requireWatsonGrip = true;

    [Header("Watson Eagle Vision Visual")]
    [Tooltip("Shows this object while Watson is using Eagle Vision.")]
    [SerializeField] private bool showInWatsonEagleVision = true;

    [Header("Watson Hand Targets")]
    [Tooltip("Targets used by Watson's carry rig while he holds this object.")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Watson Movement While Carrying")]
    [SerializeField, Min(0.05f)] private float movementSpeedMultiplier = 0.65f;
    [SerializeField, Min(0.05f)] private float rotationSpeedMultiplier = 0.7f;

    [Header("Carry Time")]
    [Tooltip("0 means that Watson may carry this object without a time limit.")]
    [SerializeField, Min(0f)] private float carryTimeoutSeconds = 20f;

    [Header("Object Audio")]
    [Tooltip("Audio emitted by this particular movable object.")]
    [SerializeField] private AudioSource objectAudioSource;
    [SerializeField] private AudioClip pickupAudioClip;
    [SerializeField] private AudioClip dropAudioClip;

    [Header("Pickup Light Requirement")]
    [SerializeField] private bool pickupWhenLightOn;
    [Tooltip("When the requirement is enabled, at least one assigned and enabled light must be on.")]
    [SerializeField] private Light[] requiredLights;
    [SerializeField, TextArea] private string lightRequirementFailureText =
        "W tych ciemnościach nie uda mi się tego zrobić.";
    [SerializeField, TextArea] private string heldLampFailureText =
        "Nie dam rady tego przenieść trzymając lampę.";

    private Interactable interactable;
    private Collider[] colliders;
    private bool[] colliderStates;
    private NavMeshObstacle[] navMeshObstacles;
    private bool[] obstacleStates;
    private Rigidbody[] rigidbodies;
    private bool[] rigidbodyKinematicStates;
    private Transform originalParent;
    private Vector3 originalLocalScale;
    private bool visionShaderVisible;
    private NavMeshObstacle generatedNavMeshObstacle;

    public Vector3 CarryLocalPosition => carryLocalPosition;
    public Quaternion CarryLocalRotation => Quaternion.Euler(carryLocalEulerAngles);
    public float MovementSpeedMultiplier => movementSpeedMultiplier;
    public float RotationSpeedMultiplier => rotationSpeedMultiplier;
    public float CarryTimeoutSeconds => carryTimeoutSeconds;
    public bool IsCarried { get; private set; }
    public string LightRequirementFailureText => lightRequirementFailureText;
    public string HeldLampFailureText => heldLampFailureText;
    public bool RequiresWatsonGrip => requireWatsonGrip;
    public Transform LeftHandTarget => leftHandTarget;
    public Transform RightHandTarget => rightHandTarget;
    public bool IsBlockedByHeldLamp => WatsonCarryController.Instance != null &&
                                       WatsonCarryController.Instance.IsHoldingLamp;

    public static bool IsWorldPositionBlocked(Vector3 worldPosition, float radius = 0.1f)
    {
        foreach (WatsonCarryable carryable in FindObjectsByType<WatsonCarryable>(FindObjectsSortMode.None))
        {
            if (carryable == null || carryable.IsCarried || !carryable.gameObject.activeInHierarchy)
                continue;

            foreach (Collider blocker in carryable.CarryRoot.GetComponentsInChildren<Collider>(true))
            {
                if (blocker == null || !blocker.enabled || blocker.isTrigger)
                    continue;

                Vector3 closestPoint = blocker.ClosestPoint(worldPosition);
                closestPoint.y = worldPosition.y;
                if ((closestPoint - worldPosition).sqrMagnitude <= radius * radius)
                    return true;
            }
        }

        return false;
    }

    public Vector3 GetDropWorldPosition(Transform watsonTransform)
    {
        return watsonTransform != null
            ? watsonTransform.TransformPoint(dropLocalOffset)
            : transform.position;
    }

    private Transform CarryRoot => carryRoot != null ? carryRoot : transform;

    public bool CanBePickedUp()
    {
        if (!pickupWhenLightOn)
            return true;

        if (requiredLights == null || requiredLights.Length == 0)
            return false;

        foreach (Light requiredLight in requiredLights)
        {
            if (requiredLight != null && requiredLight.isActiveAndEnabled && requiredLight.intensity > 0f)
                return true;
        }

        return false;
    }

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (objectAudioSource == null)
            objectAudioSource = GetComponent<AudioSource>();

        EnsureNavMeshBlocker();
        CachePhysicsState();
    }

    private void Update()
    {
        bool shouldShowVisionShader = showInWatsonEagleVision && !IsCarried &&
                                      interactable != null && interactable.isInteractableActive &&
                                      EagleVisionSystem.Instance != null && EagleVisionSystem.Instance.isActive &&
                                      SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1 &&
                                      !IsWorldInteractionBlocked();

        if (visionShaderVisible == shouldShowVisionShader)
            return;

        visionShaderVisible = shouldShowVisionShader;
        SetWatsonVisionVisible(visionShaderVisible);
    }

    private void SetWatsonVisionVisible(bool visible)
    {
        WatsonEscortController controller = WatsonEscortController.Instance;
        float visibility = controller != null ? controller.EagleVisionShaderVisibility : 1f;
        float pulseSpeed = controller != null ? controller.EagleVisionShaderPulseSpeed : 1.5f;
        Color color = controller != null ? controller.EagleVisionShaderColor : Color.green;
        Color hoverColor = controller != null ? controller.EagleVisionShaderHoverColor : Color.cyan;
        float colorTransitionSpeed = controller != null
            ? controller.EagleVisionShaderColorTransitionSpeed
            : 8f;
        float fresnelPower = controller != null ? controller.EagleVisionShaderFresnelPower : 2f;
        interactable?.SetInteractionShaderForcedVisible(
            visible,
            visibility,
            pulseSpeed,
            color,
            hoverColor,
            colorTransitionSpeed,
            fresnelPower);
    }

    private static bool IsWorldInteractionBlocked()
    {
        return DialogueEditor.ConversationManager.Instance != null &&
               DialogueEditor.ConversationManager.Instance.IsConversationActive ||
               NotebookManager.Instance != null && NotebookManager.Instance.IsNotebookOpen ||
               TutorialManager.Instance != null && TutorialManager.Instance.BlocksWorldInput ||
               TutorialTimeline.Instance != null && TutorialTimeline.Instance.BlocksWorldInput;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player == null || player.playerCharacter != PlayerCharacter.Watson)
            return;

        if (IsBlockedByHeldLamp)
        {
            PlayerTopText.Instance?.ShowWatsonTopText(heldLampFailureText);
            return;
        }

        if (!CanBePickedUp())
            return;

        WatsonCarryController controller = player.GetComponent<WatsonCarryController>();
        if (controller == null)
        {
            Debug.LogWarning($"{name}: WatsonCarryController is missing on Watson.", this);
            return;
        }

        controller.TryPickUp(this);
    }

    public void BeginCarry(Transform carryContainer)
    {
        if (carryContainer == null)
            return;

        CachePhysicsState();
        Transform objectRoot = CarryRoot;
        originalParent = objectRoot.parent;
        originalLocalScale = objectRoot.localScale;
        IsCarried = true;

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            visionShaderVisible = false;
            interactable.SetInteractionShaderHover(false);
            SetWatsonVisionVisible(false);
            interactable.SetQuestionFXRate(0f);
        }

        SetPhysicsEnabled(false);
        objectRoot.SetParent(carryContainer, true);
        objectRoot.localPosition = carryLocalPosition;
        objectRoot.localRotation = CarryLocalRotation;
        PlayObjectAudio(pickupAudioClip);
    }

    public void EndCarry(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!IsCarried)
            return;

        Transform objectRoot = CarryRoot;
        objectRoot.SetParent(originalParent, false);
        objectRoot.localScale = originalLocalScale;
        objectRoot.SetPositionAndRotation(worldPosition, worldRotation);
        SetPhysicsEnabled(true);

        if (interactable != null)
            interactable.isInteractableActive = true;

        IsCarried = false;
        PlayObjectAudio(dropAudioClip);
    }

    private void OnDisable()
    {
        if (visionShaderVisible)
            SetWatsonVisionVisible(false);

        visionShaderVisible = false;

    }

    private void CachePhysicsState()
    {
        Transform objectRoot = CarryRoot;

        colliders = objectRoot.GetComponentsInChildren<Collider>(true);
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
            colliderStates[i] = colliders[i] != null && colliders[i].enabled;

        navMeshObstacles = objectRoot.GetComponentsInChildren<NavMeshObstacle>(true);
        obstacleStates = new bool[navMeshObstacles.Length];
        for (int i = 0; i < navMeshObstacles.Length; i++)
            obstacleStates[i] = navMeshObstacles[i] != null && navMeshObstacles[i].enabled;

        rigidbodies = objectRoot.GetComponentsInChildren<Rigidbody>(true);
        rigidbodyKinematicStates = new bool[rigidbodies.Length];
        for (int i = 0; i < rigidbodies.Length; i++)
            rigidbodyKinematicStates[i] = rigidbodies[i] != null && rigidbodies[i].isKinematic;
    }

    private void EnsureNavMeshBlocker()
    {
        if (!blockNavMeshWhenDropped)
            return;

        Transform objectRoot = CarryRoot;
        NavMeshObstacle[] existingObstacles = objectRoot.GetComponentsInChildren<NavMeshObstacle>(true);
        if (existingObstacles.Length > 0)
        {
            foreach (NavMeshObstacle obstacle in existingObstacles)
            {
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }

            return;
        }

        Bounds bounds = GetColliderBounds(objectRoot);
        if (bounds.size.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning($"{name}: WatsonCarryable needs at least one Collider to create a NavMesh blocker.", this);
            return;
        }

        generatedNavMeshObstacle = objectRoot.gameObject.AddComponent<NavMeshObstacle>();
        generatedNavMeshObstacle.shape = NavMeshObstacleShape.Box;
        generatedNavMeshObstacle.center = objectRoot.InverseTransformPoint(bounds.center);

        Vector3 scale = objectRoot.lossyScale;
        generatedNavMeshObstacle.size = new Vector3(
            bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
            bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z))) + Vector3.one * navMeshBlockerPadding;
        generatedNavMeshObstacle.carving = true;
        generatedNavMeshObstacle.carveOnlyStationary = false;
    }

    private static Bounds GetColliderBounds(Transform root)
    {
        Collider[] rootColliders = root.GetComponentsInChildren<Collider>(true);
        Bounds bounds = new Bounds(root.position, Vector3.zero);
        bool hasBounds = false;

        foreach (Collider rootCollider in rootColliders)
        {
            if (rootCollider == null || rootCollider.isTrigger)
                continue;

            if (!hasBounds)
            {
                bounds = rootCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(rootCollider.bounds);
            }
        }

        return hasBounds ? bounds : new Bounds(root.position, Vector3.zero);
    }

    private void SetPhysicsEnabled(bool enabled)
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = enabled && colliderStates[i];
        }

        for (int i = 0; i < navMeshObstacles.Length; i++)
        {
            if (navMeshObstacles[i] != null)
                navMeshObstacles[i].enabled = enabled && obstacleStates[i];
        }

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] != null)
                rigidbodies[i].isKinematic = !enabled || rigidbodyKinematicStates[i];
        }
    }

    private void PlayObjectAudio(AudioClip clip)
    {
        if (objectAudioSource != null && clip != null)
            objectAudioSource.PlayOneShot(clip);
    }

}
