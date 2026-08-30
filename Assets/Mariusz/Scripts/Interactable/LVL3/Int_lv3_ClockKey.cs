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
    private Coroutine pickupDialogueCoroutine;

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

        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddItem(ItemType.Wahadlo, inventoryIcon))
        {
            ShowNoSpaceText(player);
            return;
        }

        collected = true;

        if (pickupAudioSource != null && pickupAudioClip != null)
            pickupAudioSource.PlayOneShot(pickupAudioClip);

        foreach (GameObject nextInteraction in nextInteractionGameObjects)
        {
            if (nextInteraction != null)
                nextInteraction.SetActive(true);
        }

        if (Interactable != null)
            Interactable.isInteractableActive = false;

        pickupDialogueCoroutine = StartCoroutine(PlayPickupDialogueThenHide());
    }

    private void SetupInteractionType()
    {
        SetupInteractable();

        Interactable interactable = GetComponent<Interactable>();
        if (interactable != null)
            interactable.SetInteractionType(InteractionType.Int_lv3_ClockKey);
    }

    private IEnumerator PlayPickupDialogueThenHide()
    {
        foreach (Lvl3DialogueLine line in pickupDialogueLines)
        {
            string sherlockText = line.speaker == Lvl3DialogueSpeaker.Sherlock ? line.text : string.Empty;
            string watsonText = line.speaker == Lvl3DialogueSpeaker.Watson ? line.text : string.Empty;
            string selmaText = line.speaker == Lvl3DialogueSpeaker.Selma ? line.text : string.Empty;
            string violetText = line.speaker == Lvl3DialogueSpeaker.Violet ? line.text : string.Empty;

            PlayerTopText.Instance?.ShowTopTextPersistent(sherlockText, watsonText);
            PlayerTopText.Instance?.ShowSelmaTopTextPersistent(selmaText);
            PlayerTopText.Instance?.ShowVioletTopTextPersistent(violetText);
            PlayDialogueVoice(line);

            float duration = line.duration > 0f
                ? line.duration
                : PlayerTopText.Instance != null ? PlayerTopText.Instance.textTime : 3f;
            yield return new WaitForSeconds(duration);

            PlayerTopText.Instance?.ClearTopTextIfMatches(sherlockText, watsonText);
            PlayerTopText.Instance?.ClearSelmaTopTextIfMatches(selmaText);
            PlayerTopText.Instance?.ClearVioletTopTextIfMatches(violetText);
        }

        pickupDialogueCoroutine = null;
        GameObject target = objectToHide != null ? objectToHide : gameObject;
        target.SetActive(false);
    }

    private static void PlayDialogueVoice(Lvl3DialogueLine line)
    {
        if (line.voiceClip == null)
            return;

        DialogueAudioRegistry registry = DialogueAudioRegistry.Instance;
        AudioSource source = line.speaker switch
        {
            Lvl3DialogueSpeaker.Sherlock => registry != null ? registry.SherlockVoiceSource : null,
            Lvl3DialogueSpeaker.Watson => registry != null ? registry.WatsonVoiceSource : null,
            Lvl3DialogueSpeaker.Selma => registry != null ? registry.SelmaVoiceSource : null,
            Lvl3DialogueSpeaker.Violet => registry != null ? registry.VioletVoiceSource : null,
            _ => null
        };

        if (source == null)
            return;

        source.Stop();
        source.PlayOneShot(line.voiceClip);
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
