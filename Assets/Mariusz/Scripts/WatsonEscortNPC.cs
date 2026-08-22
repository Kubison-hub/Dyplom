using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Interactable))]
public class WatsonEscortNPC : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.1f)] private float approachDistance = 1.1f;
    [SerializeField, Min(0.1f)] private float arrivalTolerance = 0.15f;
    [SerializeField, Min(0.1f)] private float rotationSpeed = 300f;
    [Tooltip("Visible model or rig that should rotate toward Watson. Leave empty to rotate this NPC root.")]
    [SerializeField] private Transform rotationTarget;

    [Header("Return to Root")]
    [Tooltip("NPC returns here after its farewell dialogue. Leave empty to keep the NPC at the escorted destination.")]
    [SerializeField] private Transform rootPosition;
    [SerializeField, Min(0.1f)] private float rootPositionSampleRadius = 1.5f;

    [Header("Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] approachDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] destinationDialogueLines;
    [SerializeField] private Lvl3DialogueLine[] farewellDialogueLines;
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioSource selmaVoiceSource;

    [Header("Vision Eye")]
    [SerializeField] private bool showInteractionShaderInWatsonVision = true;

    [Header("Placement Preview")]
    [Tooltip("Optional prefab shown in the center of the NavMesh ring while Watson chooses this NPC's destination. When empty, the Interactable shader object is cloned.")]
    [SerializeField] private GameObject placementPreviewPrefab;

    [Header("Influence Points")]
    [Tooltip("Only these points affect this NPC's final escort orientation. Leave empty to use the legacy global scene points when fallback is enabled.")]
    [SerializeField] private WatsonEscortInfluencePoint[] influencePoints;
    [SerializeField] private bool useGlobalInfluencePointsWhenListEmpty = true;

    private Interactable interactable;
    private bool visionShaderVisible;
    private Coroutine dialogueCoroutine;
    private Coroutine returnToRootCoroutine;
    private float defaultSpeed;

    public NavMeshAgent Agent => navMeshAgent;
    public bool IsReadyForEscort { get; private set; }
    public float MovementSpeed => navMeshAgent != null ? navMeshAgent.speed : 0f;
    public GameObject PlacementPreviewPrefab => placementPreviewPrefab != null
        ? placementPreviewPrefab
        : interactable != null ? interactable.interactiveShader : null;
    public WatsonEscortInfluencePoint[] InfluencePoints => influencePoints;
    public bool UseGlobalInfluencePointsWhenListEmpty => useGlobalInfluencePointsWhenListEmpty;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (rotationTarget == null)
            rotationTarget = transform;

        if (navMeshAgent != null)
            defaultSpeed = navMeshAgent.speed;
    }

    private void Update()
    {
        bool shouldShowShader = showInteractionShaderInWatsonVision &&
                                !IsReadyForEscort &&
                                MagnifierGlassController.IsWatsonGripActive &&
                                SwitchCharacter.Instance != null && SwitchCharacter.Instance.activePlayerIndex == 1;

        if (visionShaderVisible != shouldShowShader)
        {
            visionShaderVisible = shouldShowShader;
            interactable?.SetInteractionShaderForcedVisible(visionShaderVisible);
        }

        if (animator != null && HasAnimatorBool("IsWalking"))
            animator.SetBool("IsWalking", navMeshAgent != null && navMeshAgent.velocity.sqrMagnitude > 0.01f);
    }

    private void OnDisable()
    {
        interactable?.SetInteractionShaderForcedVisible(false);
        visionShaderVisible = false;
    }

    public bool CanBeEscortedBy(PlayerController player)
    {
        return player != null &&
               player.playerCharacter == PlayerCharacter.Watson &&
               MagnifierGlassController.IsWatsonGripActive;
    }

    public Vector3 GetWatsonApproachPoint(Vector3 watsonPosition)
    {
        Vector3 direction = watsonPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = -transform.forward;

        Vector3 desired = transform.position + direction.normalized * approachDistance;
        return NavMesh.SamplePosition(desired, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)
            ? hit.position
            : desired;
    }

    public bool MoveTo(Vector3 destination)
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh ||
            !NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            return false;

        navMeshAgent.updateRotation = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(hit.position);
        return true;
    }

    public bool HasReachedDestination()
    {
        return navMeshAgent != null && !navMeshAgent.pathPending &&
               navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + arrivalTolerance &&
               (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.01f);
    }

    public void StopMoving()
    {
        if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.ResetPath();
        }
    }

    public void SetEscortMovementSpeed(float speed)
    {
        if (navMeshAgent != null)
            navMeshAgent.speed = Mathf.Max(0.1f, speed);
    }

    public void RestoreMovementSpeed()
    {
        if (navMeshAgent != null)
            navMeshAgent.speed = defaultSpeed;
    }

    public void FaceTowards(Vector3 targetPosition, float blend = 1f)
    {
        Transform target = GetRotationTarget();
        Vector3 direction = targetPosition - target.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        target.rotation = Quaternion.RotateTowards(
            target.rotation,
            targetRotation,
            rotationSpeed * Mathf.Clamp01(blend) * Time.deltaTime);
    }

    public IEnumerator RotateTowards(Vector3 targetPosition, float amount = 1f)
    {
        Transform target = GetRotationTarget();
        Vector3 direction = targetPosition - target.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        yield return RotateToRotation(lookRotation, amount);
    }

    public IEnumerator RotateToRotation(Quaternion rotation, float amount = 1f)
    {
        Transform target = GetRotationTarget();
        Quaternion targetRotation = Quaternion.Slerp(target.rotation, rotation, Mathf.Clamp01(amount));
        if (navMeshAgent != null)
            navMeshAgent.updateRotation = false;

        while (Quaternion.Angle(target.rotation, targetRotation) > 0.5f)
        {
            target.rotation = Quaternion.RotateTowards(
                target.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
            yield return null;
        }

        target.rotation = targetRotation;
    }

    private Transform GetRotationTarget()
    {
        return rotationTarget != null ? rotationTarget : transform;
    }

    public void PlayApproachDialogue()
    {
        StopReturnToRoot();
        IsReadyForEscort = true;
        StartRandomDialogue(approachDialogueLines);
    }

    public void PlayDestinationDialogue()
    {
        StartRandomDialogue(destinationDialogueLines);
    }

    public void PlayFarewellDialogue()
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(PlayFarewellDialogueAndReturn());
    }

    private void StartRandomDialogue(Lvl3DialogueLine[] lines)
    {
        if (dialogueCoroutine != null)
            StopCoroutine(dialogueCoroutine);

        dialogueCoroutine = StartCoroutine(PlayRandomDialogueLine(lines));
    }

    public void EndEscort()
    {
        IsReadyForEscort = false;
        RestoreMovementSpeed();
    }

    private IEnumerator PlayFarewellDialogueAndReturn()
    {
        if (farewellDialogueLines != null && farewellDialogueLines.Length > 0)
        {
            Lvl3DialogueLine line = farewellDialogueLines[UnityEngine.Random.Range(0, farewellDialogueLines.Length)];
            yield return PlayDialogueLine(line);
        }

        dialogueCoroutine = null;
        ReturnToRootPosition();
    }

    private void ReturnToRootPosition()
    {
        StopReturnToRoot();

        if (rootPosition == null || navMeshAgent == null || !navMeshAgent.isOnNavMesh ||
            !NavMesh.SamplePosition(rootPosition.position, out NavMeshHit rootHit, rootPositionSampleRadius, NavMesh.AllAreas))
        {
            return;
        }

        returnToRootCoroutine = StartCoroutine(MoveToRootPosition(rootHit.position));
    }

    private IEnumerator MoveToRootPosition(Vector3 rootDestination)
    {
        navMeshAgent.updateRotation = true;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(rootDestination);

        while (!HasReachedDestination())
            yield return null;

        StopMoving();
        if (rootPosition != null)
            yield return RotateToRotation(rootPosition.rotation);

        returnToRootCoroutine = null;
    }

    private void StopReturnToRoot()
    {
        if (returnToRootCoroutine == null)
            return;

        StopCoroutine(returnToRootCoroutine);
        returnToRootCoroutine = null;
    }

    private IEnumerator PlayRandomDialogueLine(Lvl3DialogueLine[] lines)
    {
        if (lines == null || lines.Length == 0)
            yield break;

        Lvl3DialogueLine line = lines[UnityEngine.Random.Range(0, lines.Length)];
        yield return PlayDialogueLine(line);

        dialogueCoroutine = null;
    }

    private IEnumerator PlayDialogueLine(Lvl3DialogueLine line)
    {
        string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
        string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
        string selmaText = line.speaker == Lvl3DialogueSpeaker.Selma ? line.text : string.Empty;
        PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);
        PlayerTopText.Instance?.ShowSelmaTopTextPersistent(selmaText);

        AudioSource source = line.speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => sherlockVoiceSource,
            Lvl3DialogueSpeaker.Watson => watsonVoiceSource,
            Lvl3DialogueSpeaker.Selma => selmaVoiceSource,
            _ => null
        };
        if (source != null && line.voiceClip != null)
        {
            source.Stop();
            source.PlayOneShot(line.voiceClip);
        }

        yield return new WaitForSeconds(line.duration > 0f ? line.duration : 2f);
        PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
        PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(selmaText);
    }

    private bool HasAnimatorBool(string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
