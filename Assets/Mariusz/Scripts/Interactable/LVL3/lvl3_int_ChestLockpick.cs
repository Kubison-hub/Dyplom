using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_ChestLockpick : MonoBehaviour, IInteractionApproachGate
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
    [SerializeField, TextArea] private string watsonLockedText = "Zamknięta.";
    [SerializeField] private AudioClip watsonLockedAudio;

    [Header("Interaction Access")]
    [Tooltip("Optional override. If empty, the Interactable Point is checked.")]
    [SerializeField] private Transform accessCheckPoint;
    [SerializeField, Min(0.05f)] private float accessCheckRadius = 0.5f;
    [Tooltip("Extra layers treated as physical blockers. WatsonCarryable and active NavMeshObstacle objects are detected automatically.")]
    [SerializeField] private LayerMask extraBlockingLayers;
    [SerializeField, TextArea] private string noAccessText = "Nie ma dojścia.";
    [Tooltip("Played through Sherlock Voice Audio Source when No Access Text is shown.")]
    [SerializeField] private AudioClip noAccessAudio;

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
    private bool isOpen = false;

    private void Reset()
    {
        SetupInteractable();
    }

    private void OnValidate()
    {
        SetupInteractable();
    }

    private void Start()
    {
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
            ShowCharacterDialogue(player, watsonLockedText, null, watsonLockedAudio);
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
        {
            cameraController.SetZoomState(CameraZoomState.Narrow);
        }

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

        if (sherlockVoiceAudioSource != null && noAccessAudio != null)
            sherlockVoiceAudioSource.PlayOneShot(noAccessAudio);

        Interactable chestInteractable = GetComponent<Interactable>();
        if (player != null && chestInteractable != null)
        {
            player.RotateTowardsInteractableAndShowTopText(chestInteractable, noAccessText);
            return;
        }

        PlayerTopText.Instance?.ShowTopText(noAccessText, string.Empty);
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

        if (lampInside != null)
        {
            lampInside.SetActive(true);
        }

        openCoroutine = null;
    }

    private void SetupInteractable()
    {
        lvl3_LayerUtility.SetOutlinedObjectsLayer(gameObject);

        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            interactable.SetInteractionType(InteractionType.lvl3_int_ChestLockpick);
        }
    }
}
