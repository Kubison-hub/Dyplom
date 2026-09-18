using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_ClockKey : Lvl3ClockworkInteraction
{
    [SerializeField] private Int_lv3_Keyhole keyhole;
    [Tooltip("Leave empty to hide this key object after it is collected.")]
    [SerializeField] private GameObject objectToHide;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Pickup Audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupAudioClip;

    [Header("Pickup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] pickupDialogueLines;
    [FormerlySerializedAs("sherlockPickupText")]
    [SerializeField, HideInInspector, TextArea] private string legacySherlockPickupText = "Hmm, odłożone wahadło od zegara.";
    [FormerlySerializedAs("sherlockPickupAudio")]
    [SerializeField, HideInInspector] private AudioClip legacySherlockPickupAudio;
    [TextArea]
    [SerializeField] private string noSpaceText = "Nie mam miejsca, aby to podnieść.";

    [Header("Next Interactions")]
    [Tooltip("Whole GameObjects enabled after collecting the key.")]
    public GameObject[] nextInteractionGameObjects;

    private bool collected;

    protected override void Awake()
    {
        base.Awake();
        EnsurePickupDialogueLines();
        SetupInteractionType();
    }

    private void Reset() => SetupInteractionType();
    private void OnValidate()
    {
        EnsurePickupDialogueLines();
        SetupInteractionType();
    }

    public override bool CanPlayerUse(PlayerController player)
    {
        return player != null &&
               (player.playerCharacter == PlayerCharacter.Sherlock || player.CompareTag("PlayerA"));
    }

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (collected)
            return;

        if (!HasInventorySpace())
        {
            ShowNoSpaceText(player);
            return;
        }

        collected = true;
        GameObject target = objectToHide != null ? objectToHide : gameObject;
        SetPickupPresentationVisible(target, false);

        if (InventoryManager.Instance == null ||
            !InventoryManager.Instance.TryAddItem(ItemType.Wahadlo, inventoryIcon))
        {
            SetPickupPresentationVisible(target, true);
            collected = false;
            ShowNoSpaceText(player);
            return;
        }

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);

        if (Interactable != null)
        {
            Interactable.isInteractableActive = false;
            Interactable.MarkCompleted();
        }

        foreach (GameObject nextInteraction in nextInteractionGameObjects)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }

        StartCoroutine(PlayDialogueAfterPickupAudio(player));
    }

    private IEnumerator PlayDialogueAfterPickupAudio(PlayerController player)
    {
        if (pickupAudioSource != null && pickupAudioClip != null)
        {
            float pitch = Mathf.Abs(pickupAudioSource.pitch);
            float playbackDuration = pitch > 0.01f
                ? pickupAudioClip.length / pitch
                : pickupAudioClip.length;

            if (playbackDuration > 0f)
                yield return new WaitForSecondsRealtime(playbackDuration);
        }

        PlayDialogue(player, pickupDialogueLines);
    }

    private void SetupInteractionType()
    {
        SetupInteractable();

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_lv3_ClockKey);
    }

    protected override void OnDialogueSequenceCompleted(Lvl3DialogueLine[] lines)
    {
        if (!collected || lines != pickupDialogueLines)
            return;

        GameObject target = objectToHide != null ? objectToHide : gameObject;
        target.SetActive(false);
    }

    private static void SetPickupPresentationVisible(GameObject target, bool visible)
    {
        if (target == null)
            return;

        foreach (Renderer targetRenderer in target.GetComponentsInChildren<Renderer>(true))
        {
            if (targetRenderer != null)
                targetRenderer.enabled = visible;
        }

        foreach (Collider targetCollider in target.GetComponentsInChildren<Collider>(true))
        {
            if (targetCollider != null)
                targetCollider.enabled = visible;
        }
    }

    private static bool HasInventorySpace()
    {
        InventoryManager inventory = InventoryManager.Instance;
        return inventory != null &&
               (inventory.items.Contains(ItemType.Wahadlo) || inventory.items.Count < inventory.maxSlots);
    }

    private void EnsurePickupDialogueLines()
    {
        if (pickupDialogueLines != null && pickupDialogueLines.Length > 0)
            return;

        pickupDialogueLines = new[]
        {
            new Lvl3DialogueLine
            {
                speaker = Lvl3DialogueSpeaker.Sherlock,
                text = legacySherlockPickupText,
                voiceClip = legacySherlockPickupAudio,
                duration = 3f
            }
        };
    }

    private void ShowNoSpaceText(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (isWatson)
            PlayerTopText.Instance?.ShowWatsonTopText(noSpaceText);
        else
            PlayerTopText.Instance?.ShowTopText(noSpaceText, string.Empty);
    }
}
