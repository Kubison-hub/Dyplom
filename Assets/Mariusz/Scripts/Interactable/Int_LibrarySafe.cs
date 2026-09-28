using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibrarySafe : Lvl3InteractionDialogueBase
{
    private Interactable interactable;
    private Collider interactionCollider;

    [Header("Code Lock")]
    [SerializeField] private SafeCodeDrumMinigame codeDrumPrefab;
    [SerializeField] private Transform minigameTransform;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LockPickAudioController audioController;
    [SerializeField] private string code = "1A4";
    [SerializeField, Min(0.1f)] private float minigameExitRange = 3f;
    [SerializeField] private Vector3 minigameExitOffset;

    [Header("Safe Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] missingKeyDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Ten sejf wymaga właściwego klucza.",
            duration = 3f
        }
    };
    [SerializeField] private Lvl3DialogueLine[] keyAcceptedDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Klucz pasuje. Teraz został tylko szyfr.",
            duration = 3f
        }
    };

    [Header("Opened Safe")]
    [SerializeField] private Animator safeAnimator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private AudioSource safeOpenAudioSource;
    [SerializeField] private AudioClip safeOpenAudioClip;
    [SerializeField] private GameObject contents;
    [SerializeField] private int_LibraryPainting libraryPainting;

    private SafeCodeDrumMinigame currentMinigame;
    private PlayerController interactingPlayer;
    private bool hasKey;
    private bool keyInserted;
    private bool isOpen;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => keyAcceptedDialogue;

    private void Update()
    {
        if (currentMinigame == null || interactingPlayer == null)
            return;

        if (Vector3.Distance(interactingPlayer.transform.position, GetMinigameExitPosition()) > minigameExitRange)
            currentMinigame.Close();
    }

    private void Start()
    {
        interactable = GetComponent<Interactable>();
        interactionCollider = GetComponent<Collider>();

        if (contents != null)
            contents.SetActive(false);
    }

    public void SetKeyFound(bool found)
    {
        hasKey = found;
    }

    public void PerformInteraction(PlayerController player)
    {
        if (player != null)
            player.currentInteractable = null;

        if (isOpen || currentMinigame != null)
            return;

        bool keyInInventory = InventoryManager.Instance != null &&
                              InventoryManager.Instance.items.Contains(ItemType.LibraryKey);
        if (!keyInserted && !hasKey && !keyInInventory)
        {
            PlayDialogue(player, missingKeyDialogue);
            return;
        }

        if (codeDrumPrefab == null)
        {
            Debug.LogError($"{name}: SafeCodeDrumMinigame prefab is missing.");
            return;
        }

        if (!keyInserted)
        {
            if (InventoryManager.Instance == null ||
                !InventoryManager.Instance.TryRemoveItem(ItemType.LibraryKey))
            {
                PlayDialogue(player, missingKeyDialogue);
                return;
            }

            keyInserted = true;
            hasKey = false;
        }

        PlayDialogue(player, keyAcceptedDialogue);

        if (ClueManager.Instance != null)
            ClueManager.Instance.isLockpicking = true;

        interactingPlayer = player;

        Transform spawnTransform = minigameTransform != null ? minigameTransform : transform;
        currentMinigame = Instantiate(codeDrumPrefab, spawnTransform.position, spawnTransform.rotation);
        currentMinigame.Open(playerCamera, audioController, code, HandleUnlocked, HandleClosed);
    }

    private void HandleUnlocked()
    {
        if (audioController != null)
            audioController.PlayUnlock();

        CloseMinigame();
        isOpen = true;
        GetComponent<Interactable>()?.MarkCompleted();

        if (safeAnimator != null && !string.IsNullOrWhiteSpace(openTrigger))
            safeAnimator.SetTrigger(openTrigger);

        if (safeOpenAudioSource != null && safeOpenAudioClip != null)
            safeOpenAudioSource.PlayOneShot(safeOpenAudioClip);

        if (contents != null)
            contents.SetActive(true);

        libraryPainting?.DeactivatePaintingInteraction();

        interactable.interactiveShader = null;
        interactable.isInteractableActive = false;

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }

    private void HandleClosed()
    {
        if (audioController != null)
            audioController.PlayReset();

        CloseMinigame();
    }

    private void CloseMinigame()
    {
        if (ClueManager.Instance != null)
            ClueManager.Instance.isLockpicking = false;

        interactingPlayer = null;

        if (currentMinigame != null)
        {
            Destroy(currentMinigame.gameObject);
            currentMinigame = null;
        }
    }

    private Vector3 GetMinigameExitPosition()
    {
        Transform anchor = interactable != null && interactable.interactabePoint != null
            ? interactable.interactabePoint
            : transform;

        return anchor.TransformPoint(minigameExitOffset);
    }
}
