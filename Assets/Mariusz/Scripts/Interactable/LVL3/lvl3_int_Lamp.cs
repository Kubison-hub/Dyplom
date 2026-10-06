using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class lvl3_int_Lamp : Lvl3InteractionDialogueBase
{
    private Interactable interactable;

    public bool performed = false;
    public GameObject HeldLamp => heldLamp;

    [Header("Held Lamp")]
    [Tooltip("Separate inactive GameObject containing the held lamp model and its Point Light.")]
    [SerializeField] private GameObject heldLamp;
    [Tooltip("Default LampHolder, usually a child on Sherlock.")]
    [SerializeField] private Transform lampHolder;
    [Tooltip("Optional LampHolder child on Watson.")]
    [SerializeField] private Transform watsonLampHolder;
    [SerializeField] private Vector3 heldLampLocalPosition;
    [SerializeField] private Vector3 heldLampLocalEulerAngles;
    [SerializeField, TextArea] private string alreadyHoldingLampText = "Nie mogę nieść dwóch lamp.";
    [SerializeField, TextArea] private string carryingObjectFailureText = "Nie mogę wziąć lampy, kiedy coś już niosę.";

    [Header("Lamp Audio")]
    [Tooltip("Use an AudioSource on the persistent held-lamp object or its holder, not on this pickup object.")]
    [SerializeField] private AudioSource lampAudioSource;
    [SerializeField] private AudioClip pickupLampAudio;
    [SerializeField] private AudioClip placeLampAudio;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => new[]
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "Lampa. Może się przydać w ciemności.",
            duration = 2.5f
        }
    };

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
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
        {
            player.currentInteractable = null;
            return;
        }

        if (IsWatson(player) &&
            WatsonCarryController.Instance != null &&
            WatsonCarryController.Instance.IsCarrying)
        {
            ShowTopTextForPlayer(player, carryingObjectFailureText);
            player.currentInteractable = null;
            return;
        }

        if (IsPlayerHoldingLamp(player))
        {
            ShowTopTextForPlayer(player, alreadyHoldingLampText);
            player.currentInteractable = null;
            return;
        }

        if (!EquipHeldLamp(player))
        {
            player.currentInteractable = null;
            return;
        }

        performed = true;

        if (lvl3_GameProgress.Instance != null)
        {
            lvl3_GameProgress.Instance.lampPickedUp = true;
        }
        else
        {
            Debug.LogWarning($"{name}: lvl3_GameProgress.Instance is null.");
        }

        PlayInteractionDialogue(player);

        PlayPickupAudio();

        player.currentInteractable = null;
        RestoreCollectedPickup();
    }

    public bool CanPlayerUse(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Sherlock ||
                player.playerCharacter == PlayerCharacter.Watson ||
                player.CompareTag("PlayerA") ||
                player.CompareTag("PlayerB"));
    }

    public void RestoreCollectedPickup()
    {
        performed = true;
        GetComponent<Interactable>()?.MarkCompleted();
        SaveLoadManager.Instance?.MarkCollected(gameObject);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private bool EquipHeldLamp(PlayerController player)
    {
        if (heldLamp == null)
            return false;

        if (heldLamp.transform.IsChildOf(transform))
        {
            Debug.LogWarning($"{name}: Held Lamp must be a separate object, not a child of the pickup object.", heldLamp);
            return false;
        }

        Transform targetLampHolder = GetLampHolder(player);
        if (targetLampHolder == null)
        {
            Debug.LogWarning($"{name}: Assign a Lamp Holder for the player who picks up the lamp.", this);
            return false;
        }

        heldLamp.transform.SetParent(targetLampHolder, false);
        heldLamp.transform.localPosition = heldLampLocalPosition;
        heldLamp.transform.localRotation = Quaternion.Euler(heldLampLocalEulerAngles);
        heldLamp.SetActive(true);
        return true;
    }

    private Transform GetLampHolder(PlayerController player)
    {
        return IsWatson(player) && watsonLampHolder != null ? watsonLampHolder : lampHolder;
    }

    public void PlayPickupAudio()
    {
        PlayLampAudio(pickupLampAudio);
    }

    public void PlayPlaceAudio()
    {
        PlayLampAudio(placeLampAudio);
    }

    private void PlayLampAudio(AudioClip clip)
    {
        if (lampAudioSource != null && clip != null)
            lampAudioSource.PlayOneShot(clip);
    }

    private bool IsPlayerHoldingLamp(PlayerController player)
    {
        Transform targetLampHolder = GetLampHolder(player);
        return HasActiveHeldLamp(targetLampHolder);
    }

    private static bool HasActiveHeldLamp(Transform lampHolder)
    {
        if (lampHolder == null)
            return false;

        foreach (Transform child in lampHolder)
        {
            if (child.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }

    private static void ShowTopTextForPlayer(PlayerController player, string text)
    {
        if (PlayerTopText.Instance == null)
            return;

        if (IsWatson(player))
            PlayerTopText.Instance.ShowWatsonTopText(text);
        else
            PlayerTopText.Instance.ShowTopText(text, string.Empty);
    }

    private static bool IsWatson(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));
    }

    private void SetupInteractable()
    {
        interactable = GetComponent<Interactable>();

        if (interactable != null)
        {
            SetupInteractable(InteractionType.lvl3_int_Lamp);
        }
    }
}
