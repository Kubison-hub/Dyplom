using UnityEngine;

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
    [TextArea]
    [SerializeField] private string sherlockPickupText = "Hmm, odłożone wahadło od zegara.";
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioClip sherlockPickupAudio;
    [TextArea]
    [SerializeField] private string watsonPickupText = "Wahadło od zegara...";
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioClip watsonPickupAudio;
    [TextArea]
    [SerializeField] private string noSpaceText = "Nie mam miejsca, aby to podnieść.";

    [Header("Next Interactions")]
    [Tooltip("Whole GameObjects enabled after collecting the key.")]
    public GameObject[] nextInteractionGameObjects;

    private bool collected;

    protected override void Awake()
    {
        base.Awake();
        SetupInteractionType();
    }

    private void Reset() => SetupInteractionType();
    private void OnValidate() => SetupInteractionType();

    public override void PerformInteraction(PlayerController player)
    {
        ClearPlayerInteraction(player);

        if (collected)
            return;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(ItemType.Wahadlo, inventoryIcon))
        {
            ShowNoSpaceText(player);
            return;
        }

        collected = true;

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);
        ShowPickupDialogue(player);

        foreach (GameObject nextInteraction in nextInteractionGameObjects)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }

        GameObject target = objectToHide != null ? objectToHide : gameObject;
        target.SetActive(false);
    }

    private void SetupInteractionType()
    {
        SetupInteractable();

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_lv3_ClockKey);
    }

    private void ShowPickupDialogue(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (isWatson)
        {
            PlayerTopText.Instance?.ShowWatsonTopText(watsonPickupText);
            if (watsonVoiceSource != null && watsonPickupAudio != null)
                watsonVoiceSource.PlayOneShot(watsonPickupAudio);
            return;
        }

        PlayerTopText.Instance?.ShowTopText(sherlockPickupText, string.Empty);
        if (sherlockVoiceSource != null && sherlockPickupAudio != null)
            sherlockVoiceSource.PlayOneShot(sherlockPickupAudio);
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
