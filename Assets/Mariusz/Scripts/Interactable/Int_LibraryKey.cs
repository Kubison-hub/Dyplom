using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Int_LibraryKey : Lvl3InteractionDialogueBase
{
    private Interactable interactable;

    [SerializeField] private Int_LibrarySafe librarySafe;
    [SerializeField] private Renderer keyRenderer;
    [SerializeField] private AudioSource pickupAudio;

    [Header("Key Pickup Dialogue")]
    [SerializeField] private Lvl3DialogueLine[] keyPickupDialogue =
    {
        new Lvl3DialogueLine
        {
            speaker = Lvl3DialogueSpeaker.Sherlock,
            text = "I jest kluczyk, jakie to proste...",
            duration = 3f
        }
    };

    public bool performed;

    protected override Lvl3DialogueLine[] DefaultDialogueLines => keyPickupDialogue;

    private void Start()
    {
        interactable = GetComponent<Interactable>();
    }

    public void PerformInteraction(PlayerController player)
    {
        if (performed)
            return;

        performed = true;
        interactable?.MarkCompleted();

        if (keyRenderer != null)
            keyRenderer.enabled = false;

        if (pickupAudio != null)
            pickupAudio.Play();

        if (librarySafe != null)
            librarySafe.SetKeyFound(true);

        MagnifierGlassController.ForceCloseLoupeUntilKeyReleased();

        PlayDialogue(player, keyPickupDialogue);

        interactable.isInteractableActive = false;
        interactable.interactiveShader = null;

        if (player != null)
            player.currentInteractable = null;
    }
}
