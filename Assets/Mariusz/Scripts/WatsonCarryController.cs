using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(NavMeshAgent))]
public class WatsonCarryController : MonoBehaviour
{
    private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");
    private static readonly int PickupHash = Animator.StringToHash("Pickup");
    private static readonly int DropHash = Animator.StringToHash("Drop");

    public static WatsonCarryController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform carryContainer;
    [SerializeField] private Animator watsonAnimator;

    [Header("Watson Carry Audio")]
    [Tooltip("Optional source for Watson's pickup and drop sounds.")]
    [SerializeField] private AudioSource watsonCarryAudioSource;
    [SerializeField] private AudioClip watsonPickupAudioClip;
    [SerializeField] private AudioClip watsonDropAudioClip;

    [Header("Watson Carry Rig")]
    [SerializeField] private Rig pickupRig;
    [Tooltip("Persistent target already assigned to Watson's left-hand IK constraint.")]
    [SerializeField] private Transform leftHandTarget;
    [Tooltip("Persistent target already assigned to Watson's right-hand IK constraint.")]
    [SerializeField] private Transform rightHandTarget;
    [SerializeField, Min(0.01f)] private float pickupRigBlendDuration = 0.2f;

    [Header("Lamp Restriction")]
    [Tooltip("Watson's LampHolder used to detect when his hands are occupied by the lamp.")]
    [SerializeField] private Transform watsonLampHolder;
    [SerializeField] private GameObject heldLamp;

