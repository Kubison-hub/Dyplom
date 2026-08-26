using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_lv3_Doll : Lvl3ClockworkInteraction
{
    [Header("Approach")]
    [Tooltip("Ignore Interactable Point and approach the doll from the closest reachable NavMesh position.")]
    [SerializeField] private bool useNearestNavMeshApproach = true;
    public bool UseNearestNavMeshApproach => useNearestNavMeshApproach;

    [Header("Pickup Object")]
    [Tooltip("Leave empty to hide this GameObject after the doll is collected.")]
    [SerializeField] private GameObject objectToHide;
    [SerializeField] private Sprite inventoryIcon;

    [Header("Pickup Audio")]
    [SerializeField] private AudioSource pickupAudioSource;
    [SerializeField] private AudioClip pickupAudioClip;

    [Header("Sherlock")]
    [SerializeField, TextArea] private string sherlockPickupText = "Mała lalka. Ethel musiała ją zgubić w pośpiechu.";
    [SerializeField] private AudioSource sherlockVoiceSource;
    [SerializeField] private AudioClip sherlockPickupAudio;

    [Header("Watson")]
    [SerializeField, TextArea] private string watsonPickupText = "Lalka małej Ethel. Powinniśmy ją zachować.";
    [SerializeField] private AudioSource watsonVoiceSource;
    [SerializeField] private AudioClip watsonPickupAudio;

    [Header("Inventory")]
    [SerializeField, TextArea] private string noSpaceText = "Nie mam miejsca, aby to podnieść.";

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

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(ItemType.Lalka, inventoryIcon))
        {
            ShowTopTextForPlayer(player, noSpaceText);
            return;
        }

        collected = true;
        PlayPickupDialogue(player);

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);

        GameObject target = objectToHide != null ? objectToHide : gameObject;
        target.SetActive(false);
    }

    private void SetupInteractionType()
    {
        SetupInteractable();

        if (Interactable != null)
            Interactable.SetInteractionType(InteractionType.Int_lv3_Doll);
    }

    private void PlayPickupDialogue(PlayerController player)
    {
        bool isWatson = player != null &&
                        (player.playerCharacter == PlayerCharacter.Watson || player.CompareTag("PlayerB"));

        if (isWatson)
        {
            ShowTopTextForPlayer(player, watsonPickupText);
            if (watsonVoiceSource != null && watsonPickupAudio != null)
                watsonVoiceSource.PlayOneShot(watsonPickupAudio);
            return;
        }

        ShowTopTextForPlayer(player, sherlockPickupText);
        if (sherlockVoiceSource != null && sherlockPickupAudio != null)
            sherlockVoiceSource.PlayOneShot(sherlockPickupAudio);
    }
}
