using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_ChestLockpick : MonoBehaviour
{
    private Interactable interactable;

    public bool performed = false;

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

        performed = true;
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