    [Header("Dropping")]
    [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 1f;
    [SerializeField, Min(0f)] private float dropGroundOffset = 0.02f;
    [SerializeField, Min(0.01f)] private float arrivalTolerance = 0.08f;

    private PlayerController playerController;
    private NavMeshAgent navMeshAgent;
    private WatsonCarryable carriedObject;
    private float carryStartedAt;
    private float defaultSpeed;
    private float defaultAngularSpeed;
    private bool shouldDropAtDestination;
    private Vector3 requestedDropPosition;
    private bool hasIsCarryingParameter;
    private bool hasPickupParameter;
    private bool hasDropParameter;
    private Transform defaultLeftHandTargetParent;
    private Transform defaultRightHandTargetParent;
    private Vector3 defaultLeftHandTargetLocalPosition;
    private Vector3 defaultRightHandTargetLocalPosition;
    private Quaternion defaultLeftHandTargetLocalRotation;
    private Quaternion defaultRightHandTargetLocalRotation;
    private Coroutine pickupRigBlendCoroutine;
    private Transform activeCarryLeftHandTarget;
    private Transform activeCarryRightHandTarget;

    public bool IsCarrying => carriedObject != null;
    public bool IsHoldingLamp => HasActiveHeldLamp();

    private void Awake()
    {
        Instance = this;
        playerController = GetComponent<PlayerController>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        if (watsonAnimator == null)
            watsonAnimator = GetComponent<Animator>();
        if (watsonCarryAudioSource == null)
            watsonCarryAudioSource = GetComponent<AudioSource>();

        hasIsCarryingParameter = HasAnimatorParameter(IsCarryingHash, AnimatorControllerParameterType.Bool);
        hasPickupParameter = HasAnimatorParameter(PickupHash, AnimatorControllerParameterType.Trigger);
        hasDropParameter = HasAnimatorParameter(DropHash, AnimatorControllerParameterType.Trigger);
        CacheDefaultCarryRigTargetTransforms();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private bool HasActiveHeldLamp()
    {
        if (watsonLampHolder == null)
            return false;

        foreach (Transform child in watsonLampHolder)
        {
            if (child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void Update()
    {
        if (carriedObject == null)
            return;

        if (carriedObject.CarryTimeoutSeconds > 0f &&
            Time.time >= carryStartedAt + carriedObject.CarryTimeoutSeconds)
        {
            DropAtCurrentPosition();
            return;
        }

        if (shouldDropAtDestination && HasReachedDestination())
            DropAt(requestedDropPosition);
    }

    private void LateUpdate()
    {
        if (carriedObject != null)
            UpdateCarryHandTargets();
    }

    public void TryPickUp(WatsonCarryable carryable)
    {
        if (carryable == null ||
            carryable.IsCarried ||
            carriedObject != null ||
            IsHoldingLamp ||
            !carryable.CanBePickedUp())
            return;

        if (carryContainer == null)
        {
            Debug.LogWarning($"{name}: Assign Carry Container before using Watson carryables.", this);
            return;
        }

        defaultSpeed = navMeshAgent.speed;
        defaultAngularSpeed = navMeshAgent.angularSpeed;
        navMeshAgent.speed = defaultSpeed * carryable.MovementSpeedMultiplier;
        navMeshAgent.angularSpeed = defaultAngularSpeed * carryable.RotationSpeedMultiplier;
        navMeshAgent.ResetPath();

        carriedObject = carryable;
        carriedObject.BeginCarry(carryContainer);
        ActivateCarryRig(carriedObject);
        carryStartedAt = Time.time;
        shouldDropAtDestination = false;
        SetCarryingAnimation(true, hasPickupParameter);
        PlayWatsonCarryAudio(watsonPickupAudioClip);
    }

    public bool TryHandleGroundClick(PlayerController player, Vector3 groundPoint)
    {
        if (carriedObject == null || player != playerController || !navMeshAgent.isOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(groundPoint, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            return true;

        if (WatsonCarryable.IsWorldPositionBlocked(hit.position, navMeshAgent.radius))
            return true;

        requestedDropPosition = hit.position;
        shouldDropAtDestination = true;
        playerController.MoveToPoint(hit.position);
        return true;
    }

    private void DropAtCurrentPosition()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.ResetPath();
            DropAt(navMeshAgent.nextPosition);
            return;
        }

        DropAt(transform.position);
    }

    private void DropAt(Vector3 position)
    {
        if (carriedObject == null)
            return;

        // The clicked point is Watson's destination. The item itself is placed slightly ahead of him.
        position = carriedObject.GetDropWorldPosition(transform);

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            position = hit.position;

        position += Vector3.up * dropGroundOffset;
        carriedObject.EndCarry(position, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
        carriedObject = null;
        DeactivateCarryRig();
        shouldDropAtDestination = false;
        navMeshAgent.speed = defaultSpeed;
        navMeshAgent.angularSpeed = defaultAngularSpeed;
        SetCarryingAnimation(false, hasDropParameter);
        PlayWatsonCarryAudio(watsonDropAudioClip);
    }

    private bool HasReachedDestination()
    {
        if (navMeshAgent.pathPending)
            return false;

        if (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + arrivalTolerance)
            return false;

        return !navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.01f;
    }

    private void SetCarryingAnimation(bool isCarrying, bool fireTrigger)
    {
        if (watsonAnimator == null)
            return;

        if (hasIsCarryingParameter)
            watsonAnimator.SetBool(IsCarryingHash, isCarrying);

        if (fireTrigger)
            watsonAnimator.SetTrigger(isCarrying ? PickupHash : DropHash);
    }

    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType type)
    {
        if (watsonAnimator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in watsonAnimator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == type)
                return true;
        }

        return false;
    }

    private void PlayWatsonCarryAudio(AudioClip clip)
    {
        if (watsonCarryAudioSource != null && clip != null)
            watsonCarryAudioSource.PlayOneShot(clip);
    }

    private void CacheDefaultCarryRigTargetTransforms()
    {
        if (leftHandTarget != null)
        {
            defaultLeftHandTargetParent = leftHandTarget.parent;
            defaultLeftHandTargetLocalPosition = leftHandTarget.localPosition;
            defaultLeftHandTargetLocalRotation = leftHandTarget.localRotation;
        }

        if (rightHandTarget != null)
        {
            defaultRightHandTargetParent = rightHandTarget.parent;
            defaultRightHandTargetLocalPosition = rightHandTarget.localPosition;
            defaultRightHandTargetLocalRotation = rightHandTarget.localRotation;
        }
    }

    private void ActivateCarryRig(WatsonCarryable carryable)
    {
        if (carryable == null)
            return;

        activeCarryLeftHandTarget = carryable.LeftHandTarget;
        activeCarryRightHandTarget = carryable.RightHandTarget;
        UpdateCarryHandTargets();

        BlendCarryRigTo(1f);
    }

    private void DeactivateCarryRig()
    {
        activeCarryLeftHandTarget = null;
        activeCarryRightHandTarget = null;

        if (pickupRig == null)
        {
            RestoreDefaultCarryRigTargets();
            return;
        }

        if (pickupRigBlendCoroutine != null)
            StopCoroutine(pickupRigBlendCoroutine);

        pickupRigBlendCoroutine = StartCoroutine(BlendCarryRigOut());
    }

    private void BlendCarryRigTo(float targetWeight)
    {
        if (pickupRig == null)
            return;

        if (pickupRigBlendCoroutine != null)
            StopCoroutine(pickupRigBlendCoroutine);

        pickupRigBlendCoroutine = StartCoroutine(BlendCarryRig(targetWeight));
    }

    private IEnumerator BlendCarryRigOut()
    {
        yield return BlendCarryRig(0f);
        RestoreDefaultCarryRigTargets();
    }

    private IEnumerator BlendCarryRig(float targetWeight)
    {
        if (pickupRig == null)
            yield break;

        float startWeight = pickupRig.weight;
        float elapsed = 0f;
        while (elapsed < pickupRigBlendDuration)
        {
            elapsed += Time.deltaTime;
            pickupRig.weight = Mathf.Lerp(startWeight, targetWeight, elapsed / pickupRigBlendDuration);
            yield return null;
        }

        pickupRig.weight = targetWeight;
        pickupRigBlendCoroutine = null;
    }

    private void RestoreDefaultCarryRigTargets()
    {
        RestoreHandTarget(
            leftHandTarget,
            defaultLeftHandTargetParent,
            defaultLeftHandTargetLocalPosition,
            defaultLeftHandTargetLocalRotation);
        RestoreHandTarget(
            rightHandTarget,
            defaultRightHandTargetParent,
            defaultRightHandTargetLocalPosition,
            defaultRightHandTargetLocalRotation);
    }

    private void UpdateCarryHandTargets()
    {
        CopyTargetPose(leftHandTarget, activeCarryLeftHandTarget);
        CopyTargetPose(rightHandTarget, activeCarryRightHandTarget);
    }

    private static void CopyTargetPose(Transform handTarget, Transform carryableTarget)
    {
        if (handTarget != null && carryableTarget != null)
            handTarget.SetPositionAndRotation(carryableTarget.position, carryableTarget.rotation);
    }

    private static void RestoreHandTarget(
        Transform handTarget,
        Transform originalParent,
        Vector3 originalLocalPosition,
        Quaternion originalLocalRotation)
    {
        if (handTarget == null)
            return;

        handTarget.SetParent(originalParent, false);
        handTarget.localPosition = originalLocalPosition;
        handTarget.localRotation = originalLocalRotation;
    }
}
