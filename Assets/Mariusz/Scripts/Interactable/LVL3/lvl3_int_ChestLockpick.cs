using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_ChestLockpick : Lvl3InteractionDialogueBase, IInteractionApproachGate
{
    private Interactable interactable;

    public bool performed = false;

    [Header("Inspection Dialogue")]
    [FormerlySerializedAs("oldChestText")]
    [SerializeField, TextArea] private string sherlockOldChestText = "Stara skrzynia.";
    [SerializeField] private AudioSource sherlockVoiceAudioSource;
    [SerializeField] private AudioClip sherlockOldChestAudio;
    [SerializeField, TextArea] private string watsonOldChestText = "Stara skrzynia. Ciekawe, co skrywa w środku.";
    [SerializeField] private AudioSource watsonVoiceAudioSource;
    [SerializeField] private AudioClip watsonOldChestAudio;

    [Header("Watson Locked Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] watsonLockedDialogue;
    [FormerlySerializedAs("watsonLockedText")]
    [SerializeField, HideInInspector, TextArea] private string legacyWatsonLockedText = "Zamknięta.";
    [FormerlySerializedAs("watsonLockedAudio")]
    [SerializeField, HideInInspector] private AudioClip legacyWatsonLockedAudio;

    [Header("Interaction Access")]
    [Tooltip("Optional override. If empty, the Interactable Point is checked.")]
    [SerializeField] private Transform accessCheckPoint;
    [SerializeField, Min(0.05f)] private float accessCheckRadius = 0.5f;
    [Tooltip("Extra layers treated as physical blockers. WatsonCarryable and active NavMeshObstacle objects are detected automatically.")]
    [SerializeField] private LayerMask extraBlockingLayers;

    [Header("No Access Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] noAccessDialogue;
    [FormerlySerializedAs("noAccessText")]
    [SerializeField, HideInInspector, TextArea] private string legacyNoAccessText = "Nie ma dojścia.";
    [FormerlySerializedAs("noAccessAudio")]
    [SerializeField, HideInInspector] private AudioClip legacyNoAccessAudio;

    [Header("Chest")]
    [SerializeField] private Transform lid;
    [SerializeField] private Vector3 openEuler = new Vector3(-80f, 0f, 0f);
    [SerializeField] private float openSpeed = 120f;
    [SerializeField] private GameObject lampInside;
    [SerializeField] private bool hideLampOnStart = true;
    [SerializeField] private AudioSource openAudio;

    [Header("Lockpick")]
    [SerializeField] private LockPickMinigameController minigamePrefab;
    [SerializeField] private Transform minigameTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LockPickAudioController audioController;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private int lockFieldCount = 5;
    [SerializeField] private int sequenceLength = 4;

    private LockPickMinigameController currentMinigame;
    private Coroutine openCoroutine;
    private Coroutine blockedPlayerRotationCoroutine;
    private bool isOpen = false;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => noAccessDialogue;

    private void Reset()
    {
        SetupInteractable();
    }

    private void OnValidate()
    {
        EnsureNoAccessDialogue();
        EnsureWatsonLockedDialogue();
        SetupInteractable();
    }

    private void Start()
    {
        EnsureNoAccessDialogue();
        EnsureWatsonLockedDialogue();
        SetupInteractable();

        if (lampInside != null && hideLampOnStart)
        {
            lampInside.SetActive(false);
        }
    }

    public void PerformInteraction(PlayerController player)
    {
        if (isOpen || currentMinigame != null)
        {
            player.currentInteractable = null;
            return;
        }

        if (!performed)
        {
            performed = true;
            string inspectionText = IsWatson(player) ? watsonOldChestText : sherlockOldChestText;
            ShowCharacterDialogue(player, inspectionText, sherlockOldChestAudio, watsonOldChestAudio);
            player.currentInteractable = null;
            return;
        }

        if (IsWatson(player))
        {
            PlayDialogue(player, watsonLockedDialogue);
            player.currentInteractable = null;
            return;
        }

        player.currentInteractable = null;

        if (interactable != null && interactable.questionVFX != null)
        {
            interactable.questionVFX.Stop();
        }

        if (minigamePrefab == null)
        {
            Debug.LogError($"{name}: Lockpick minigame prefab is missing.");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError($"{name}: Player camera is missing.");
            return;
        }

        if (cameraController != null)
            cameraController.SetZoomPreset("Narrow");

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.isLockpicking = true;
        }

        Transform spawnTransform = minigameTransform != null ? minigameTransform : transform;

        currentMinigame = Instantiate(
            minigamePrefab,
            spawnTransform.position,
            spawnTransform.rotation
        );

        currentMinigame.Open(
            playerCamera,
            audioController,
            HandleUnlocked,
            HandleClosed,
            lockFieldCount,
            sequenceLength
        );
    }

    public bool CanPlayerUse(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Sherlock ||
                player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerA") ||
                player.CompareTag("PlayerB"));
    }

    public bool CanApproachInteraction(PlayerController player)
    {
        Transform targetPoint = GetAccessCheckPoint();
        if (targetPoint == null)
            return true;

        if (player == null || player.navMeshAgent == null || !player.navMeshAgent.isOnNavMesh)
            return false;

        NavMeshPath path = new NavMeshPath();
        if (!player.navMeshAgent.CalculatePath(targetPoint.position, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        foreach (Collider candidate in Physics.OverlapSphere(
                     targetPoint.position,
                     accessCheckRadius,
                     ~0,
                     QueryTriggerInteraction.Ignore))
        {
            if (IsBlockingCollider(candidate, player))
                return false;
        }

        return true;
    }

    public void ShowApproachBlockedText(PlayerController player)
    {
        if (IsWatson(player))
            return;

        if (blockedPlayerRotationCoroutine != null)
            StopCoroutine(blockedPlayerRotationCoroutine);

        blockedPlayerRotationCoroutine = StartCoroutine(PlayBlockedApproachSequence(player));
    }

    private IEnumerator PlayBlockedApproachSequence(PlayerController player)
    {
        if (player == null)
        {
            blockedPlayerRotationCoroutine = null;
            yield break;
        }

        NavMeshAgent agent = player.navMeshAgent;
        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.updateRotation = false;
        }

        Vector3 direction = transform.position - player.transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            float rotationSpeed = Mathf.Max(0.01f, player.interactionPointRotationSpeed);

            while (Quaternion.Angle(player.transform.rotation, targetRotation) > 1f)
            {
                player.transform.rotation = Quaternion.RotateTowards(
                    player.transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
                yield return null;
            }

            player.transform.rotation = targetRotation;
        }

        if (agent != null)
            agent.updateRotation = true;

        PlayDialogue(player, noAccessDialogue);
        interactable?.TriggerCompanionInteractionFocus(player);
        blockedPlayerRotationCoroutine = null;
    }

    private void ShowCharacterDialogue(
        PlayerController player,
        string text,
        AudioClip sherlockAudio,
        AudioClip watsonAudio)
    {
        if (IsWatson(player))
        {
            PlayerTopText.Instance?.ShowWatsonTopText(text);
            if (watsonVoiceAudioSource != null && watsonAudio != null)
                watsonVoiceAudioSource.PlayOneShot(watsonAudio);
            return;
        }

        PlayerTopText.Instance?.ShowTopText(text, string.Empty);
        if (sherlockVoiceAudioSource != null && sherlockAudio != null)
            sherlockVoiceAudioSource.PlayOneShot(sherlockAudio);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
    }

    private void EnsureWatsonLockedDialogue()
    {
        if (watsonLockedDialogue != null && watsonLockedDialogue.Length > 0)
            return;

        watsonLockedDialogue = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Watson,
                text = legacyWatsonLockedText,
                voiceClip = legacyWatsonLockedAudio,
                duration = 2f
            }
        };
    }

    private Transform GetAccessCheckPoint()
    {
        if (accessCheckPoint != null)
            return accessCheckPoint;

        if (interactable == null)
            interactable = GetComponent<Interactable>();

        return interactable != null ? interactable.interactabePoint : null;
    }

    private bool IsBlockingCollider(Collider candidate, PlayerController player)
    {
        if (candidate == null || candidate.transform.IsChildOf(transform))
            return false;

        if (candidate.GetComponentInParent<PlayerController>() != null)
            return false;

        if (extraBlockingLayers.value != 0 &&
            (extraBlockingLayers.value & (1 << candidate.gameObject.layer)) != 0)
        {
            return true;
        }

        NavMeshObstacle obstacle = candidate.GetComponentInParent<NavMeshObstacle>();
        if (obstacle != null && obstacle.enabled)
            return true;

        return candidate.GetComponentInParent<WatsonCarryable>() != null;
    }

    private void HandleUnlocked()
    {
        if (audioController != null)
        {
            audioController.PlayUnlock();
        }

        CloseLockpickView();

        if (!isOpen && openCoroutine == null)
        {
            openCoroutine = StartCoroutine(OpenChest());
        }
    }

    private void HandleClosed()
    {
        if (audioController != null)
        {
            audioController.PlayReset();
        }

        CloseLockpickView();
    }

    private void CloseLockpickView()
    {
        if (cameraController != null)
        {
            cameraController.ReturnToPreviousZoomState();
        }

        if (ClueManager.Instance != null)
        {
            ClueManager.Instance.isLockpicking = false;
        }

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
        }
    }

    private IEnumerator OpenChest()
    {
        isOpen = true;
        interactable?.MarkCompleted();

        if (interactable != null)
        {
            interactable.isInteractableActive = false;
            interactable.interactiveShader = null;
        }

        Collider[] colliders = GetComponents<Collider>();
        foreach (Collider chestCollider in colliders)
        {
            chestCollider.enabled = false;
        }

        if (lampInside != null)
            lampInside.SetActive(true);

        if (openAudio != null)
        {
            openAudio.Play();
        }

        if (lid != null)
        {
            Quaternion openRotation = Quaternion.Euler(openEuler);

            while (Quaternion.Angle(lid.localRotation, openRotation) > 0.5f)
            {
                lid.localRotation = Quaternion.RotateTowards(
                    lid.localRotation,
                    openRotation,
                    openSpeed * Time.deltaTime
                );

                yield return null;
            }

            lid.localRotation = openRotation;
        }
        else
        {
            Debug.LogWarning($"{name}: Chest lid is missing.");
        }

        openCoroutine = null;
    }

    private void EnsureNoAccessDialogue()
    {
        if (noAccessDialogue != null && noAccessDialogue.Length > 0)
            return;

        noAccessDialogue = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = legacyNoAccessText,
                voiceClip = legacyNoAccessAudio,
                duration = 3f
            }
        };
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_ChestLockpick);
        }
    }
}
